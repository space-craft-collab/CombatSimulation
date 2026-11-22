using Catalog.Contracts;

namespace Catalog.Domain;

/// <summary>
/// A monster species and its base stats, as stored in the Catalog.
/// </summary>
/// <param name="Id">The species id, e.g. <c>"ember-fox"</c>.</param>
/// <param name="Name">The display name.</param>
/// <param name="Element">The species' element.</param>
/// <param name="MaxHitPoints">Hit points at full health.</param>
/// <param name="Attack">Offensive strength.</param>
/// <param name="Defense">Damage reduction.</param>
/// <param name="Speed">Decides turn order within a round.</param>
internal sealed record MonsterSpecies(
    string Id,
    string Name,
    Element Element,
    int MaxHitPoints,
    int Attack,
    int Defense,
    int Speed);
