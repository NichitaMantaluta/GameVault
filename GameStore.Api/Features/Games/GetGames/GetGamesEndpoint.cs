using GameStore.Api.Authentication;
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
        HttpContext httpContext,
        GameStoreDbContext db,
        CancellationToken cancellationToken,
        int? page = null,
        int? pageSize = null,
        string? search = null,
        bool? includeInactive = null,
        string? statusFirst = null)
    {
        var resolvedPage = page ?? DefaultPage;
        var resolvedPageSize = pageSize ?? DefaultPageSize;

        var validationErrors = Validate(resolvedPage, resolvedPageSize, statusFirst);
        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var isAdmin = httpContext.User.IsInRole(AuthorizationPolicies.AdminRole)
            || httpContext.User.HasClaim("role", AuthorizationPolicies.AdminRole);
        var includeInactiveGames = includeInactive == true && isAdmin;
        var inactiveFirst = includeInactiveGames
            && string.Equals(statusFirst, "inactive", StringComparison.OrdinalIgnoreCase);

        var gamesQuery = db.Games.AsNoTracking();

        if (!includeInactiveGames)
        {
            gamesQuery = gamesQuery.Where(game => game.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            gamesQuery = gamesQuery.Where(game => game.Name.ToLower().Contains(term));
        }

        var totalCount = await gamesQuery.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)resolvedPageSize);

        var joinedQuery =
            from game in gamesQuery
            join genre in db.Genres.AsNoTracking() on game.GenreId equals genre.Id
            select new { game, genre.Name };

        var orderedQuery = includeInactiveGames
            ? inactiveFirst
                ? joinedQuery.OrderBy(row => row.game.IsActive).ThenBy(row => row.game.Name).ThenBy(row => row.game.Id)
                : joinedQuery.OrderByDescending(row => row.game.IsActive).ThenBy(row => row.game.Name).ThenBy(row => row.game.Id)
            : joinedQuery.OrderBy(row => row.game.Name).ThenBy(row => row.game.Id);

        var items = await orderedQuery
            .Skip((resolvedPage - 1) * resolvedPageSize)
            .Take(resolvedPageSize)
            .Select(row => new GetGamesItem(
                row.game.Id,
                row.game.Name,
                row.game.Description,
                row.game.Price,
                row.game.ImageUrl,
                row.game.GenreId,
                row.Name,
                row.game.IsActive))
            .ToListAsync(cancellationToken);

        return Results.Ok(new GetGamesResponse(
            items,
            resolvedPage,
            resolvedPageSize,
            totalCount,
            totalPages));
    }

    private static Dictionary<string, string[]> Validate(int page, int pageSize, string? statusFirst)
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

        if (!string.IsNullOrWhiteSpace(statusFirst)
            && !string.Equals(statusFirst, "active", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(statusFirst, "inactive", StringComparison.OrdinalIgnoreCase))
        {
            errors["statusFirst"] = ["statusFirst must be 'active' or 'inactive'."];
        }

        return errors;
    }
}
