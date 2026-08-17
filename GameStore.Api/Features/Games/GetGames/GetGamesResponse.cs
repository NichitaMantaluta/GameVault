namespace GameStore.Api.Features.Games.GetGames;

public record GetGamesItem(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int GenreId,
    string GenreName);

public record GetGamesResponse(
    IReadOnlyList<GetGamesItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
