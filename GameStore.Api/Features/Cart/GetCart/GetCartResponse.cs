namespace GameStore.Api.Features.Cart.GetCart;

public record GetCartItemResponse(
    Guid GameId,
    string Name,
    decimal Price,
    string? ImageUrl,
    int Quantity,
    decimal LineTotal);

public record GetCartResponse(
    IReadOnlyList<GetCartItemResponse> Items,
    decimal Subtotal);
