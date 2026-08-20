using GameStore.Api.Domain.Orders;

namespace GameStore.Api.Features.Orders.GetOrders;

public record GetOrdersItem(
    Guid Id,
    OrderStatus Status,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset CreatedAt);

public record GetOrdersResponse(IReadOnlyList<GetOrdersItem> Items);
