namespace Battles.Grains;

/// <summary>
/// One monster slot as the battle sees it: owner, combat stats and
/// whether it is still standing. Hit points live in the monster grain.
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
    public bool IsDefeated { get; set; }
}
