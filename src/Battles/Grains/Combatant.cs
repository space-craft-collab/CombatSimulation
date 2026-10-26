namespace Battles.Grains;

/// <summary>
/// One monster slot as the battle sees it: owner, combat stats and a
/// mirror of its hit points. The monster grain stays the authority on
/// hit points; the battle copies them from each damage snapshot so
/// the bot can decide without extra grain calls.
/// </summary>
[GenerateSerializer]
internal sealed class Combatant
{
    [Id(0)]
    public required int Slot { get; init; }

    [Id(1)]
    public required string ParticipantId { get; init; }

    [Id(2)]
    public required int Attack { get; init; }

    [Id(3)]
    public required int Defense { get; init; }

    [Id(4)]
    public required int Speed { get; init; }

    [Id(5)]
    public required int HitPoints { get; set; }

    [Id(6)]
    public required bool IsBot { get; init; }

    [Id(7)]
    public required string SpeciesId { get; init; }

    [Id(8)]
    public required int MaxHitPoints { get; init; }

    public bool IsDefeated => HitPoints <= 0;
}
