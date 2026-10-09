namespace Battles.Grains.Abstractions;

/// <summary>
/// The stats a monster instance enters a battle with.
/// </summary>
/// <param name="SpeciesId">The Catalog species.</param>
/// <param name="MaxHitPoints">Hit points at full health.</param>
/// <param name="Attack">Offensive strength.</param>
/// <param name="Defense">Damage reduction.</param>
/// <param name="Speed">Decides turn order within a round.</param>
[GenerateSerializer]
public sealed record MonsterStats(
    [property: Id(0)] string SpeciesId,
    [property: Id(1)] int MaxHitPoints,
    [property: Id(2)] int Attack,
    [property: Id(3)] int Defense,
    [property: Id(4)] int Speed);
