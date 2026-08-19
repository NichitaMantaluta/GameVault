using GameStore.Api.Authentication;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Cart.RemoveCartItem;

public static class RemoveCartItemEndpoint
{
    public static RouteHandlerBuilder MapRemoveCartItem(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/api/cart/items/{gameId:guid}", HandleAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid gameId,
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

        var cart = await db.Carts
            .Include(existing => existing.Items)
            .FirstOrDefaultAsync(existing => existing.UserId == userId, cancellationToken);

        var item = cart?.Items.FirstOrDefault(existing => existing.GameId == gameId);
        if (cart is null || item is null)
        {
            return Results.Problem(
                detail: $"Cart item for game '{gameId}' was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        db.CartItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
