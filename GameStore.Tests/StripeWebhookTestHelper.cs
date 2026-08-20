using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace GameStore.Tests;

internal static class StripeWebhookTestHelper
{
    public static async Task<HttpResponseMessage> PostCheckoutSessionCompletedAsync(
        HttpClient client,
        Guid orderId,
        string sessionId,
        string webhookSecret,
        bool includeOrderCorrelation = true)
    {
        var payload = BuildCheckoutSessionCompletedPayload(orderId, sessionId, includeOrderCorrelation);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/stripe/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", CreateSignatureHeader(payload, webhookSecret));
        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> PostRawAsync(
        HttpClient client,
        string payload,
        string? signatureHeader)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/stripe/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        if (signatureHeader is not null)
        {
            request.Headers.TryAddWithoutValidation("Stripe-Signature", signatureHeader);
        }

        return await client.SendAsync(request);
    }

    public static string CreateSignatureHeader(string payload, string webhookSecret, long? timestamp = null)
    {
        var unixTimestamp = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{unixTimestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var signature = Convert.ToHexString(hash).ToLowerInvariant();
        return $"t={unixTimestamp},v1={signature}";
    }

    public static string BuildCheckoutSessionCompletedPayload(
        Guid orderId,
        string sessionId,
        bool includeOrderCorrelation = true)
    {
        var orderIdValue = orderId.ToString();
        var clientReference = includeOrderCorrelation
            ? $"\"client_reference_id\": \"{orderIdValue}\","
            : "\"client_reference_id\": null,";
        var metadata = includeOrderCorrelation
            ? $"\"metadata\": {{ \"orderId\": \"{orderIdValue}\" }},"
            : "\"metadata\": {},";

        return $$"""
            {
              "id": "evt_test_{{sessionId}}",
              "object": "event",
              "api_version": "2024-06-20",
              "created": {{DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}},
              "type": "checkout.session.completed",
              "livemode": false,
              "pending_webhooks": 1,
              "request": { "id": null, "idempotency_key": null },
              "data": {
                "object": {
                  "id": "{{sessionId}}",
                  "object": "checkout.session",
                  {{clientReference}}
                  {{metadata}}
                  "mode": "payment",
                  "payment_status": "paid",
                  "status": "complete",
                  "currency": "usd",
                  "amount_total": 1999
                }
              }
            }
            """;
    }
}
