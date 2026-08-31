using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameStore.Api.Domain.Carts;
using GameStore.Api.Domain.Orders;
using GameStore.Api.Features.Cart.GetCart;
using GameStore.Api.Features.Orders.CreateOrder;
using GameStore.Api.Features.Orders.GetOrder;
using GameStore.Api.Integrations.Payments;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Tests.Features.Orders.CreateOrder;

public class CreateOrderTests : IClassFixture<GameStoreApiFactory>
{
    private readonly GameStoreApiFactory _factory;

    public CreateOrderTests(GameStoreApiFactory factory)
    {
        _factory = factory;
        _factory.PaymentService.ShouldFail = false;
        _factory.PaymentService.LastRequest = null;
    }

    [Fact]
    public async Task CreateOrder_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/orders", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithCartItems_CreatesPendingOrderCheckoutSessionAndKeepsCart()
    {
        var genreId = await _factory.SeedGenreAsync();
        var mario = await _factory.SeedGameAsync(
            genreId,
            name: $"Mario {Guid.NewGuid()}",
            price: 19.99m);
        var zelda = await _factory.SeedGameAsync(
            genreId,
            name: $"Zelda {Guid.NewGuid()}",
            price: 59.99m);
        var userId = $"checkout-{Guid.NewGuid()}";
        var client = _factory.CreateUserClient(userId);

        await client.PostAsJsonAsync("/api/cart/items", new { gameId = mario.Id });
        await client.PostAsJsonAsync("/api/cart/items", new { gameId = zelda.Id });

        var response = await client.PostAsync("/api/orders", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateOrderResponse>(TestJson.Options);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(OrderStatus.Pending, body.Status);
        Assert.Equal(79.98m, body.TotalAmount);
        Assert.Equal("USD", body.Currency);
        Assert.False(string.IsNullOrWhiteSpace(body.CheckoutUrl));
        Assert.StartsWith("https://checkout.test/pay/", body.CheckoutUrl);
        Assert.Equal($"/api/orders/{body.Id}", response.Headers.Location?.ToString());
        Assert.Equal(2, body.Items.Count);

        var marioItem = Assert.Single(body.Items, item => item.GameId == mario.Id);
        Assert.Equal(mario.Name, marioItem.GameName);
        Assert.Equal(19.99m, marioItem.Price);

        var zeldaItem = Assert.Single(body.Items, item => item.GameId == zelda.Id);
        Assert.Equal(zelda.Name, zeldaItem.GameName);
        Assert.Equal(59.99m, zeldaItem.Price);

        var paymentRequest = _factory.PaymentService.LastRequest;
        Assert.NotNull(paymentRequest);
        Assert.Equal(body.Id, paymentRequest.OrderId);
        Assert.Equal("USD", paymentRequest.Currency);
        Assert.Equal(2, paymentRequest.LineItems.Count);
        Assert.Contains(paymentRequest.LineItems, item => item.Name == mario.Name && item.UnitAmount == 19.99m);
        Assert.Contains(paymentRequest.LineItems, item => item.Name == zelda.Name && item.UnitAmount == 59.99m);

        var cart = await client.GetFromJsonAsync<GetCartResponse>("/api/cart", TestJson.Options);
        Assert.NotNull(cart);
        Assert.Equal(2, cart.Items.Count);

        using var scope = _factory.Services.CreateScope();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
        Assert.IsType<FakePaymentService>(paymentService);

        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        var persisted = await db.Orders
            .Include(order => order.Items)
            .SingleAsync(order => order.Id == body.Id);
        Assert.Equal(userId, persisted.UserId);
        Assert.Equal(OrderStatus.Pending, persisted.Status);
        Assert.Equal($"cs_test_{body.Id:N}", persisted.StripeCheckoutSessionId);
        Assert.Equal(79.98m, persisted.TotalAmount);
        Assert.Equal(2, persisted.Items.Count);
    }

    [Fact]
    public async Task CreateOrder_UsesServerSideGamePricesForCheckoutLineItems()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(genreId, name: $"Priced {Guid.NewGuid()}", price: 12.34m);
        var client = _factory.CreateUserClient($"priced-{Guid.NewGuid()}");
        await client.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });

        var response = await client.PostAsync("/api/orders", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var paymentRequest = _factory.PaymentService.LastRequest;
        Assert.NotNull(paymentRequest);
        var line = Assert.Single(paymentRequest.LineItems);
        Assert.Equal(game.Name, line.Name);
        Assert.Equal(12.34m, line.UnitAmount);
    }

    [Fact]
    public async Task CreateOrder_WhenPaymentProviderFails_DoesNotPersistOrderOrClearCart()
    {
        _factory.PaymentService.ShouldFail = true;

        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(genreId, name: $"Fail {Guid.NewGuid()}", price: 10m);
        var userId = $"fail-{Guid.NewGuid()}";
        var client = _factory.CreateUserClient(userId);
        await client.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });

        var response = await client.PostAsync("/api/orders", null);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        await AssertUserHasNoOrdersAsync(userId);
        await AssertCartContainsGameAsync(client, game.Id);
    }

    [Fact]
    public async Task CreateOrder_WithEmptyCart_ReturnsBadRequestAndCreatesNoOrder()
    {
        var userId = $"empty-{Guid.NewGuid()}";
        var client = _factory.CreateUserClient(userId);

        var response = await client.PostAsync("/api/orders", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertUserHasNoOrdersAsync(userId);
    }

    [Fact]
    public async Task CreateOrder_WithInactiveGame_ReturnsBadRequestAndLeavesCartUnchanged()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(genreId, name: $"Inactive later {Guid.NewGuid()}", price: 10m);
        var userId = $"inactive-{Guid.NewGuid()}";
        var client = _factory.CreateUserClient(userId);
        await client.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
            var persistedGame = await db.Games.FindAsync(game.Id);
            Assert.NotNull(persistedGame);
            persistedGame.IsActive = false;
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsync("/api/orders", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertUserHasNoOrdersAsync(userId);
        await AssertCartContainsGameAsync(client, game.Id);
    }

    [Fact]
    public async Task CreateOrder_WithMissingGame_ReturnsBadRequestAndLeavesCartUnchanged()
    {
        var userId = $"missing-game-{Guid.NewGuid()}";
        var missingGameId = Guid.NewGuid();
        await SeedCartItemAsync(userId, missingGameId);
        var client = _factory.CreateUserClient(userId);

        var response = await client.PostAsync("/api/orders", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertUserHasNoOrdersAsync(userId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        Assert.True(await db.CartItems.AnyAsync(item => item.GameId == missingGameId));
    }

    [Fact]
    public async Task CreateOrder_WhenGameAlreadyOwned_ReturnsBadRequestAndLeavesCartUnchanged()
    {
        var genreId = await _factory.SeedGenreAsync();
        var owned = await _factory.SeedGameAsync(genreId, name: $"Owned {Guid.NewGuid()}", price: 12m);
        var extra = await _factory.SeedGameAsync(genreId, name: $"Extra {Guid.NewGuid()}", price: 8m);
        var userId = $"owned-{Guid.NewGuid()}";
        var client = _factory.CreateUserClient(userId);
        var anonymous = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/cart/items", new { gameId = owned.Id });
        var first = await client.PostAsync("/api/orders", null);
        var created = await first.Content.ReadFromJsonAsync<CreateOrderResponse>(TestJson.Options);
        Assert.NotNull(created);

        var webhook = await StripeWebhookTestHelper.PostCheckoutSessionCompletedAsync(
            anonymous,
            created.Id,
            created.CheckoutUrl.Split('/').Last(),
            GameStoreApiFactory.StripeWebhookSecret);
        Assert.Equal(HttpStatusCode.OK, webhook.StatusCode);

        // AddCartItem blocks owned games; seed the cart directly to verify checkout still rejects them.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
            var cart = await db.Carts
                .Include(existing => existing.Items)
                .SingleOrDefaultAsync(existing => existing.UserId == userId);

            if (cart is null)
            {
                cart = new GameStore.Api.Domain.Carts.Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = userId
                };
                db.Carts.Add(cart);
            }
            else
            {
                cart.Items.Clear();
            }

            cart.Items.Add(new CartItem { CartId = cart.Id, GameId = owned.Id, Quantity = 1 });
            cart.Items.Add(new CartItem { CartId = cart.Id, GameId = extra.Id, Quantity = 1 });
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsync("/api/orders", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestJson.Options);
        Assert.Equal(
            $"Game '{owned.Name}' is already owned.",
            problem.GetProperty("detail").GetString());

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
            Assert.Equal(1, await db.Orders.CountAsync(order => order.UserId == userId));
            Assert.Equal(1, await db.OrderItems.CountAsync(item => item.GameId == owned.Id));
            Assert.False(await db.OrderItems.AnyAsync(item => item.GameId == extra.Id));
        }

        var cartResponse = await client.GetFromJsonAsync<GetCartResponse>("/api/cart", TestJson.Options);
        Assert.NotNull(cartResponse);
        Assert.Equal(2, cartResponse.Items.Count);
        Assert.Contains(cartResponse.Items, item => item.GameId == owned.Id);
        Assert.Contains(cartResponse.Items, item => item.GameId == extra.Id);
    }

    [Fact]
    public async Task CreateOrder_AfterGamePriceAndNameChange_KeepsOriginalSnapshots()
    {
        var genreId = await _factory.SeedGenreAsync();
        var originalName = $"Snapshot {Guid.NewGuid()}";
        var game = await _factory.SeedGameAsync(genreId, name: originalName, price: 19.99m);
        var client = _factory.CreateUserClient($"snapshot-{Guid.NewGuid()}");

        await client.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });
        var createResponse = await client.PostAsync("/api/orders", null);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>(TestJson.Options);
        Assert.NotNull(created);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
            var persistedGame = await db.Games.FindAsync(game.Id);
            Assert.NotNull(persistedGame);
            persistedGame.Name = "Changed Name";
            persistedGame.Price = 99.99m;
            await db.SaveChangesAsync();
        }

        var orderResponse = await client.GetAsync("/api/orders/" + created.Id);
        var order = await orderResponse.Content.ReadFromJsonAsync<GetOrderResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, orderResponse.StatusCode);
        Assert.NotNull(order);
        var item = Assert.Single(order.Items);
        Assert.Equal(originalName, item.GameName);
        Assert.Equal(19.99m, item.Price);
        Assert.Equal(19.99m, order.TotalAmount);
    }

    private async Task SeedCartItemAsync(string userId, Guid gameId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        await db.Database.EnsureCreatedAsync();

        var cart = new GameStore.Api.Domain.Carts.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };
        cart.Items.Add(new CartItem
        {
            CartId = cart.Id,
            GameId = gameId,
            Quantity = 1
        });
        db.Carts.Add(cart);
        await db.SaveChangesAsync();
    }

    private async Task AssertUserHasNoOrdersAsync(string userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        Assert.False(await db.Orders.AnyAsync(order => order.UserId == userId));
    }

    private static async Task AssertCartContainsGameAsync(HttpClient client, Guid gameId)
    {
        var cart = await client.GetFromJsonAsync<GetCartResponse>("/api/cart", TestJson.Options);
        Assert.NotNull(cart);
        var item = Assert.Single(cart.Items);
        Assert.Equal(gameId, item.GameId);
    }
}
