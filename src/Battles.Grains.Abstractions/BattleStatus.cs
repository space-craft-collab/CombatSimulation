namespace Battles.Grains.Abstractions;

/// <summary>
/// Lifecycle of a live battle (ADR-0007).
/// </summary>
public enum BattleStatus
{
    /// <summary>The battle exists but has not started yet.</summary>
    Created,

    /// <summary>
    /// One human opened the battle and waits for an opponent, human
    /// or bot, to join.
    /// </summary>
    WaitingForPlayers,

    /// <summary>Rounds are being played.</summary>
    InProgress,

    /// <summary>The battle has a result and accepts no more actions.</summary>
    Completed,
}
