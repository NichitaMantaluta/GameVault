using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameStore.Api.Features.Cart.GetCart;
using GameStore.Api.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Tests.Features.Cart.RemoveCartItem;

public class RemoveCartItemTests : IClassFixture<GameStoreApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly GameStoreApiFactory _factory;

    public RemoveCartItemTests(GameStoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RemoveCartItem_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/cart/items/" + Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RemoveCartItem_RemovesItemWithoutDeletingGame()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(genreId, name: $"Remove {Guid.NewGuid()}");
        var client = _factory.CreateUserClient($"remove-{Guid.NewGuid()}");
        await client.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });

        var response = await client.DeleteAsync("/api/cart/items/" + game.Id);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var cartResponse = await client.GetAsync("/api/cart");
        var cart = await cartResponse.Content.ReadFromJsonAsync<GetCartResponse>(JsonOptions);
        Assert.NotNull(cart);
        Assert.Empty(cart.Items);
        Assert.Equal(0m, cart.Subtotal);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        var persistedGame = await db.Games.FindAsync(game.Id);
        Assert.NotNull(persistedGame);
        Assert.True(persistedGame.IsActive);
    }

    [Fact]
    public async Task RemoveCartItem_WhenItemDoesNotExist_ReturnsNotFound()
    {
        var client = _factory.CreateUserClient($"remove-missing-{Guid.NewGuid()}");

        var response = await client.DeleteAsync("/api/cart/items/" + Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
