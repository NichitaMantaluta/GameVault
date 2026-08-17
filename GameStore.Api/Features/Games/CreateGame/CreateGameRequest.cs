namespace GameStore.Api.Features.Games.CreateGame;

public record CreateGameRequest(
    string Name,
    string Description,
    decimal Price,
    int GenreId,
    string? ImageUrl);
