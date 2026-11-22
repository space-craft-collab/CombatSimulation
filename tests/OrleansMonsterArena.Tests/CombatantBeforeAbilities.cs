namespace OrleansMonsterArena.Tests;

/// <summary>
/// The shape a battle's monster slot was stored in before Phase 4.5:
/// the members with the <c>[Id]</c> numbers 0 to 8 and nothing else.
/// <see cref="GrainStateSerializationTests"/> writes it and reads it
/// back as today's <c>Combatant</c>, the way a battle stored then is
/// read now.
/// </summary>
[GenerateSerializer]
internal sealed class CombatantBeforeAbilities
{
    [Id(0)]
    public int Slot { get; init; }

    [Id(1)]
    public string ParticipantId { get; init; } = "";

    [Id(2)]
    public int Attack { get; init; }

    [Id(3)]
    public int Defense { get; init; }

    [Id(4)]
    public int Speed { get; init; }

    [Id(5)]
    public int HitPoints { get; init; }

    [Id(6)]
    public bool IsBot { get; init; }

    [Id(7)]
    public string SpeciesId { get; init; } = "";

    [Id(8)]
    public int MaxHitPoints { get; init; }
}
