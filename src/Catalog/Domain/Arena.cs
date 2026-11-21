namespace Catalog.Domain;

/// <summary>
/// An arena: the template battles run on (ADR-0007), as stored in
/// the Catalog.
/// </summary>
/// <param name="Id">The arena id, e.g. <c>"volcano"</c>.</param>
/// <param name="Name">The display name.</param>
internal sealed record Arena(string Id, string Name);
