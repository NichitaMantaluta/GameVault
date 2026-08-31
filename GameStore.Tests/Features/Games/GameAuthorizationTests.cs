using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameStore.Api.Features.Games.CreateGame;
using GameStore.Api.Features.Games.GetGame;
using GameStore.Api.Features.Games.GetGames;
using GameStore.Api.Features.Games.UpdateGame;
using GameStore.Api.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Tests.Features.Games;

public class GameAuthorizationTests : IClassFixture<GameStoreApiFactory>
{
    private static readonly object ValidGameRequest = new
    {
        name = "Super Mario Bros. 3",
        description = "A classic platform game.",
        price = 19.99m,
        genreId = 1
    };

    private readonly GameStoreApiFactory _factory;

    public GameAuthorizationTests(GameStoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateGame_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/games", ValidGameRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_NonAdmin_ReturnsForbidden()
    {
        var client = _factory.CreateCustomerClient();

        var response = await client.PostAsJsonAsync("/api/games", ValidGameRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_Admin_ReturnsCreated()
    {
        var genreId = await _factory.SeedGenreAsync();
        var client = _factory.CreateAdminClient();
        var request = new
        {
            name = "Authorized Create",
            description = "Created by an administrator.",
            price = 19.99m,
            genreId
        };

        var response = await client.PostAsJsonAsync("/api/games", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateGameResponse>(TestJson.Options);
        Assert.NotNull(body);
        Assert.Equal(request.name, body.Name);
    }

    [Fact]
    public async Task UpdateGame_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/games/" + Guid.NewGuid(), ValidGameRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGame_NonAdmin_ReturnsForbidden()
    {
        var client = _factory.CreateCustomerClient();

        var response = await client.PutAsJsonAsync("/api/games/" + Guid.NewGuid(), ValidGameRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGame_Admin_IsAllowed()
    {
        var genreId = await _factory.SeedGenreAsync("Action");
        var game = await _factory.SeedGameAsync(genreId);
        var client = _factory.CreateAdminClient();
        var request = new
        {
            name = "Authorized Update",
            description = "Updated by an administrator.",
            price = 24.99m,
            genreId,
            isActive = true
        };

        var response = await client.PutAsJsonAsync("/api/games/" + game.Id, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<UpdateGameResponse>(TestJson.Options);
        Assert.NotNull(body);
        Assert.Equal(request.name, body.Name);
    }

    [Fact]
    public async Task DeleteGame_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/games/" + Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGame_NonAdmin_ReturnsForbidden()
    {
        var client = _factory.CreateCustomerClient();

        var response = await client.DeleteAsync("/api/games/" + Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGame_Admin_IsAllowed()
    {
        var genreId = await _factory.SeedGenreAsync("Adventure");
        var game = await _factory.SeedGameAsync(genreId, name: "Authorized Delete");
        var client = _factory.CreateAdminClient();

        var response = await client.DeleteAsync("/api/games/" + game.Id);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        var persisted = await db.Games.FindAsync(game.Id);

        Assert.NotNull(persisted);
        Assert.False(persisted.IsActive);
    }

    [Fact]
    public async Task GetGames_RemainsPublic()
    {
        var genreId = await _factory.SeedGenreAsync("Public Catalog");
        await _factory.SeedGameAsync(genreId, name: "Public Game");
        var anonymous = _factory.CreateClient();
        var customer = _factory.CreateCustomerClient();

        var anonymousResponse = await anonymous.GetAsync("/api/games");
        var customerResponse = await customer.GetAsync("/api/games");

        Assert.Equal(HttpStatusCode.OK, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, customerResponse.StatusCode);

        var catalog = await anonymousResponse.Content.ReadFromJsonAsync<GetGamesResponse>(TestJson.Options);
        Assert.NotNull(catalog);
        Assert.Contains(catalog.Items, item => item.Name == "Public Game");
    }

    [Fact]
    public async Task GetGame_RemainsPublic()
    {
        var genreId = await _factory.SeedGenreAsync("Public Detail");
        var game = await _factory.SeedGameAsync(genreId, name: "Public Detail Game");
        var anonymous = _factory.CreateClient();
        var customer = _factory.CreateCustomerClient();

        var anonymousResponse = await anonymous.GetAsync("/api/games/" + game.Id);
        var customerResponse = await customer.GetAsync("/api/games/" + game.Id);

        Assert.Equal(HttpStatusCode.OK, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, customerResponse.StatusCode);

        var body = await anonymousResponse.Content.ReadFromJsonAsync<GetGameResponse>(TestJson.Options);
        Assert.NotNull(body);
        Assert.Equal(game.Id, body.Id);
    }
}
