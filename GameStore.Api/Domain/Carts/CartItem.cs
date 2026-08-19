namespace GameStore.Api.Domain.Carts;

public class CartItem
{
    public Guid CartId { get; set; }
    public Guid GameId { get; set; }
    public int Quantity { get; set; }
}
