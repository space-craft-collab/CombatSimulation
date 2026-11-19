namespace Battles.Grains.Abstractions;

/// <summary>
/// Lifecycle of a live battle (ADR-0007). Stored as its number in
/// grain state, so the values are frozen: never renumber or reuse one,
/// add new members at the end with the next number.
/// </summary>
public enum BattleStatus
{
    /// <summary>The battle exists but has not started yet.</summary>
    Created = 0,

    /// <summary>
    /// One human opened the battle and waits for an opponent, human
    /// or bot, to join.
    /// </summary>
    WaitingForPlayers = 1,

    /// <summary>Rounds are being played.</summary>
    InProgress = 2,

    /// <summary>The battle has a result and accepts no more actions.</summary>
    Completed = 3,
}
