using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Games.UpdateGame;

public static class UpdateGameEndpoint
{
    // Keep in sync with GameStoreDbContext Game property max lengths.
    private const int NameMaxLength = 200;
    private const int DescriptionMaxLength = 4000;
    private const int ImageUrlMaxLength = 2048;

    public static RouteHandlerBuilder MapUpdateGame(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/api/games/{id:guid}", HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateGameRequest request,
        GameStoreDbContext db,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);
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
        game.ImageUrl = NormalizeImageUrl(request.ImageUrl);
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

    private static Dictionary<string, string[]> Validate(UpdateGameRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["Name"] = ["Name is required."];
        }
        else if (request.Name.Trim().Length > NameMaxLength)
        {
            errors["Name"] = [$"Name must be {NameMaxLength} characters or fewer."];
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors["Description"] = ["Description is required."];
        }
        else if (request.Description.Trim().Length > DescriptionMaxLength)
        {
            errors["Description"] = [$"Description must be {DescriptionMaxLength} characters or fewer."];
        }

        if (request.Price < 0)
        {
            errors["Price"] = ["Price must be greater than or equal to 0."];
        }

        if (request.GenreId <= 0)
        {
            errors["GenreId"] = ["GenreId is required."];
        }

        var imageUrl = NormalizeImageUrl(request.ImageUrl);
        if (imageUrl is not null && imageUrl.Length > ImageUrlMaxLength)
        {
            errors["ImageUrl"] = [$"ImageUrl must be {ImageUrlMaxLength} characters or fewer."];
        }

        return errors;
    }

    private static string? NormalizeImageUrl(string? imageUrl)
    {
        return string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
    }
}
