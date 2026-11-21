namespace Players.Domain;

/// <summary>
/// One player's record of decided battles, as stored in the Players
/// module.
/// </summary>
/// <param name="PlayerId">The client-chosen player id.</param>
/// <param name="Wins">Battles won.</param>
/// <param name="Losses">Battles lost.</param>
internal sealed record PlayerStats(string PlayerId, int Wins, int Losses);
