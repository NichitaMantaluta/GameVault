using GameStore.Api.Authentication;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Orders.GetOrders;

public static class GetOrdersEndpoint
{
    public static RouteHandlerBuilder MapGetOrders(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/api/orders", HandleAsync)
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

        var items = await db.Orders
            .AsNoTracking()
            .Where(order => order.UserId == userId)
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Select(order => new GetOrdersItem(
                order.Id,
                order.Status,
                order.TotalAmount,
                order.Currency,
                order.CreatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(new GetOrdersResponse(items));
    }
}
