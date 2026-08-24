using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameStore.Api.Domain.Games;
using GameStore.Api.Features.Games.GetGames;
using GameStore.Api.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Tests.Features.Games.GetGames;

public class GetGamesTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly GameStoreApiFactory _factory;
    private readonly HttpClient _client;

    public GetGamesTests()
    {
        _factory = new GameStoreApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task GetGames_ReturnsOnlyActiveGames()
    {
        var genreId = await SeedGenreAsync();
        await SeedGameAsync(genreId, "Celeste");
        await SeedGameAsync(genreId, "Hades");
        await SeedGameAsync(genreId, "Hidden Gem", isActive: false);

        var body = await GetGamesAsync("/api/games");

        Assert.Equal(2, body.TotalCount);
        Assert.Equal(2, body.Items.Count);
        Assert.DoesNotContain(body.Items, item => item.Name == "Hidden Gem");
        Assert.Contains(body.Items, item => item.Name == "Celeste");
        Assert.Contains(body.Items, item => item.Name == "Hades");
    }

    [Fact]
    public async Task GetGames_PaginatesAndReturnsMetadata()
    {
        var genreId = await SeedGenreAsync();
        await SeedGameAsync(genreId, "Animal Crossing");
        await SeedGameAsync(genreId, "Celeste");
        await SeedGameAsync(genreId, "Hades");
        await SeedGameAsync(genreId, "Portal");
        await SeedGameAsync(genreId, "Zelda");

        var body = await GetGamesAsync("/api/games?page=2&pageSize=2");

        Assert.Equal(2, body.Page);
        Assert.Equal(2, body.PageSize);
        Assert.Equal(5, body.TotalCount);
        Assert.Equal(3, body.TotalPages);
        Assert.Equal(2, body.Items.Count);
        Assert.Equal("Hades", body.Items[0].Name);
        Assert.Equal("Portal", body.Items[1].Name);
    }

    [Fact]
    public async Task GetGames_SearchIsCaseInsensitiveAndFiltersByName()
    {
        var genreId = await SeedGenreAsync();
        await SeedGameAsync(genreId, "Super Mario Bros. 3");
        await SeedGameAsync(genreId, "Mario Kart 8");
        await SeedGameAsync(genreId, "The Legend of Zelda");

        var lower = await GetGamesAsync("/api/games?search=mario");
        var upper = await GetGamesAsync("/api/games?search=MARIO");

        Assert.Equal(2, lower.TotalCount);
        Assert.Equal(2, upper.TotalCount);
        Assert.DoesNotContain(lower.Items, item => item.Name == "The Legend of Zelda");
        Assert.Equal(lower.Items.Select(item => item.Name), upper.Items.Select(item => item.Name));
    }

    [Fact]
    public async Task GetGames_EmptyOrWhitespaceSearch_DoesNotFilter()
    {
        var genreId = await SeedGenreAsync();
        await SeedGameAsync(genreId, "Celeste");
        await SeedGameAsync(genreId, "Hades");

        var omitted = await GetGamesAsync("/api/games");
        var empty = await GetGamesAsync("/api/games?search=");
        var whitespace = await GetGamesAsync("/api/games?search=%20");

        Assert.Equal(2, omitted.TotalCount);
        Assert.Equal(2, empty.TotalCount);
        Assert.Equal(2, whitespace.TotalCount);
    }

    [Fact]
    public async Task GetGames_PageBeyondLast_ReturnsEmptyItems()
    {
        var genreId = await SeedGenreAsync();
        await SeedGameAsync(genreId, "Celeste");
        await SeedGameAsync(genreId, "Hades");

        var response = await _client.GetAsync("/api/games?page=10&pageSize=12");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetGamesResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(10, body.Page);
        Assert.Equal(12, body.PageSize);
        Assert.Equal(2, body.TotalCount);
        Assert.Equal(1, body.TotalPages);
        Assert.Empty(body.Items);
    }

    [Fact]
    public async Task GetGames_OrdersByNameThenId()
    {
        var genreId = await SeedGenreAsync();
        await SeedGameAsync(genreId, "Zelda");
        await SeedGameAsync(genreId, "Animal Crossing");
        await SeedGameAsync(genreId, "Celeste");

        var body = await GetGamesAsync("/api/games");

        Assert.Equal(
            new[] { "Animal Crossing", "Celeste", "Zelda" },
            body.Items.Select(item => item.Name).ToArray());
    }

    [Fact]
    public async Task GetGames_ResponseContainsExpectedItemFields()
    {
        var genreId = await SeedGenreAsync();
        var game = await SeedGameAsync(
            genreId,
            "Celeste",
            description: "A mountain-climbing platformer.",
            price: 19.99m,
            imageUrl: "https://upload.wikimedia.org/wikipedia/commons/0/0f/Celeste_box_art_full.png");

        var body = await GetGamesAsync("/api/games");

        Assert.Equal(1, body.Page);
        Assert.Equal(12, body.PageSize);
        var item = Assert.Single(body.Items);
        Assert.Equal(game.Id, item.Id);
        Assert.Equal("Celeste", item.Name);
        Assert.Equal("A mountain-climbing platformer.", item.Description);
        Assert.Equal(19.99m, item.Price);
        Assert.Equal(game.ImageUrl, item.ImageUrl);
        Assert.Equal(genreId, item.GenreId);
        Assert.Equal("Platformer", item.GenreName);
        Assert.True(item.IsActive);
    }

    [Fact]
    public async Task GetGames_IncludeInactive_WithoutAdmin_StillReturnsOnlyActive()
    {
        var genreId = await SeedGenreAsync();
        await SeedGameAsync(genreId, "Celeste");
        await SeedGameAsync(genreId, "Hidden Gem", isActive: false);

        var body = await GetGamesAsync("/api/games?includeInactive=true");

        Assert.Equal(1, body.TotalCount);
        Assert.DoesNotContain(body.Items, item => item.Name == "Hidden Gem");
    }

    [Fact]
    public async Task GetGames_IncludeInactive_AsAdmin_ReturnsActiveAndInactive()
    {
        var genreId = await SeedGenreAsync();
        await SeedGameAsync(genreId, "Celeste");
        await SeedGameAsync(genreId, "Hidden Gem", isActive: false);

        var admin = _factory.CreateAdminClient();
        var response = await admin.GetAsync("/api/games?includeInactive=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetGamesResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(2, body.TotalCount);
        Assert.Contains(body.Items, item => item.Name == "Celeste" && item.IsActive);
        Assert.Contains(body.Items, item => item.Name == "Hidden Gem" && !item.IsActive);
    }

    [Fact]
    public async Task GetGames_StatusFirstInactive_AsAdmin_OrdersDisabledFirst()
    {
        var genreId = await SeedGenreAsync();
        await SeedGameAsync(genreId, "Active Game");
        await SeedGameAsync(genreId, "Disabled Game", isActive: false);

        var admin = _factory.CreateAdminClient();
        var response = await admin.GetAsync("/api/games?includeInactive=true&statusFirst=inactive");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetGamesResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(2, body.Items.Count);
        Assert.False(body.Items[0].IsActive);
        Assert.True(body.Items[1].IsActive);
    }

    [Fact]
    public async Task GetGames_StatusFirstActive_AsAdmin_OrdersActiveFirst()
    {
        var genreId = await SeedGenreAsync();
        await SeedGameAsync(genreId, "Active Game");
        await SeedGameAsync(genreId, "Disabled Game", isActive: false);

        var admin = _factory.CreateAdminClient();
        var response = await admin.GetAsync("/api/games?includeInactive=true&statusFirst=active");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetGamesResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(2, body.Items.Count);
        Assert.True(body.Items[0].IsActive);
        Assert.False(body.Items[1].IsActive);
    }

    [Fact]
    public async Task GetGames_InvalidPagination_ReturnsValidationProblem()
    {
        var pageZero = await _client.GetAsync("/api/games?page=0");
        var pageSizeTooLarge = await _client.GetAsync("/api/games?pageSize=51");

        Assert.Equal(HttpStatusCode.BadRequest, pageZero.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, pageSizeTooLarge.StatusCode);
    }

    private async Task<GetGamesResponse> GetGamesAsync(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetGamesResponse>(JsonOptions);
        Assert.NotNull(body);
        return body;
    }

    private async Task<int> SeedGenreAsync(string name = "Platformer")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        await db.Database.EnsureCreatedAsync();

        var genre = new Genre { Name = name };
        db.Genres.Add(genre);
        await db.SaveChangesAsync();
        return genre.Id;
    }

    private async Task<Game> SeedGameAsync(
        int genreId,
        string name,
        bool isActive = true,
        string description = "A video game.",
        decimal price = 19.99m,
        string? imageUrl = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        await db.Database.EnsureCreatedAsync();

        var now = DateTimeOffset.UtcNow;
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price,
            ImageUrl = imageUrl,
            GenreId = genreId,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = isActive
        };

        db.Games.Add(game);
        await db.SaveChangesAsync();
        return game;
    }
}
