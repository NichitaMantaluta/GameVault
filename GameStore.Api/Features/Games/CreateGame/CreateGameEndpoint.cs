using GameStore.Api.Authentication;
using GameStore.Api.Domain.Games;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Games.CreateGame;

public static class CreateGameEndpoint
{
    public static RouteHandlerBuilder MapCreateGame(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/api/games", HandleAsync)
            .RequireAuthorization(AuthorizationPolicies.Admin);
    }

    private static async Task<IResult> HandleAsync(
        CreateGameRequest request,
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

        var genreExists = await db.Genres
            .AnyAsync(genre => genre.Id == request.GenreId, cancellationToken);

        if (!genreExists)
        {
            return Results.Problem(
                detail: $"Genre '{request.GenreId}' was not found.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var now = DateTimeOffset.UtcNow;
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            ImageUrl = GameMutationValidator.NormalizeImageUrl(request.ImageUrl),
            GenreId = request.GenreId,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true
        };

        db.Games.Add(game);
        await db.SaveChangesAsync(cancellationToken);

        var response = new CreateGameResponse(
            game.Id,
            game.Name,
            game.Description,
            game.Price,
            game.ImageUrl,
            game.GenreId,
            game.CreatedAt,
            game.UpdatedAt,
            game.IsActive);

        return Results.Created($"/api/games/{game.Id}", response);
    }
}
