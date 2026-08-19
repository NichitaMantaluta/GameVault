using GameStore.Api.Authentication;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Cart.GetCart;

public static class GetCartEndpoint
{
    public static RouteHandlerBuilder MapGetCart(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/api/cart", HandleAsync)
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

        var cartId = await db.Carts
            .AsNoTracking()
            .Where(cart => cart.UserId == userId)
            .Select(cart => (Guid?)cart.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (cartId is null)
        {
            return Results.Ok(CartResponseFactory.Empty);
        }

        var response = await CartResponseFactory.CreateAsync(db, cartId.Value, cancellationToken);
        return Results.Ok(response);
    }
}
