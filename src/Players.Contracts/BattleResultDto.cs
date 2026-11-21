namespace Players.Contracts;

/// <summary>
/// The human sides of a decided battle, as the player stats count
/// them. Bots are left out by the caller, so a battle against the
/// bot has either no winner (the bot won) or no loser (the bot
/// lost); at least one player is always present.
/// </summary>
/// <param name="WinnerId">The winning player's id, or
/// <see langword="null"/> when a bot won.</param>
/// <param name="LoserIds">The losing players' ids; one in a 1v1
/// between humans, none when the player beat a bot.</param>
public sealed record BattleResultDto(string? WinnerId, IReadOnlyList<string> LoserIds);
