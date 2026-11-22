namespace Catalog.Contracts;

/// <summary>
/// A monster species with its base stats.
/// </summary>
/// <param name="Id">The species id, e.g. <c>"ember-fox"</c>.</param>
/// <param name="Name">The display name.</param>
/// <param name="Element">The species' element.</param>
/// <param name="MaxHitPoints">Hit points at full health.</param>
/// <param name="Attack">Offensive strength.</param>
/// <param name="Defense">Damage reduction.</param>
/// <param name="Speed">Decides turn order within a round.</param>
/// <param name="Abilities">The abilities the species knows, the basic attack first.</param>
public sealed record MonsterSpeciesDto(
    string Id,
    string Name,
    Element Element,
    int MaxHitPoints,
    int Attack,
    int Defense,
    int Speed,
    IReadOnlyList<AbilityDto> Abilities);
