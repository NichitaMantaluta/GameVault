using GameStore.Api.Authentication;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Games.UpdateGame;

public static class UpdateGameEndpoint
{
    public static RouteHandlerBuilder MapUpdateGame(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/api/games/{id:guid}", HandleAsync)
            .RequireAuthorization(AuthorizationPolicies.Admin);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateGameRequest request,
        GameStoreDbContext db,
        CancellationToken cancellationToken)
    {
        var validationErrors = GameMutationValidator.Validate(
            request.Name,
            request.Description,
            request.Price,
            request.GenreId,
            request.ImageUrl);
        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var game = await db.Games.FindAsync([id], cancellationToken);
        if (game is null)
        {
            return Results.Problem(
                detail: $"Game '{id}' was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var genreExists = await db.Genres
            .AnyAsync(genre => genre.Id == request.GenreId, cancellationToken);

        if (!genreExists)
        {
            return Results.Problem(
                detail: $"Genre '{request.GenreId}' was not found.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        game.Name = request.Name.Trim();
        game.Description = request.Description.Trim();
        game.Price = request.Price;
        game.ImageUrl = GameMutationValidator.NormalizeImageUrl(request.ImageUrl);
        game.GenreId = request.GenreId;
        game.IsActive = request.IsActive;
        game.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var response = new UpdateGameResponse(
            game.Id,
            game.Name,
            game.Description,
            game.Price,
            game.ImageUrl,
            game.GenreId,
            game.CreatedAt,
            game.UpdatedAt,
            game.IsActive);

        return Results.Ok(response);
    }
}
