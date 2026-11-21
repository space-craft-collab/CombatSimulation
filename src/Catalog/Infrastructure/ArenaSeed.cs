using Catalog.Domain;

namespace Catalog.Infrastructure;

/// <summary>
/// The arenas the Catalog ships with. They reach SQL through the
/// migrations (<c>HasData</c>), so a change here needs a new
/// migration to take effect.
/// </summary>
internal static class ArenaSeed
{
    public static IReadOnlyList<Arena> All { get; } =
    [
        new("volcano", "Volcano"),
        new("glacier", "Glacier"),
        new("moss-grove", "Moss Grove"),
    ];
}
