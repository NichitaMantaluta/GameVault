using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameStore.Api.Features.Games.CreateGame;
using GameStore.Api.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Tests.Features.Games.CreateGame;

public class CreateGameTests : IClassFixture<GameStoreApiFactory>
{
    private readonly GameStoreApiFactory _factory;
    private readonly HttpClient _client;

    public CreateGameTests(GameStoreApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        TestJwt.AuthenticateAsAdmin(_client);
    }

    [Fact]
    public async Task CreateGame_WithValidRequest_ReturnsCreatedAndPersistsActiveGame()
    {
        var genreId = await _factory.SeedGenreAsync();
        var request = new
        {
            name = "Super Mario Bros. 3",
            description = "A classic platform game.",
            price = 19.99m,
            genreId
        };

        var response = await _client.PostAsJsonAsync("/api/games", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateGameResponse>(TestJson.Options);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(request.name, body.Name);
        Assert.Equal(request.description, body.Description);
        Assert.Equal(request.price, body.Price);
        Assert.Null(body.ImageUrl);
        Assert.Equal(genreId, body.GenreId);
        Assert.True(body.IsActive);
        Assert.Equal($"/api/games/{body.Id}", response.Headers.Location?.ToString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        var persisted = await db.Games.FindAsync(body.Id);

        Assert.NotNull(persisted);
        Assert.Equal(request.name, persisted.Name);
        Assert.Equal(request.description, persisted.Description);
        Assert.Equal(request.price, persisted.Price);
        Assert.Null(persisted.ImageUrl);
        Assert.Equal(genreId, persisted.GenreId);
        Assert.True(persisted.IsActive);
        Assert.True(persisted.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.True(persisted.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.Equal(persisted.CreatedAt, persisted.UpdatedAt);
    }

    [Theory]
    [InlineData(null, "A classic platform game.", 19.99)]
    [InlineData("", "A classic platform game.", 19.99)]
    [InlineData("   ", "A classic platform game.", 19.99)]
    [InlineData("Super Mario Bros. 3", null, 19.99)]
    [InlineData("Super Mario Bros. 3", "", 19.99)]
    [InlineData("Super Mario Bros. 3", "   ", 19.99)]
    public async Task CreateGame_WithMissingRequiredFields_ReturnsValidationProblem(
        string? name,
        string? description,
        decimal price)
    {
        var genreId = await _factory.SeedGenreAsync();
        var payload = JsonSerializer.Serialize(new
        {
            name,
            description,
            price,
            genreId
        });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/games", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_WithNameTooLong_ReturnsValidationProblem()
    {
        var genreId = await _factory.SeedGenreAsync();
        var request = new
        {
            name = new string('A', 201),
            description = "A classic platform game.",
            price = 19.99m,
            genreId
        };

        var response = await _client.PostAsJsonAsync("/api/games", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_WithDescriptionTooLong_ReturnsValidationProblem()
    {
        var genreId = await _factory.SeedGenreAsync();
        var request = new
        {
            name = "Super Mario Bros. 3",
            description = new string('A', 4001),
            price = 19.99m,
            genreId
        };

        var response = await _client.PostAsJsonAsync("/api/games", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_WithNegativePrice_ReturnsValidationProblem()
    {
        var genreId = await _factory.SeedGenreAsync();
        var request = new
        {
            name = "Super Mario Bros. 3",
            description = "A classic platform game.",
            price = -1m,
            genreId
        };

        var response = await _client.PostAsJsonAsync("/api/games", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_WithEmptyGenreId_ReturnsValidationProblem()
    {
        var request = new
        {
            name = "Super Mario Bros. 3",
            description = "A classic platform game.",
            price = 19.99m,
            genreId = 0
        };

        var response = await _client.PostAsJsonAsync("/api/games", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_WithNonExistentGenre_ReturnsBadRequest()
    {
        await _factory.SeedGenreAsync();
        var request = new
        {
            name = "Super Mario Bros. 3",
            description = "A classic platform game.",
            price = 19.99m,
            genreId = 99999
        };

        var response = await _client.PostAsJsonAsync("/api/games", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_IgnoresClientSuppliedServerManagedFields()
    {
        var genreId = await _factory.SeedGenreAsync();
        var clientSuppliedId = Guid.NewGuid();
        var payload = $$"""
            {
              "id": "{{clientSuppliedId}}",
              "name": "Super Mario Bros. 3",
              "description": "A classic platform game.",
              "price": 19.99,
              "genreId": {{genreId}},
              "createdAt": "2000-01-01T00:00:00Z",
              "updatedAt": "2000-01-01T00:00:00Z",
              "isActive": false,
              "imageUrl": "https://example.com/ignored.png"
            }
            """;

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/games", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateGameResponse>(TestJson.Options);
        Assert.NotNull(body);
        Assert.NotEqual(clientSuppliedId, body.Id);
        Assert.True(body.IsActive);
        Assert.NotEqual(DateTimeOffset.Parse("2000-01-01T00:00:00Z"), body.CreatedAt);
    }

    [Fact]
    public async Task CreateGame_WithImageUrl_PersistsImageUrl()
    {
        var genreId = await _factory.SeedGenreAsync();
        const string imageUrl = "https://upload.wikimedia.org/wikipedia/commons/0/0f/Celeste_box_art_full.png";
        var request = new
        {
            name = "Celeste",
            description = "A precise and emotional mountain-climbing platformer.",
            price = 19.99m,
            genreId,
            imageUrl
        };

        var response = await _client.PostAsJsonAsync("/api/games", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateGameResponse>(TestJson.Options);
        Assert.NotNull(body);
        Assert.Equal(imageUrl, body.ImageUrl);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        var persisted = await db.Games.FindAsync(body.Id);

        Assert.NotNull(persisted);
        Assert.Equal(imageUrl, persisted.ImageUrl);
    }

    [Fact]
    public async Task CreateGame_WithImageUrlTooLong_ReturnsValidationProblem()
    {
        var genreId = await _factory.SeedGenreAsync();
        var request = new
        {
            name = "Celeste",
            description = "A precise and emotional mountain-climbing platformer.",
            price = 19.99m,
            genreId,
            imageUrl = "https://example.com/" + new string('a', 2048)
        };

        var response = await _client.PostAsJsonAsync("/api/games", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
