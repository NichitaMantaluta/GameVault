using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameStore.Api.Features.Cart.GetCart;

namespace GameStore.Tests.Features.Cart.UpdateCartItem;

public class UpdateCartItemTests : IClassFixture<GameStoreApiFactory>
{
    private readonly GameStoreApiFactory _factory;

    public UpdateCartItemTests(GameStoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateCartItem_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsJsonAsync(
            "/api/cart/items/" + Guid.NewGuid(),
            new { quantity = 2 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCartItem_WithValidQuantity_UpdatesItem()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(genreId, name: $"Update Qty {Guid.NewGuid()}", price: 15m);
        var client = _factory.CreateUserClient($"update-{Guid.NewGuid()}");
        await client.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });

        var response = await client.PatchAsJsonAsync(
            "/api/cart/items/" + game.Id,
            new { quantity = 3 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetCartResponse>(TestJson.Options);
        Assert.NotNull(body);
        var item = Assert.Single(body.Items);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(45m, item.LineTotal);
        Assert.Equal(45m, body.Subtotal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateCartItem_WithInvalidQuantity_ReturnsValidationProblem(int quantity)
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(genreId, name: $"Invalid Qty {Guid.NewGuid()}");
        var client = _factory.CreateUserClient($"invalid-qty-{Guid.NewGuid()}");
        await client.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });

        var response = await client.PatchAsJsonAsync(
            "/api/cart/items/" + game.Id,
            new { quantity });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCartItem_WhenItemDoesNotExist_ReturnsNotFound()
    {
        var client = _factory.CreateUserClient($"missing-item-{Guid.NewGuid()}");

        var response = await client.PatchAsJsonAsync(
            "/api/cart/items/" + Guid.NewGuid(),
            new { quantity = 2 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
