namespace OrleansMonsterArena.Tests;

/// <summary>
/// Values of the <c>Category</c> trait that CI filters on.
/// </summary>
internal static class TestCategories
{
    /// <summary>
    /// Tests that start Docker containers through Testcontainers.
    /// The required <c>build-test</c> job skips them; the
    /// <c>integration</c> job runs only them.
    /// </summary>
    public const string Containers = "Containers";
}
