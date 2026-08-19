using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameStore.Api.Features.Games.GetGames;
using GameStore.Api.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Tests.Features.Games.DeleteGame;

public class DeleteGameTests : IClassFixture<GameStoreApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly GameStoreApiFactory _factory;
    private readonly HttpClient _client;

    public DeleteGameTests(GameStoreApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAdminClient();
    }

    [Fact]
    public async Task DeleteGame_WithExistingActiveGame_DeactivatesAndHidesFromCatalog()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(genreId, name: "Celeste", isActive: true);

        var response = await _client.DeleteAsync("/api/games/" + game.Id);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        var persisted = await db.Games.FindAsync(game.Id);

        Assert.NotNull(persisted);
        Assert.False(persisted.IsActive);
        Assert.True(persisted.UpdatedAt > game.UpdatedAt);

        var catalogResponse = await _client.GetAsync("/api/games");
        Assert.Equal(HttpStatusCode.OK, catalogResponse.StatusCode);

        var catalog = await catalogResponse.Content.ReadFromJsonAsync<GetGamesResponse>(JsonOptions);
        Assert.NotNull(catalog);
        Assert.DoesNotContain(catalog.Items, item => item.Id == game.Id);
    }

    [Fact]
    public async Task DeleteGame_WithAlreadyInactiveGame_ReturnsNoContent()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(genreId, isActive: false);

        var response = await _client.DeleteAsync("/api/games/" + game.Id);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGame_WithUnknownId_ReturnsNotFound()
    {
        await _factory.SeedGenreAsync();

        var response = await _client.DeleteAsync("/api/games/" + Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
