namespace OrleansMonsterArena.Tests;

/// <summary>
/// Values of the <c>Category</c> trait that CI filters on.
/// </summary>
internal static class TestCategories
{
    /// <summary>
    /// Slow end-to-end scenarios (host restarts, the hot → cold
    /// bridge) that start containers of their own on top of the
    /// shared SQL Server. The required <c>build-test</c> job skips
    /// them; the <c>integration</c> job runs only them.
    /// </summary>
    public const string Integration = "Integration";
}
