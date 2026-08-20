using GameStore.Api.Authentication;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Orders.GetOrder;

public static class GetOrderEndpoint
{
    public static RouteHandlerBuilder MapGetOrder(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/api/orders/{id:guid}", HandleAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
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

        var order = await db.Orders
            .AsNoTracking()
            .Where(existing => existing.Id == id && existing.UserId == userId)
            .Select(existing => new GetOrderResponse(
                existing.Id,
                existing.Status,
                existing.TotalAmount,
                existing.Currency,
                existing.CreatedAt,
                existing.Items
                    .Select(item => new GetOrderItemResponse(item.GameId, item.GameName, item.Price))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            return Results.Problem(
                detail: $"Order '{id}' was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Ok(order);
    }
}
