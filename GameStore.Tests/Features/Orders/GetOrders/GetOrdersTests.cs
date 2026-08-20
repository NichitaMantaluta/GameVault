using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameStore.Api.Domain.Orders;
using GameStore.Api.Features.Orders.CreateOrder;
using GameStore.Api.Features.Orders.GetOrders;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Tests.Features.Orders.GetOrders;

public class GetOrdersTests : IClassFixture<GameStoreApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly GameStoreApiFactory _factory;

    public GetOrdersTests(GameStoreApiFactory factory)
    {
        _factory = factory;
        _factory.PaymentService.ShouldFail = false;
    }

    [Fact]
    public async Task GetOrders_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_WhenUserHasNoOrders_ReturnsEmptyList()
    {
        var client = _factory.CreateUserClient($"none-{Guid.NewGuid()}");

        var response = await client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetOrdersResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Empty(body.Items);
    }

    [Fact]
    public async Task GetOrders_ReturnsOnlyCurrentUsersOrdersNewestFirst()
    {
        var genreId = await _factory.SeedGenreAsync();
        var ownerGame1 = await _factory.SeedGameAsync(genreId, name: $"Owner 1 {Guid.NewGuid()}", price: 10m);
        var ownerGame2 = await _factory.SeedGameAsync(genreId, name: $"Owner 2 {Guid.NewGuid()}", price: 20m);
        var otherGame = await _factory.SeedGameAsync(genreId, name: $"Other {Guid.NewGuid()}", price: 30m);

        var owner = _factory.CreateUserClient($"list-owner-{Guid.NewGuid()}");
        var other = _factory.CreateUserClient($"list-other-{Guid.NewGuid()}");
        var anonymous = _factory.CreateClient();

        await owner.PostAsJsonAsync("/api/cart/items", new { gameId = ownerGame1.Id });
        var firstResponse = await owner.PostAsync("/api/orders", null);
        var first = await firstResponse.Content.ReadFromJsonAsync<CreateOrderResponse>(JsonOptions);
        Assert.NotNull(first);
        Assert.Equal(HttpStatusCode.OK, (await StripeWebhookTestHelper.PostCheckoutSessionCompletedAsync(
            anonymous,
            first.Id,
            first.CheckoutUrl.Split('/').Last(),
            GameStoreApiFactory.StripeWebhookSecret)).StatusCode);

        await owner.PostAsJsonAsync("/api/cart/items", new { gameId = ownerGame2.Id });
        var secondResponse = await owner.PostAsync("/api/orders", null);
        var second = await secondResponse.Content.ReadFromJsonAsync<CreateOrderResponse>(JsonOptions);
        Assert.NotNull(second);
        Assert.Equal(HttpStatusCode.OK, (await StripeWebhookTestHelper.PostCheckoutSessionCompletedAsync(
            anonymous,
            second.Id,
            second.CheckoutUrl.Split('/').Last(),
            GameStoreApiFactory.StripeWebhookSecret)).StatusCode);

        await other.PostAsJsonAsync("/api/cart/items", new { gameId = otherGame.Id });
        var otherResponse = await other.PostAsync("/api/orders", null);
        Assert.Equal(HttpStatusCode.Created, otherResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
            var older = await db.Orders.SingleAsync(order => order.Id == first.Id);
            older.CreatedAt = second.CreatedAt.AddMinutes(-5);
            await db.SaveChangesAsync();
        }

        var response = await owner.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetOrdersResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(2, body.Items.Count);
        Assert.Equal(second.Id, body.Items[0].Id);
        Assert.Equal(first.Id, body.Items[1].Id);
        Assert.All(body.Items, item =>
        {
            Assert.Equal(OrderStatus.Completed, item.Status);
            Assert.Equal("USD", item.Currency);
            Assert.NotEqual(Guid.Empty, item.Id);
        });
        Assert.Equal(20m, body.Items[0].TotalAmount);
        Assert.Equal(10m, body.Items[1].TotalAmount);

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("gameName", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gameId", json, StringComparison.OrdinalIgnoreCase);

        var otherList = await other.GetFromJsonAsync<GetOrdersResponse>("/api/orders", JsonOptions);
        Assert.NotNull(otherList);
        var otherOrder = Assert.Single(otherList.Items);
        Assert.DoesNotContain(body.Items, item => item.Id == otherOrder.Id);
    }
}
