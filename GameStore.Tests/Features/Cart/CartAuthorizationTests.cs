using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameStore.Api.Features.Cart.GetCart;

namespace GameStore.Tests.Features.Cart;

public class CartAuthorizationTests : IClassFixture<GameStoreApiFactory>
{
    private readonly GameStoreApiFactory _factory;

    public CartAuthorizationTests(GameStoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Cart_IsScopedToAuthenticatedUser()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(genreId, name: $"Owned {Guid.NewGuid()}");
        var owner = _factory.CreateUserClient($"owner-{Guid.NewGuid()}");
        var other = _factory.CreateUserClient($"other-{Guid.NewGuid()}");

        var addResponse = await owner.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        var ownerCartResponse = await owner.GetAsync("/api/cart");
        var otherCartResponse = await other.GetAsync("/api/cart");

        Assert.Equal(HttpStatusCode.OK, ownerCartResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otherCartResponse.StatusCode);

        var ownerCart = await ownerCartResponse.Content.ReadFromJsonAsync<GetCartResponse>(TestJson.Options);
        var otherCart = await otherCartResponse.Content.ReadFromJsonAsync<GetCartResponse>(TestJson.Options);

        Assert.NotNull(ownerCart);
        Assert.Single(ownerCart.Items);
        Assert.Equal(game.Id, ownerCart.Items[0].GameId);

        Assert.NotNull(otherCart);
        Assert.Empty(otherCart.Items);

        var otherUpdate = await other.PatchAsJsonAsync(
            "/api/cart/items/" + game.Id,
            new { quantity = 5 });
        var otherDelete = await other.DeleteAsync("/api/cart/items/" + game.Id);

        Assert.Equal(HttpStatusCode.NotFound, otherUpdate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherDelete.StatusCode);

        var ownerCartAfter = await owner.GetFromJsonAsync<GetCartResponse>("/api/cart", TestJson.Options);
        Assert.NotNull(ownerCartAfter);
        Assert.Equal(1, Assert.Single(ownerCartAfter.Items).Quantity);
    }
}
