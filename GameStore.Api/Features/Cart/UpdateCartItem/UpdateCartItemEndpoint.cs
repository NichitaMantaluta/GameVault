using GameStore.Api.Authentication;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Cart.UpdateCartItem;

public static class UpdateCartItemEndpoint
{
    public static RouteHandlerBuilder MapUpdateCartItem(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPatch("/api/cart/items/{gameId:guid}", HandleAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid gameId,
        UpdateCartItemRequest request,
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

        if (request.Quantity < 1)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Quantity"] = ["Quantity must be greater than or equal to 1."]
            });
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

        item.Quantity = request.Quantity;
        await db.SaveChangesAsync(cancellationToken);

        var response = await CartResponseFactory.CreateAsync(db, cart.Id, cancellationToken);
        return Results.Ok(response);
    }
}
