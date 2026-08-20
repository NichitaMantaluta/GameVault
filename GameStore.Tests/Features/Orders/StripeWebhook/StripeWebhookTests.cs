using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameStore.Api.Domain.Orders;
using GameStore.Api.Features.Cart.GetCart;
using GameStore.Api.Features.Orders.CreateOrder;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Tests.Features.Orders.StripeWebhook;

public class StripeWebhookTests : IClassFixture<GameStoreApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly GameStoreApiFactory _factory;

    public StripeWebhookTests(GameStoreApiFactory factory)
    {
        _factory = factory;
        _factory.PaymentService.ShouldFail = false;
    }

    [Fact]
    public async Task StripeWebhook_WithValidCheckoutCompleted_CompletesOrderAndClearsCart()
    {
        var (client, anonymous, created, gameId) = await CreatePendingOrderAsync();

        var response = await StripeWebhookTestHelper.PostCheckoutSessionCompletedAsync(
            anonymous,
            created.Id,
            SessionIdFrom(created),
            GameStoreApiFactory.StripeWebhookSecret);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        var order = await db.Orders.SingleAsync(existing => existing.Id == created.Id);
        Assert.Equal(OrderStatus.Completed, order.Status);

        var cart = await client.GetFromJsonAsync<GetCartResponse>("/api/cart", JsonOptions);
        Assert.NotNull(cart);
        Assert.Empty(cart.Items);
        Assert.False(await db.CartItems.AnyAsync(item => item.GameId == gameId));
    }

    [Fact]
    public async Task StripeWebhook_WithInvalidSignature_ReturnsBadRequest()
    {
        var (_, anonymous, created, _) = await CreatePendingOrderAsync();
        var payload = StripeWebhookTestHelper.BuildCheckoutSessionCompletedPayload(
            created.Id,
            SessionIdFrom(created));

        var response = await StripeWebhookTestHelper.PostRawAsync(
            anonymous,
            payload,
            StripeWebhookTestHelper.CreateSignatureHeader(payload, "whsec_wrong_secret"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertOrderStillPendingAsync(created.Id);
    }

    [Fact]
    public async Task StripeWebhook_DuplicateDelivery_IsIdempotent()
    {
        var (_, anonymous, created, _) = await CreatePendingOrderAsync();
        var sessionId = SessionIdFrom(created);

        var first = await StripeWebhookTestHelper.PostCheckoutSessionCompletedAsync(
            anonymous,
            created.Id,
            sessionId,
            GameStoreApiFactory.StripeWebhookSecret);
        var second = await StripeWebhookTestHelper.PostCheckoutSessionCompletedAsync(
            anonymous,
            created.Id,
            sessionId,
            GameStoreApiFactory.StripeWebhookSecret);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        Assert.Equal(1, await db.Orders.CountAsync(order => order.Id == created.Id));
        Assert.Equal(OrderStatus.Completed, (await db.Orders.SingleAsync(order => order.Id == created.Id)).Status);
    }

    [Fact]
    public async Task StripeWebhook_AlreadyCompletedOrder_ReturnsOkWithoutChangingCartAgain()
    {
        var (client, anonymous, created, gameId) = await CreatePendingOrderAsync();
        var sessionId = SessionIdFrom(created);

        Assert.Equal(HttpStatusCode.OK, (await StripeWebhookTestHelper.PostCheckoutSessionCompletedAsync(
            anonymous,
            created.Id,
            sessionId,
            GameStoreApiFactory.StripeWebhookSecret)).StatusCode);

        await client.PostAsJsonAsync("/api/cart/items", new { gameId });

        var response = await StripeWebhookTestHelper.PostCheckoutSessionCompletedAsync(
            anonymous,
            created.Id,
            sessionId,
            GameStoreApiFactory.StripeWebhookSecret);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cart = await client.GetFromJsonAsync<GetCartResponse>("/api/cart", JsonOptions);
        Assert.NotNull(cart);
        Assert.Equal(gameId, Assert.Single(cart.Items).GameId);
    }

    [Fact]
    public async Task StripeWebhook_UnknownOrder_ReturnsNotFound()
    {
        var anonymous = _factory.CreateClient();
        var unknownOrderId = Guid.NewGuid();
        var sessionId = $"cs_test_{unknownOrderId:N}";

        var response = await StripeWebhookTestHelper.PostCheckoutSessionCompletedAsync(
            anonymous,
            unknownOrderId,
            sessionId,
            GameStoreApiFactory.StripeWebhookSecret);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StripeWebhook_MissingOrderCorrelation_ReturnsBadRequest()
    {
        var (_, anonymous, created, _) = await CreatePendingOrderAsync();
        var payload = StripeWebhookTestHelper.BuildCheckoutSessionCompletedPayload(
            created.Id,
            SessionIdFrom(created),
            includeOrderCorrelation: false);

        var response = await StripeWebhookTestHelper.PostRawAsync(
            anonymous,
            payload,
            StripeWebhookTestHelper.CreateSignatureHeader(payload, GameStoreApiFactory.StripeWebhookSecret));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertOrderStillPendingAsync(created.Id);
    }

    private async Task<(HttpClient Client, HttpClient Anonymous, CreateOrderResponse Created, Guid GameId)>
        CreatePendingOrderAsync()
    {
        var genreId = await _factory.SeedGenreAsync();
        var game = await _factory.SeedGameAsync(genreId, name: $"Webhook {Guid.NewGuid()}", price: 25m);
        var client = _factory.CreateUserClient($"webhook-{Guid.NewGuid()}");
        await client.PostAsJsonAsync("/api/cart/items", new { gameId = game.Id });

        var createResponse = await client.PostAsync("/api/orders", null);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(OrderStatus.Pending, created.Status);

        return (client, _factory.CreateClient(), created, game.Id);
    }

    private async Task AssertOrderStillPendingAsync(Guid orderId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        var order = await db.Orders.SingleAsync(existing => existing.Id == orderId);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    private static string SessionIdFrom(CreateOrderResponse created) =>
        created.CheckoutUrl.Split('/').Last();
}
