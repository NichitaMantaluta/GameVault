namespace GameStore.Api.Integrations.Payments;

public record CheckoutLineItem(string Name, decimal UnitAmount);

public record CreateCheckoutSessionRequest(
    Guid OrderId,
    string Currency,
    IReadOnlyList<CheckoutLineItem> LineItems,
    string SuccessUrl,
    string CancelUrl);

public record CheckoutSessionResult(string SessionId, string CheckoutUrl);

public interface IPaymentService
{
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken);
}
