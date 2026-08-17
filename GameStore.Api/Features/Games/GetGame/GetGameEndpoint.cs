using GameStore.Api.Persistence;

namespace GameStore.Api.Features.Games.GetGame;

public static class GetGameEndpoint
{
    public static RouteHandlerBuilder MapGetGame(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/api/games/{id:guid}", HandleAsync);
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

        var response = new GetGameResponse(
            game.Id,
            game.Name,
            game.Description,
            game.Price,
            game.GenreId,
            game.CreatedAt,
            game.UpdatedAt,
            game.IsActive);

        return Results.Ok(response);
    }
}
