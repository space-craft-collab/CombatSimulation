namespace Players.Contracts;

/// <summary>
/// The sides of a decided battle, as the player stats count them.
/// Bots are left out by the caller; only player ids appear here.
/// </summary>
/// <param name="WinnerId">The winning player's id.</param>
/// <param name="LoserIds">The losing players' ids; one in a 1v1.</param>
public sealed record BattleResultDto(string WinnerId, IReadOnlyList<string> LoserIds);
