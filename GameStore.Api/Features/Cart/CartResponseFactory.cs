using GameStore.Api.Features.Cart.GetCart;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Cart;

internal static class CartResponseFactory
{
    public static GetCartResponse Empty { get; } = new([], 0m);

    public static async Task<GetCartResponse> CreateAsync(
        GameStoreDbContext db,
        Guid cartId,
        CancellationToken cancellationToken)
    {
        var items = await db.CartItems
            .AsNoTracking()
            .Where(item => item.CartId == cartId)
            .Join(
                db.Games.AsNoTracking(),
                item => item.GameId,
                game => game.Id,
                (item, game) => new GetCartItemResponse(
                    game.Id,
                    game.Name,
                    game.Price,
                    game.ImageUrl,
                    item.Quantity,
                    game.Price * item.Quantity))
            .ToListAsync(cancellationToken);

        return new GetCartResponse(items, items.Sum(item => item.LineTotal));
    }
}
