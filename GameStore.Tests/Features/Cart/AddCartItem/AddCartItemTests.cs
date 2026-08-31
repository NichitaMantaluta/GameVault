using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameStore.Api.Features.Cart.GetCart;
using GameStore.Api.Features.Orders.CreateOrder;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Tests.Features.Cart.AddCartItem;

public class AddCartItemTests : IClassFixture<GameStoreApiFactory>
{
    private readonly GameStoreApiFactory _factory;

    public AddCartItemTests(GameStoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddCartItem_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/cart/items", new { gameId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddCartItem_WithActiveGame_AddsQuantityOne()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(genreId, name: $"Add Active {Guid.NewGuid()}", price: 12.50m);
        var client = _factory.CreateUserClient($"add-active-{Guid.NewGuid()}");

        var response = await client.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetCartResponse>(TestJson.Options);
        Assert.NotNull(body);
        var item = Assert.Single(body.Items);
        Assert.Equal(game.Id, item.GameId);
        Assert.Equal(game.Name, item.Name);
        Assert.Equal(12.50m, item.Price);
        Assert.Equal(1, item.Quantity);
        Assert.Equal(12.50m, item.LineTotal);
        Assert.Equal(12.50m, body.Subtotal);
    }

    [Fact]
    public async Task AddCartItem_WhenGameAlreadyInCart_DoesNotDuplicateOrIncrement()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(genreId, name: $"Already In Cart {Guid.NewGuid()}", price: 10m);
        var client = _factory.CreateUserClient($"already-{Guid.NewGuid()}");

        await client.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });
        var response = await client.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetCartResponse>(TestJson.Options);
        Assert.NotNull(body);
        var item = Assert.Single(body.Items);
        Assert.Equal(1, item.Quantity);
        Assert.Equal(10m, item.LineTotal);
        Assert.Equal(10m, body.Subtotal);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        var persistedCount = await db.CartItems.CountAsync(cartItem => cartItem.GameId == game.Id);
        Assert.Equal(1, persistedCount);
    }

    [Fact]
    public async Task AddCartItem_WithNonexistentGame_ReturnsNotFound()
    {
        var client = _factory.CreateUserClient($"missing-game-{Guid.NewGuid()}");

        var response = await client.PostAsJsonAsync("/api/cart/items", new { gameId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddCartItem_WithInactiveGame_ReturnsBadRequest()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(
            genreId,
            name: $"Inactive {Guid.NewGuid()}",
            isActive: false);
        var client = _factory.CreateUserClient($"inactive-{Guid.NewGuid()}");

        var response = await client.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddCartItem_WhenGameAlreadyOwned_ReturnsBadRequest()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(genreId, name: $"Owned Add {Guid.NewGuid()}", price: 15m);
        var userId = $"owned-add-{Guid.NewGuid()}";
        var client = _factory.CreateUserClient(userId);
        var anonymous = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });
        var createResponse = await client.PostAsync("/api/orders", null);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>(TestJson.Options);
        Assert.NotNull(created);

        var webhook = await StripeWebhookTestHelper.PostCheckoutSessionCompletedAsync(
            anonymous,
            created.Id,
            created.CheckoutUrl.Split('/').Last(),
            GameStoreApiFactory.StripeWebhookSecret);
        Assert.Equal(HttpStatusCode.OK, webhook.StatusCode);

        var response = await client.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestJson.Options);
        Assert.Equal(
            $"Game '{game.Name}' is already owned.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task AddCartItem_IgnoresClientSuppliedUserId()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(genreId, name: $"Owner {Guid.NewGuid()}");
        var ownerId = $"owner-{Guid.NewGuid()}";
        var client = _factory.CreateUserClient(ownerId);

        var response = await client.PostAsJsonAsync(
            "/api/cart/items",
            new { gameId = game.Id, userId = "someone-else" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        var cart = await db.Carts.SingleAsync(existing => existing.UserId == ownerId);
        Assert.Equal(ownerId, cart.UserId);
        Assert.False(await db.Carts.AnyAsync(existing => existing.UserId == "someone-else"));
    }
}
