namespace Players.Features.GetPlayerStats;

/// <summary>
/// Body of <c>GET /players/{playerId}</c>: the player's record of
/// decided battles.
/// </summary>
/// <param name="PlayerId">The player id.</param>
/// <param name="Wins">Battles won.</param>
/// <param name="Losses">Battles lost.</param>
internal sealed record PlayerStatsResponse(string PlayerId, int Wins, int Losses);
