namespace GameStore.Api.Features.Games.GetGames;

public record GetGamesItem(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string? ImageUrl,
    int GenreId,
    string GenreName,
    bool IsActive);

public record GetGamesResponse(
    IReadOnlyList<GetGamesItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
