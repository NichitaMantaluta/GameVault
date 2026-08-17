using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameStore.Api.Domain.Games;
using GameStore.Api.Features.Games.UpdateGame;
using GameStore.Api.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Tests.Features.Games.UpdateGame;

public class UpdateGameTests : IClassFixture<GameStoreApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly GameStoreApiFactory _factory;
    private readonly HttpClient _client;

    public UpdateGameTests(GameStoreApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UpdateGame_WithValidRequest_ReturnsOkAndPersistsChanges()
    {
        var originalGenreId = await _factory.SeedGenreAsync("Platformer");
        var newGenreId = await _factory.SeedGenreAsync("Action");
        var createdAt = DateTimeOffset.UtcNow.AddHours(-2);
        var game = await SeedGameAsync(
            originalGenreId,
            name: "Super Mario Bros. 3",
            description: "A classic platform game.",
            price: 19.99m,
            isActive: false,
            createdAt: createdAt,
            updatedAt: createdAt);

        var request = new
        {
            name = "Super Mario Bros. 3 Deluxe",
            description = "An updated classic platform game.",
            price = 24.99m,
            genreId = newGenreId
        };

        var response = await _client.PutAsJsonAsync("/api/games/" + game.Id, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<UpdateGameResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(game.Id, body.Id);
        Assert.Equal(request.name, body.Name);
        Assert.Equal(request.description, body.Description);
        Assert.Equal(request.price, body.Price);
        Assert.Equal(newGenreId, body.GenreId);
        Assert.False(body.IsActive);
        Assert.Equal(createdAt, body.CreatedAt);
        Assert.True(body.UpdatedAt > createdAt);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        var persisted = await db.Games.FindAsync(game.Id);

        Assert.NotNull(persisted);
        Assert.Equal(request.name, persisted.Name);
        Assert.Equal(request.description, persisted.Description);
        Assert.Equal(request.price, persisted.Price);
        Assert.Equal(newGenreId, persisted.GenreId);
        Assert.False(persisted.IsActive);
        Assert.Equal(createdAt, persisted.CreatedAt);
        Assert.True(persisted.UpdatedAt > createdAt);
    }

    [Fact]
    public async Task UpdateGame_IgnoresClientSuppliedServerManagedFields()
    {
        var genreId = await _factory.SeedGenreAsync();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-2);
        var game = await SeedGameAsync(
            genreId,
            isActive: true,
            createdAt: createdAt,
            updatedAt: createdAt);
        var clientSuppliedId = Guid.NewGuid();

        var payload = $$"""
            {
              "id": "{{clientSuppliedId}}",
              "name": "Updated Name",
              "description": "Updated description.",
              "price": 9.99,
              "genreId": {{genreId}},
              "createdAt": "2000-01-01T00:00:00Z",
              "updatedAt": "2000-01-01T00:00:00Z",
              "isActive": false
            }
            """;

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync("/api/games/" + game.Id, content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<UpdateGameResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(game.Id, body.Id);
        Assert.NotEqual(clientSuppliedId, body.Id);
        Assert.True(body.IsActive);
        Assert.Equal(createdAt, body.CreatedAt);
        Assert.NotEqual(DateTimeOffset.Parse("2000-01-01T00:00:00Z"), body.UpdatedAt);
    }

    [Theory]
    [InlineData(null, "A classic platform game.", 19.99)]
    [InlineData("", "A classic platform game.", 19.99)]
    [InlineData("   ", "A classic platform game.", 19.99)]
    [InlineData("Super Mario Bros. 3", null, 19.99)]
    [InlineData("Super Mario Bros. 3", "", 19.99)]
    [InlineData("Super Mario Bros. 3", "   ", 19.99)]
    public async Task UpdateGame_WithMissingRequiredFields_ReturnsValidationProblem(
        string? name,
        string? description,
        decimal price)
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await SeedGameAsync(genreId);
        var payload = JsonSerializer.Serialize(new
        {
            name,
            description,
            price,
            genreId
        });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync("/api/games/" + game.Id, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGame_WithNegativePrice_ReturnsValidationProblem()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await SeedGameAsync(genreId);
        var request = new
        {
            name = "Super Mario Bros. 3",
            description = "A classic platform game.",
            price = -1m,
            genreId
        };

        var response = await _client.PutAsJsonAsync("/api/games/" + game.Id, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGame_WithMissingGenreId_ReturnsValidationProblem()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await SeedGameAsync(genreId);
        var request = new
        {
            name = "Super Mario Bros. 3",
            description = "A classic platform game.",
            price = 19.99m,
            genreId = 0
        };

        var response = await _client.PutAsJsonAsync("/api/games/" + game.Id, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGame_WithNonExistentGame_ReturnsNotFound()
    {
        var genreId = await _factory.SeedGenreAsync();
        var request = new
        {
            name = "Super Mario Bros. 3",
            description = "A classic platform game.",
            price = 19.99m,
            genreId
        };

        var response = await _client.PutAsJsonAsync("/api/games/" + Guid.NewGuid(), request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGame_WithNonExistentGenre_ReturnsBadRequest()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await SeedGameAsync(genreId);
        var request = new
        {
            name = "Super Mario Bros. 3",
            description = "A classic platform game.",
            price = 19.99m,
            genreId = 99999
        };

        var response = await _client.PutAsJsonAsync("/api/games/" + game.Id, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Game> SeedGameAsync(
        int genreId,
        string name = "Super Mario Bros. 3",
        string description = "A classic platform game.",
        decimal price = 19.99m,
        bool isActive = true,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        await db.Database.EnsureCreatedAsync();

        var timestamp = createdAt ?? DateTimeOffset.UtcNow;
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price,
            GenreId = genreId,
            CreatedAt = timestamp,
            UpdatedAt = updatedAt ?? timestamp,
            IsActive = isActive
        };

        db.Games.Add(game);
        await db.SaveChangesAsync();
        return game;
    }
}
