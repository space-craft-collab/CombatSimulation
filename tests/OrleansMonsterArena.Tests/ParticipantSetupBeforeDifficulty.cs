using System.Collections.Immutable;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// The shape a side of a battle was sent in before difficulties
/// existed: the members with the <c>[Id]</c> numbers 0 to 2 and
/// nothing else. <see cref="GrainStateSerializationTests"/> writes it
/// and reads it back as today's <c>ParticipantSetup</c>, the way an
/// older silo's message is read now.
/// </summary>
/// <param name="ParticipantId">The player id, or a bot identifier.</param>
/// <param name="IsBot">Whether the participant is driven by a bot.</param>
/// <param name="MonsterSpeciesIds">The fielded species, in slot order.</param>
[GenerateSerializer]
internal sealed record ParticipantSetupBeforeDifficulty(
    [property: Id(0)] string ParticipantId,
    [property: Id(1)] bool IsBot,
    [property: Id(2)] ImmutableArray<string> MonsterSpeciesIds);
