namespace GameStore.Api.Integrations.Payments;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = "http://localhost:5173/checkout/success?orderId={ORDER_ID}";
    public string CancelUrl { get; set; } = "http://localhost:5173/checkout/cancel";
}
