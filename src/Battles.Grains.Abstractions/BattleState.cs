using System.Collections.Immutable;

namespace Battles.Grains.Abstractions;

/// <summary>
/// Point-in-time view of a live battle.
/// </summary>
/// <param name="BattleId">The battle's id, which is also its grain key.</param>
/// <param name="ArenaId">The Catalog arena the battle runs on.</param>
/// <param name="Status">The current lifecycle state.</param>
/// <param name="Round">The current round, starting at 1 once in progress.</param>
/// <param name="WinnerId">The winning participant once completed; otherwise <see langword="null"/>.
/// A completed battle without a winner was abandoned: no human acted before the turn deadline.</param>
/// <param name="TurnDeadline">When the current round resolves without the humans that have not acted;
/// <see langword="null"/> when no human is due to act.</param>
/// <param name="Combatants">Every monster slot in slot order, defeated ones included.</param>
[GenerateSerializer]
public sealed record BattleState(
    [property: Id(0)] Guid BattleId,
    [property: Id(1)] string ArenaId,
    [property: Id(2)] BattleStatus Status,
    [property: Id(3)] int Round,
    [property: Id(4)] string? WinnerId,
    [property: Id(5)] DateTimeOffset? TurnDeadline,
    [property: Id(6)] ImmutableArray<CombatantSnapshot> Combatants);
