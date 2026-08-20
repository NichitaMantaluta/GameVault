using GameStore.Api.Domain.Orders;

namespace GameStore.Api.Features.Orders.GetOrder;

public record GetOrderItemResponse(
    Guid GameId,
    string GameName,
    decimal Price);

public record GetOrderResponse(
    Guid Id,
    OrderStatus Status,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset CreatedAt,
    IReadOnlyList<GetOrderItemResponse> Items);
