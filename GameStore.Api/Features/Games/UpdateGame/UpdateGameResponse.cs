namespace GameStore.Api.Features.Games.UpdateGame;

public record UpdateGameResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string? ImageUrl,
    int GenreId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsActive);
