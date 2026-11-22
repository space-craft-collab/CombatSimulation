using System.Collections.Immutable;

namespace Battles.Grains.Abstractions;

/// <summary>
/// One side of a battle: a player or a bot and the monsters it fields.
/// </summary>
/// <param name="ParticipantId">The player id, or a bot identifier.</param>
/// <param name="IsBot">Whether the participant is driven by a bot.</param>
/// <param name="MonsterSpeciesIds">The Catalog species of each fielded monster, in slot order.</param>
/// <param name="Difficulty">How hard the bot is (ADR-0013): on <see cref="Difficulty.Easy"/> its monsters
/// enter the battle weakened. Ignored for a human side. A message without it reads as
/// <see cref="Difficulty.Normal"/>.</param>
[GenerateSerializer]
[Alias("battles.ParticipantSetup")]
public sealed record ParticipantSetup(
    [property: Id(0)] string ParticipantId,
    [property: Id(1)] bool IsBot,
    [property: Id(2)] ImmutableArray<string> MonsterSpeciesIds,
    [property: Id(3)] Difficulty Difficulty = Difficulty.Normal);
