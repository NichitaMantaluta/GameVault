using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameStore.Api.Domain.Orders;
using GameStore.Api.Features.Orders.CreateOrder;
using GameStore.Api.Features.Orders.GetOrder;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Tests.Features.Orders.GetOrder;

public class GetOrderTests : IClassFixture<GameStoreApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly GameStoreApiFactory _factory;

    public GetOrderTests(GameStoreApiFactory factory)
    {
        _factory = factory;
        _factory.PaymentService.ShouldFail = false;
    }

    [Fact]
    public async Task GetOrder_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/orders/" + Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_WithOwnOrder_ReturnsOrderWithoutQuantityOrLineTotal()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(genreId, name: $"Get {Guid.NewGuid()}", price: 21.50m);
        var client = _factory.CreateUserClient($"get-own-{Guid.NewGuid()}");
        await client.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });

        var createResponse = await client.PostAsync("/api/orders", null);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>(JsonOptions);
        Assert.NotNull(created);

        var response = await client.GetAsync("/api/orders/" + created.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetOrderResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(created.Id, body.Id);
        Assert.Equal(OrderStatus.Pending, body.Status);
        Assert.Equal(21.50m, body.TotalAmount);
        Assert.Equal("USD", body.Currency);
        Assert.Equal(created.CreatedAt, body.CreatedAt);

        var item = Assert.Single(body.Items);
        Assert.Equal(game.Id, item.GameId);
        Assert.Equal(game.Name, item.GameName);
        Assert.Equal(21.50m, item.Price);

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("quantity", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lineTotal", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetOrder_WithUnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateUserClient($"missing-order-{Guid.NewGuid()}");

        var response = await client.GetAsync("/api/orders/" + Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_ForAnotherUsersOrder_ReturnsNotFound()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(genreId, name: $"Other {Guid.NewGuid()}", price: 15m);
        var owner = _factory.CreateUserClient($"owner-{Guid.NewGuid()}");
        var other = _factory.CreateUserClient($"other-{Guid.NewGuid()}");

        await owner.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });
        var createResponse = await owner.PostAsync("/api/orders", null);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>(JsonOptions);
        Assert.NotNull(created);

        var response = await other.GetAsync("/api/orders/" + created.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        Assert.True(await db.Orders.AnyAsync(order => order.Id == created.Id));
    }

    [Fact]
    public async Task GetOrder_AfterGameDeactivation_StillReturnsHistoricalOrder()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(genreId, name: $"Keep {Guid.NewGuid()}", price: 30m);
        var client = _factory.CreateUserClient($"keep-{Guid.NewGuid()}");
        await client.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });
        var createResponse = await client.PostAsync("/api/orders", null);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>(JsonOptions);
        Assert.NotNull(created);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
            var persistedGame = await db.Games.FindAsync(game.Id);
            Assert.NotNull(persistedGame);
            persistedGame.IsActive = false;
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/orders/" + created.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetOrderResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(created.Id, body.Id);
        Assert.Equal(game.Name, Assert.Single(body.Items).GameName);
        Assert.True(await GameStillExistsAsync(game.Id));
    }

    private async Task<bool> GameStillExistsAsync(Guid gameId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        return await db.Games.AnyAsync(game => game.Id == gameId);
    }
}
