using GameStore.Api.Authentication;
using GameStore.Api.Persistence;

namespace GameStore.Api.Features.Orders.GetOwnedGames;

public static class GetOwnedGamesEndpoint
{
    public static RouteHandlerBuilder MapGetOwnedGames(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/api/orders/owned-games", HandleAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        GameStoreDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUser.GetId(httpContext.User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Problem(
                detail: "The access token was accepted but did not contain a user id.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var gameIds = await OwnedGamesQuery.GetOwnedGameIdsAsync(db, userId, cancellationToken);
        return Results.Ok(new GetOwnedGamesResponse(gameIds.ToList()));
    }
}
