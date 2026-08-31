using System.Net;
using System.Net.Http.Json;
using GameStore.Api.Features.Games.GetGame;

namespace GameStore.Tests.Features.Games.GetGame;

public class GetGameTests : IClassFixture<GameStoreApiFactory>
{
    private readonly GameStoreApiFactory _factory;
    private readonly HttpClient _client;

    public GetGameTests(GameStoreApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetGame_WithExistingId_ReturnsOkAndGame()
    {
        var game = await SeedGameAsync();

        var response = await _client.GetAsync("/api/games/" + game.Id.ToString());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetGameResponse>(TestJson.Options);
        Assert.NotNull(body);
        Assert.Equal(game.Id, body.Id);
        Assert.Equal(game.Name, body.Name);
        Assert.Equal(game.Description, body.Description);
        Assert.Equal(game.Price, body.Price);
        Assert.Equal(game.ImageUrl, body.ImageUrl);
        Assert.Equal(game.GenreId, body.GenreId);
        Assert.Equal("Platformer", body.GenreName);
        Assert.True(body.IsActive);
        Assert.Equal(game.CreatedAt, body.CreatedAt);
        Assert.Equal(game.UpdatedAt, body.UpdatedAt);
    }

    [Fact]
    public async Task GetGame_WithInactiveGame_ReturnsOk()
    {
        var game = await SeedGameAsync(isActive: false);

        var response = await _client.GetAsync("/api/games/" + game.Id.ToString());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetGameResponse>(TestJson.Options);
        Assert.NotNull(body);
        Assert.Equal(game.Id, body.Id);
        Assert.False(body.IsActive);
    }

    [Fact]
    public async Task GetGame_WithUnknownId_ReturnsNotFound()
    {
        await SeedGameAsync();

        var response = await _client.GetAsync("/api/games/" + Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetGame_WithMalformedId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/games/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<GameStore.Api.Domain.Games.Game> SeedGameAsync(bool isActive = true)
    {
        var genreId = await _factory.SeedGenreAsync();
        return await _factory.SeedGameAsync(
            genreId,
            isActive: isActive,
            imageUrl: "https://example.com/super-mario-bros-3.png");
    }
}
