namespace GameStore.Api.Features.Games.CreateGame;

public record CreateGameResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int GenreId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsActive);
