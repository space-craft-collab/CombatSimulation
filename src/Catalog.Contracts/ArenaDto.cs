namespace Catalog.Contracts;

/// <summary>
/// An arena: the template battles run on (ADR-0007).
/// </summary>
/// <param name="Id">The arena id, e.g. <c>"volcano"</c>.</param>
/// <param name="Name">The display name.</param>
public sealed record ArenaDto(string Id, string Name);
