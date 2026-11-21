namespace Catalog.Features.ListArenas;

/// <summary>
/// One arena in <c>GET /arenas</c>, as the HTTP API shows it.
/// </summary>
/// <param name="Id">The arena id, used in <c>/arenas/{arenaId}/battles</c>.</param>
/// <param name="Name">The display name.</param>
internal sealed record ArenaResponse(string Id, string Name);
