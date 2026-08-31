using GameStore.Api.Authentication;
using GameStore.Api.Domain.Orders;
using GameStore.Api.Integrations.Payments;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GameStore.Api.Features.Orders.CreateOrder;

public static class CreateOrderEndpoint
{
    internal const string Currency = "USD";

    public static RouteHandlerBuilder MapCreateOrder(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/api/orders", HandleAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        GameStoreDbContext db,
        IPaymentService paymentService,
        IOptions<StripeOptions> stripeOptions,
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

        if (cart is null || cart.Items.Count == 0)
        {
            return Results.Problem(
                detail: "The cart is empty.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var gameIds = cart.Items.Select(item => item.GameId).Distinct().ToList();
        var games = await db.Games
            .AsNoTracking()
            .Where(game => gameIds.Contains(game.Id))
            .ToDictionaryAsync(game => game.Id, cancellationToken);

        foreach (var cartItem in cart.Items)
        {
            if (!games.TryGetValue(cartItem.GameId, out var game))
            {
                return Results.Problem(
                    detail: $"Game '{cartItem.GameId}' was not found.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (!game.IsActive)
            {
                return Results.Problem(
                    detail: $"Game '{cartItem.GameId}' is not available.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }

        var ownedGameIds = await OwnedGamesQuery.GetOwnedGameIdsAsync(db, userId, cancellationToken);

        var alreadyOwned = cart.Items.FirstOrDefault(item => ownedGameIds.Contains(item.GameId));
        if (alreadyOwned is not null)
        {
            return Results.Problem(
                detail: $"Game '{games[alreadyOwned.GameId].Name}' is already owned.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var pendingOrders = await db.Orders
            .Include(order => order.Items)
            .Where(order => order.UserId == userId && order.Status == OrderStatus.Pending)
            .ToListAsync(cancellationToken);
        if (pendingOrders.Count > 0)
        {
            db.Orders.RemoveRange(pendingOrders);
        }

        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatus.Pending,
            Currency = Currency,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var cartItem in cart.Items)
        {
            var game = games[cartItem.GameId];
            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                UserId = userId,
                GameId = game.Id,
                GameName = game.Name,
                Price = game.Price
            });
        }

        order.TotalAmount = order.Items.Sum(item => item.Price);

        var options = stripeOptions.Value;
        var successUrl = options.SuccessUrl.Replace(
            "{ORDER_ID}",
            order.Id.ToString(),
            StringComparison.Ordinal);
        var cancelUrl = options.CancelUrl.Replace(
            "{ORDER_ID}",
            order.Id.ToString(),
            StringComparison.Ordinal);

        CheckoutSessionResult checkout;
        try
        {
            checkout = await paymentService.CreateCheckoutSessionAsync(
                new CreateCheckoutSessionRequest(
                    order.Id,
                    order.Currency,
                    order.Items
                        .Select(item => new CheckoutLineItem(item.GameName, item.Price))
                        .ToList(),
                    successUrl,
                    cancelUrl),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Results.Problem(
                detail: "Unable to create a payment checkout session.",
                statusCode: StatusCodes.Status502BadGateway);
        }

        order.StripeCheckoutSessionId = checkout.SessionId;
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        var response = new CreateOrderResponse(
            order.Id,
            order.Status,
            order.TotalAmount,
            order.Currency,
            order.CreatedAt,
            checkout.CheckoutUrl,
            order.Items
                .Select(item => new CreateOrderItemResponse(item.GameId, item.GameName, item.Price))
                .ToList());

        return Results.Created($"/api/orders/{order.Id}", response);
    }
}
