using System.Collections.Immutable;

namespace Battles.Grains.Abstractions;

/// <summary>
/// One entry in an arena's battle list.
/// </summary>
/// <param name="BattleId">The battle's id.</param>
/// <param name="Status">The battle's lifecycle state.</param>
/// <param name="ParticipantIds">The sides that have joined so far, in join order.</param>
[GenerateSerializer]
public sealed record BattleSummary(
    [property: Id(0)] Guid BattleId,
    [property: Id(1)] BattleStatus Status,
    [property: Id(2)] ImmutableArray<string> ParticipantIds);
