namespace OrleansMonsterArena.Tests;

/// <summary>
/// The Catalog arenas the HTTP tests play on. Battles must run on an
/// arena the Catalog seeds, so the tests share one and tell their
/// battles apart by id.
/// </summary>
internal static class TestArenas
{
    /// <summary>
    /// A seeded arena id.
    /// </summary>
    public const string Volcano = "volcano";

    /// <summary>
    /// <c>/arenas/volcano/battles</c>: starts, opens and lists battles
    /// on <see cref="Volcano"/>.
    /// </summary>
    public static Uri VolcanoBattles { get; } = new($"/arenas/{Volcano}/battles", UriKind.Relative);
}
