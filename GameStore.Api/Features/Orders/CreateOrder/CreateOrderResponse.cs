using GameStore.Api.Domain.Orders;

namespace GameStore.Api.Features.Orders.CreateOrder;

public record CreateOrderItemResponse(
    Guid GameId,
    string GameName,
    decimal Price);

public record CreateOrderResponse(
    Guid Id,
    OrderStatus Status,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset CreatedAt,
    string CheckoutUrl,
    IReadOnlyList<CreateOrderItemResponse> Items);
