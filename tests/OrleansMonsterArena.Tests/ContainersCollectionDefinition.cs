using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Collection for tests that boot their own hosts against
/// containers. The co-hosted silo binds fixed localhost ports, so
/// these tests run alone, after every parallel collection, the
/// shared <see cref="AppHostFixture"/> included, has finished.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ContainersCollectionDefinition
{
    /// <summary>
    /// The collection name referenced by <c>[Collection]</c>.
    /// </summary>
    public const string Name = "Containers";
}
