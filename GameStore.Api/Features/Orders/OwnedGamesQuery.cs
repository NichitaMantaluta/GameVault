using GameStore.Api.Domain.Orders;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Orders;

public static class OwnedGamesQuery
{
    public static Task<HashSet<Guid>> GetOwnedGameIdsAsync(
        GameStoreDbContext db,
        string userId,
        CancellationToken cancellationToken)
    {
        return db.Orders
            .AsNoTracking()
            .Where(order => order.UserId == userId && order.Status == OrderStatus.Completed)
            .SelectMany(order => order.Items)
            .Select(item => item.GameId)
            .ToHashSetAsync(cancellationToken);
    }
}
