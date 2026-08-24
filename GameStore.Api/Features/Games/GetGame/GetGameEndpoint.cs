using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;

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
        var response = await (
                from game in db.Games.AsNoTracking()
                join genre in db.Genres.AsNoTracking() on game.GenreId equals genre.Id
                where game.Id == id
                select new GetGameResponse(
                    game.Id,
                    game.Name,
                    game.Description,
                    game.Price,
                    game.ImageUrl,
                    game.GenreId,
                    genre.Name,
                    game.CreatedAt,
                    game.UpdatedAt,
                    game.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return Results.Problem(
                detail: $"Game '{id}' was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Ok(response);
    }
}
