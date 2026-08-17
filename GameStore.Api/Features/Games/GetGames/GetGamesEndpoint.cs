using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Games.GetGames;

public static class GetGamesEndpoint
{
    internal const int DefaultPage = 1;
    internal const int DefaultPageSize = 12;
    internal const int MaxPageSize = 50;

    public static RouteHandlerBuilder MapGetGames(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/api/games", HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        GameStoreDbContext db,
        CancellationToken cancellationToken,
        int? page = null,
        int? pageSize = null,
        string? search = null)
    {
        var resolvedPage = page ?? DefaultPage;
        var resolvedPageSize = pageSize ?? DefaultPageSize;

        var validationErrors = Validate(resolvedPage, resolvedPageSize);
        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var gamesQuery = db.Games
            .AsNoTracking()
            .Where(game => game.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            gamesQuery = gamesQuery.Where(game => game.Name.ToLower().Contains(term));
        }

        var totalCount = await gamesQuery.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)resolvedPageSize);

        var items = await (
                from game in gamesQuery
                join genre in db.Genres.AsNoTracking() on game.GenreId equals genre.Id
                orderby game.Name, game.Id
                select new GetGamesItem(
                    game.Id,
                    game.Name,
                    game.Description,
                    game.Price,
                    game.ImageUrl,
                    game.GenreId,
                    genre.Name))
            .Skip((resolvedPage - 1) * resolvedPageSize)
            .Take(resolvedPageSize)
            .ToListAsync(cancellationToken);

        return Results.Ok(new GetGamesResponse(
            items,
            resolvedPage,
            resolvedPageSize,
            totalCount,
            totalPages));
    }

    private static Dictionary<string, string[]> Validate(int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();

        if (page < 1)
        {
            errors["page"] = ["Page must be greater than or equal to 1."];
        }

        if (pageSize < 1)
        {
            errors["pageSize"] = ["PageSize must be greater than or equal to 1."];
        }
        else if (pageSize > MaxPageSize)
        {
            errors["pageSize"] = [$"PageSize must be {MaxPageSize} or fewer."];
        }

        return errors;
    }
}
