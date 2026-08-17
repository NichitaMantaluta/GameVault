namespace GameStore.Api.Features.Games.UpdateGame;

public record UpdateGameRequest(
    string Name,
    string Description,
    decimal Price,
    int GenreId,
    bool IsActive);
