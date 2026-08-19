using GameStore.Api.Authentication;
using GameStore.Api.Persistence;

namespace GameStore.Api.Features.Games.DeleteGame;

public static class DeleteGameEndpoint
{
    public static RouteHandlerBuilder MapDeleteGame(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/api/games/{id:guid}", HandleAsync)
            .RequireAuthorization(AuthorizationPolicies.Admin);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GameStoreDbContext db,
        CancellationToken cancellationToken)
    {
        var game = await db.Games.FindAsync([id], cancellationToken);
        if (game is null)
        {
            return Results.Problem(
                detail: $"Game '{id}' was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        if (game.IsActive)
        {
            game.IsActive = false;
            game.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Results.NoContent();
    }
}
