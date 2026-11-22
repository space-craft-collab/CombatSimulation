namespace Battles.Domain;

/// <summary>
/// The difficulty a completed battle was played on (ADR-0013), as
/// the cold store records it. It mirrors the grain contracts'
/// <c>Difficulty</c>, which the Domain may not depend on
/// (ADR-0009). Stored by name in SQL, so a name never changes.
/// </summary>
internal enum BattleDifficulty
{
    /// <summary>The bot's monsters fought with their species' stats, or no bot took part.</summary>
    Normal = 0,

    /// <summary>The bot's monsters entered the battle weakened.</summary>
    Easy = 1,
}
