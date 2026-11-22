using Catalog.Contracts;

namespace Battles.Domain;

/// <summary>
/// One monster in a battle as the combat rules see it: owner, combat
/// stats and current hit points. Zero hit points means defeated.
/// </summary>
internal sealed record Fighter
{
    /// <summary>
    /// The slot, numbered across the whole battle.
    /// </summary>
    public required int Slot { get; init; }

    /// <summary>
    /// The side fielding the monster.
    /// </summary>
    public required string ParticipantId { get; init; }

    /// <summary>
    /// The monster's element.
    /// </summary>
    public Element Element { get; init; }

    /// <summary>
    /// Offensive strength.
    /// </summary>
    public required int Attack { get; init; }

    /// <summary>
    /// Damage reduction.
    /// </summary>
    public required int Defense { get; init; }

    /// <summary>
    /// Decides turn order within a round.
    /// </summary>
    public required int Speed { get; init; }

    /// <summary>
    /// Remaining hit points.
    /// </summary>
    public required int HitPoints { get; init; }

    /// <summary>
    /// Whether the monster is out of the fight.
    /// </summary>
    public bool IsDefeated => HitPoints <= 0;
}
