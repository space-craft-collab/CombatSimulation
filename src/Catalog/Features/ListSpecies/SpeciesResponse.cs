namespace Catalog.Features.ListSpecies;

/// <summary>
/// One species in <c>GET /species</c>, as the HTTP API shows it.
/// </summary>
/// <param name="Id">The species id, as a team names it when a battle starts.</param>
/// <param name="Name">The display name.</param>
/// <param name="MaxHitPoints">Hit points at full health.</param>
/// <param name="Attack">Offensive strength.</param>
/// <param name="Defense">Damage reduction.</param>
/// <param name="Speed">Decides turn order within a round.</param>
internal sealed record SpeciesResponse(
    string Id,
    string Name,
    int MaxHitPoints,
    int Attack,
    int Defense,
    int Speed);
