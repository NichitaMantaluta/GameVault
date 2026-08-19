using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameStore.Api.Features.Cart.GetCart;

namespace GameStore.Tests.Features.Cart.GetCart;

public class GetCartTests : IClassFixture<GameStoreApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly GameStoreApiFactory _factory;

    public GetCartTests(GameStoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCart_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/cart");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCart_WhenNoCartExists_ReturnsEmptyCart()
    {
        var client = _factory.CreateUserClient($"empty-cart-{Guid.NewGuid()}");

        var response = await client.GetAsync("/api/cart");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetCartResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Empty(body.Items);
        Assert.Equal(0m, body.Subtotal);
    }

    [Fact]
    public async Task GetCart_ReturnsLineTotalsAndSubtotalFromCurrentGamePrice()
    {
        var genreId = await _factory.SeedGenreAsync();
        var mario = await _factory.SeedGameAsync(
            genreId,
            name: $"Mario {Guid.NewGuid()}",
            price: 19.99m,
            imageUrl: "https://example.com/mario.png");
        var zelda = await _factory.SeedGameAsync(
            genreId,
            name: $"Zelda {Guid.NewGuid()}",
            price: 59.99m,
            imageUrl: "https://example.com/zelda.png");
        var client = _factory.CreateUserClient($"totals-{Guid.NewGuid()}");

        await client.PostAsJsonAsync("/api/cart/items", new { gameId = mario.Id });
        await client.PostAsJsonAsync("/api/cart/items", new { gameId = zelda.Id });

        var response = await client.GetAsync("/api/cart");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetCartResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(2, body.Items.Count);

        var marioItem = Assert.Single(body.Items, item => item.GameId == mario.Id);
        Assert.Equal(mario.Name, marioItem.Name);
        Assert.Equal(19.99m, marioItem.Price);
        Assert.Equal(mario.ImageUrl, marioItem.ImageUrl);
        Assert.Equal(1, marioItem.Quantity);
        Assert.Equal(19.99m, marioItem.LineTotal);

        var zeldaItem = Assert.Single(body.Items, item => item.GameId == zelda.Id);
        Assert.Equal(1, zeldaItem.Quantity);
        Assert.Equal(59.99m, zeldaItem.LineTotal);

        Assert.Equal(79.98m, body.Subtotal);
    }
}
