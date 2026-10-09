using System.Collections.Immutable;

namespace Battles.Grains.Abstractions;

/// <summary>
/// One side of a battle: a player or a bot and the monsters it fields.
/// </summary>
/// <param name="ParticipantId">The player id, or a bot identifier.</param>
/// <param name="IsBot">Whether the participant is driven by a bot.</param>
/// <param name="MonsterSpeciesIds">The Catalog species of each fielded monster, in slot order.</param>
[GenerateSerializer]
public sealed record ParticipantSetup(
    [property: Id(0)] string ParticipantId,
    [property: Id(1)] bool IsBot,
    [property: Id(2)] ImmutableArray<string> MonsterSpeciesIds);
