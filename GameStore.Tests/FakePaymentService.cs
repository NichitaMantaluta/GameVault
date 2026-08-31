using GameStore.Api.Integrations.Payments;

namespace GameStore.Tests;

public sealed class FakePaymentService : IPaymentService
{
    public CreateCheckoutSessionRequest? LastRequest { get; set; }

    public bool ShouldFail { get; set; }

    public Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (ShouldFail)
        {
            throw new InvalidOperationException("Payment provider unavailable.");
        }

        LastRequest = request;
        var sessionId = $"cs_test_{request.OrderId:N}";
        return Task.FromResult(new CheckoutSessionResult(
            sessionId,
            $"https://checkout.test/pay/{sessionId}"));
    }
}
