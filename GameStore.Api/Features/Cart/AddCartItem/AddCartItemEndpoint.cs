using GameStore.Api.Authentication;
using GameStore.Api.Domain.Carts;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Cart.AddCartItem;

public static class AddCartItemEndpoint
{
    public static RouteHandlerBuilder MapAddCartItem(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/api/cart/items", HandleAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        AddCartItemRequest request,
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

        if (request.GameId == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["GameId"] = ["GameId is required."]
            });
        }

        var game = await db.Games.FindAsync([request.GameId], cancellationToken);
        if (game is null)
        {
            return Results.Problem(
                detail: $"Game '{request.GameId}' was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        if (!game.IsActive)
        {
            return Results.Problem(
                detail: $"Game '{request.GameId}' is not available.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var cart = await db.Carts
            .Include(existing => existing.Items)
            .FirstOrDefaultAsync(existing => existing.UserId == userId, cancellationToken);

        if (cart is null)
        {
            cart = new GameStore.Api.Domain.Carts.Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };
            db.Carts.Add(cart);
        }

        var item = cart.Items.FirstOrDefault(existing => existing.GameId == request.GameId);
        if (item is null)
        {
            cart.Items.Add(new CartItem
            {
                CartId = cart.Id,
                GameId = request.GameId,
                Quantity = 1
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        var response = await CartResponseFactory.CreateAsync(db, cart.Id, cancellationToken);
        return Results.Ok(response);
    }
}
