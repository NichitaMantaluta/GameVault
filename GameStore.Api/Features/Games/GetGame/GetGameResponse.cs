namespace GameStore.Api.Features.Games.GetGame;

public record GetGameResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string? ImageUrl,
    int GenreId,
    string GenreName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsActive);
