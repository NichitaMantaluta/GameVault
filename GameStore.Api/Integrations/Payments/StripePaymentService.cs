using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace GameStore.Api.Integrations.Payments;

public sealed class StripePaymentService(IOptions<StripeOptions> options) : IPaymentService
{
    private readonly StripeOptions _options = options.Value;

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new InvalidOperationException("Stripe:SecretKey is not configured.");
        }

        var client = new StripeClient(_options.SecretKey);
        var sessionService = new SessionService(client);

        var lineItems = request.LineItems.Select(item => new SessionLineItemOptions
        {
            Quantity = 1,
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = request.Currency.ToLowerInvariant(),
                UnitAmount = ToStripeUnitAmount(item.UnitAmount),
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = item.Name
                }
            }
        }).ToList();

        var session = await sessionService.CreateAsync(
            new SessionCreateOptions
            {
                Mode = "payment",
                ClientReferenceId = request.OrderId.ToString(),
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                LineItems = lineItems,
                Metadata = new Dictionary<string, string>
                {
                    ["orderId"] = request.OrderId.ToString()
                }
            },
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException("Stripe Checkout Session did not return a URL.");
        }

        return new CheckoutSessionResult(session.Id, session.Url);
    }

    internal static long ToStripeUnitAmount(decimal amount)
    {
        return decimal.ToInt64(decimal.Round(amount * 100m, MidpointRounding.AwayFromZero));
    }
}
