namespace GameStore.Api.Features.Orders.GetOwnedGames;

public record GetOwnedGamesResponse(IReadOnlyList<Guid> GameIds);
