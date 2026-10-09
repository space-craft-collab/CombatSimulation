using System.Collections.Immutable;

namespace Battles.Grains.Abstractions;

/// <summary>
/// Everything a live battle needs to start.
/// </summary>
/// <param name="ArenaId">The Catalog arena the battle runs on.</param>
/// <param name="Participants">The sides taking part; the Phase 2 demo uses two.</param>
[GenerateSerializer]
public sealed record BattleSetup(
    [property: Id(0)] string ArenaId,
    [property: Id(1)] ImmutableArray<ParticipantSetup> Participants);
