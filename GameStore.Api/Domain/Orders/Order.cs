namespace GameStore.Api.Domain.Orders;

public class Order
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public required string Currency { get; set; }
    public string? StripeCheckoutSessionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
