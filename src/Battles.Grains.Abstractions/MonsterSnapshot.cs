namespace Battles.Grains.Abstractions;

/// <summary>
/// Point-in-time view of a monster instance.
/// </summary>
/// <param name="SpeciesId">The Catalog species.</param>
/// <param name="HitPoints">Remaining hit points.</param>
/// <param name="MaxHitPoints">Hit points at full health.</param>
[GenerateSerializer]
public sealed record MonsterSnapshot(
    [property: Id(0)] string SpeciesId,
    [property: Id(1)] int HitPoints,
    [property: Id(2)] int MaxHitPoints)
{
    /// <summary>
    /// Whether the monster is out of the fight.
    /// </summary>
    public bool IsDefeated => HitPoints <= 0;
}
