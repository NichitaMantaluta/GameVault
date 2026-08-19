namespace GameStore.Api.Domain.Carts;

public class Cart
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public List<CartItem> Items { get; set; } = [];
}
