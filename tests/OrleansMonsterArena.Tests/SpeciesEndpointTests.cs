using System.Net.Http.Json;
using Catalog.Features.ListSpecies;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Reads the Catalog species over HTTP, as the team picker does.
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class SpeciesEndpointTests(AppHostFixture host)
{
    [Fact]
    public async Task GetSpecies_SeededCatalog_ListsSpeciesByNameWithStats()
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        var species = await client.GetFromJsonAsync<List<SpeciesResponse>>(
            new Uri("/species", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            [
                new SpeciesResponse("ember-fox", "Ember Fox", 40, 14, 4, 12),
                new SpeciesResponse("frost-owl", "Frost Owl", 35, 12, 5, 14),
                new SpeciesResponse("lava-crab", "Lava Crab", 60, 10, 8, 5),
                new SpeciesResponse("moss-golem", "Moss Golem", 80, 8, 10, 3),
            ],
            species);
    }
}
