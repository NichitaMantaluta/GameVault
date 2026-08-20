using GameStore.Api.Domain.Orders;
using GameStore.Api.Integrations.Payments;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace GameStore.Api.Features.Orders.StripeWebhook;

public static class StripeWebhookEndpoint
{
    public static RouteHandlerBuilder MapStripeWebhook(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/api/payments/stripe/webhook", HandleAsync)
            .AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        GameStoreDbContext db,
        IOptions<StripeOptions> stripeOptions,
        CancellationToken cancellationToken)
    {
        var webhookSecret = stripeOptions.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            return Results.Problem(
                detail: "Stripe:WebhookSecret is not configured.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        string payload;
        using (var reader = new StreamReader(httpContext.Request.Body))
        {
            payload = await reader.ReadToEndAsync(cancellationToken);
        }

        var signatureHeader = httpContext.Request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return Results.Problem(
                detail: "Missing Stripe-Signature header.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                payload,
                signatureHeader,
                webhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (Exception)
        {
            return Results.Problem(
                detail: "Invalid Stripe webhook signature.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (stripeEvent.Type != EventTypes.CheckoutSessionCompleted)
        {
            return Results.Ok();
        }

        if (stripeEvent.Data.Object is not Session session)
        {
            return Results.Problem(
                detail: "Checkout session payload was missing.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!TryGetOrderId(session, out var orderId))
        {
            return Results.Problem(
                detail: "Checkout session did not include a correlating order id.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var order = await db.Orders
            .Include(existing => existing.Items)
            .FirstOrDefaultAsync(existing => existing.Id == orderId, cancellationToken);

        if (order is null)
        {
            return Results.Problem(
                detail: $"Order '{orderId}' was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        if (!string.IsNullOrWhiteSpace(order.StripeCheckoutSessionId) &&
            !string.Equals(order.StripeCheckoutSessionId, session.Id, StringComparison.Ordinal))
        {
            return Results.Problem(
                detail: "Checkout session does not match the order.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (order.Status == OrderStatus.Completed)
        {
            return Results.Ok();
        }

        order.Status = OrderStatus.Completed;
        order.StripeCheckoutSessionId ??= session.Id;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        var cart = await db.Carts
            .Include(existing => existing.Items)
            .FirstOrDefaultAsync(existing => existing.UserId == order.UserId, cancellationToken);

        if (cart is not null)
        {
            var purchasedGameIds = order.Items.Select(item => item.GameId).ToHashSet();
            cart.Items.RemoveAll(item => purchasedGameIds.Contains(item.GameId));
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok();
    }

    private static bool TryGetOrderId(Session session, out Guid orderId)
    {
        if (!string.IsNullOrWhiteSpace(session.ClientReferenceId) &&
            Guid.TryParse(session.ClientReferenceId, out orderId))
        {
            return true;
        }

        if (session.Metadata is not null &&
            session.Metadata.TryGetValue("orderId", out var metadataOrderId) &&
            Guid.TryParse(metadataOrderId, out orderId))
        {
            return true;
        }

        orderId = Guid.Empty;
        return false;
    }
}
