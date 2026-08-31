using System.Net;
using System.Net.Http.Json;
using GameStore.Api.Features.Orders.CreateOrder;
using GameStore.Api.Features.Orders.GetOwnedGames;

namespace GameStore.Tests.Features.Orders.GetOwnedGames;

public class GetOwnedGamesTests : IClassFixture<GameStoreApiFactory>
{
    private readonly GameStoreApiFactory _factory;

    public GetOwnedGamesTests(GameStoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetOwnedGames_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/orders/owned-games");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOwnedGames_WithNoPurchases_ReturnsEmptyList()
    {
        var client = _factory.CreateUserClient($"owned-empty-{Guid.NewGuid()}");

        var response = await client.GetAsync("/api/orders/owned-games");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetOwnedGamesResponse>(TestJson.Options);
        Assert.NotNull(body);
        Assert.Empty(body.GameIds);
    }

    [Fact]
    public async Task GetOwnedGames_ReturnsOnlyCompletedPurchasesForCurrentUser()
    {
        var genreId = await _factory.SeedGenreAsync();
        var owned = await _factory.SeedGameAsync(genreId, name: $"Library Owned {Guid.NewGuid()}", price: 11m);
        var otherUsersGame = await _factory.SeedGameAsync(genreId, name: $"Library Other {Guid.NewGuid()}", price: 9m);
        var owner = _factory.CreateUserClient($"library-owner-{Guid.NewGuid()}");
        var other = _factory.CreateUserClient($"library-other-{Guid.NewGuid()}");
        var anonymous = _factory.CreateClient();

        await owner.PostAsJsonAsync("/api/cart/items", new { gameId = owned.Id });
        var ownerOrder = await (await owner.PostAsync("/api/orders", null))
            .Content.ReadFromJsonAsync<CreateOrderResponse>(TestJson.Options);
        Assert.NotNull(ownerOrder);
        Assert.Equal(HttpStatusCode.OK, (await StripeWebhookTestHelper.PostCheckoutSessionCompletedAsync(
            anonymous,
            ownerOrder.Id,
            ownerOrder.CheckoutUrl.Split('/').Last(),
            GameStoreApiFactory.StripeWebhookSecret)).StatusCode);

        await other.PostAsJsonAsync("/api/cart/items", new { gameId = otherUsersGame.Id });
        var otherOrder = await (await other.PostAsync("/api/orders", null))
            .Content.ReadFromJsonAsync<CreateOrderResponse>(TestJson.Options);
        Assert.NotNull(otherOrder);
        Assert.Equal(HttpStatusCode.OK, (await StripeWebhookTestHelper.PostCheckoutSessionCompletedAsync(
            anonymous,
            otherOrder.Id,
            otherOrder.CheckoutUrl.Split('/').Last(),
            GameStoreApiFactory.StripeWebhookSecret)).StatusCode);

        var response = await owner.GetAsync("/api/orders/owned-games");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetOwnedGamesResponse>(TestJson.Options);
        Assert.NotNull(body);
        Assert.Equal([owned.Id], body.GameIds);
    }
}
