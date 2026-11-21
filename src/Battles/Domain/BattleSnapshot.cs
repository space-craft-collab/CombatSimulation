namespace Battles.Domain;

/// <summary>
/// The permanent record of a completed battle in the cold store
/// (ADR-0003), written once by the battle grain when the battle
/// ends and never changed afterwards.
/// </summary>
internal sealed class BattleSnapshot
{
    /// <summary>
    /// The battle's id, the same as its grain key.
    /// </summary>
    public required Guid BattleId { get; init; }

    /// <summary>
    /// The Catalog arena the battle ran on.
    /// </summary>
    public required string ArenaId { get; init; }

    /// <summary>
    /// The round the battle ended in; 0 for an open battle nobody
    /// joined.
    /// </summary>
    public required int Round { get; init; }

    /// <summary>
    /// The winning side, bot or human; <see langword="null"/> when
    /// the battle was abandoned.
    /// </summary>
    public required string? WinnerId { get; init; }

    /// <summary>
    /// When the battle ended.
    /// </summary>
    public required DateTimeOffset CompletedAt { get; init; }

    /// <summary>
    /// Every monster slot with its final hit points, in slot order.
    /// </summary>
    public required List<SnapshotCombatant> Combatants { get; init; }
}
