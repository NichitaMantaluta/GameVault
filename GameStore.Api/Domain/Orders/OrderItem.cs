namespace GameStore.Api.Domain.Orders;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public required string UserId { get; set; }
    public Guid GameId { get; set; }
    public required string GameName { get; set; }
    public decimal Price { get; set; }
}
