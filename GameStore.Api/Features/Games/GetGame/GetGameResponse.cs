namespace GameStore.Api.Features.Games.GetGame;

public record GetGameResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int GenreId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsActive);
