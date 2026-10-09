namespace Catalog.Contracts;

/// <summary>
/// A monster species with its base stats.
/// </summary>
/// <param name="Id">The species id, e.g. <c>"ember-fox"</c>.</param>
/// <param name="Name">The display name.</param>
/// <param name="MaxHitPoints">Hit points at full health.</param>
/// <param name="Attack">Offensive strength.</param>
/// <param name="Defense">Damage reduction.</param>
/// <param name="Speed">Decides turn order within a round.</param>
public sealed record MonsterSpeciesDto(
    string Id,
    string Name,
    int MaxHitPoints,
    int Attack,
    int Defense,
    int Speed);
