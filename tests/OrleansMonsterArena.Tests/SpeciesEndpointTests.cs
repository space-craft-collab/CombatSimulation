using System.Net.Http.Json;
using Catalog.Contracts;
using Catalog.Features.ListSpecies;
using Catalog.Infrastructure;
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

        // Assert: every seeded species with the element and stats it
        // was seeded with, ordered by name.
        Assert.NotNull(species);
        Assert.Equal(
            SpeciesSeed.All
                .OrderBy(seeded => seeded.Name, StringComparer.Ordinal)
                .Select(seeded => (seeded.Id, seeded.Name, seeded.Element, seeded.MaxHitPoints, seeded.Attack, seeded.Defense, seeded.Speed)),
            species.Select(listed => (listed.Id, listed.Name, listed.Element, listed.MaxHitPoints, listed.Attack, listed.Defense, listed.Speed)));
    }

    [Fact]
    public async Task GetSpecies_SeededCatalog_ListsEachSpeciesAbilitiesInOrder()
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        var species = await client.GetFromJsonAsync<List<SpeciesResponse>>(
            new Uri("/species", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert: the ability ids the seed links to each species, and
        // a guard or a heal marked as used on the monster itself.
        Assert.NotNull(species);
        Assert.All(species, listed =>
        {
            Assert.Equal(
                AbilitySeed.BySpecies
                    .Where(link => link.SpeciesId == listed.Id)
                    .OrderBy(link => link.Position)
                    .Select(link => link.AbilityId),
                listed.Abilities.Select(ability => ability.Id));
            Assert.All(listed.Abilities, ability =>
            {
                Assert.False(string.IsNullOrWhiteSpace(ability.Name));
                Assert.Equal(
                    ability.Effect is AbilityEffect.Guard or AbilityEffect.Heal,
                    ability.TargetsSelf);
            });
        });
    }
}
