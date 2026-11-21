using System.Net;
using System.Net.Http.Json;
using Catalog.Features.ListArenas;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Plays the Catalog arenas over HTTP: list them, and get a 404 for
/// battles on an arena the Catalog does not know.
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class ArenaEndpointTests(AppHostFixture host)
{
    private static readonly Uri UnknownArenaBattles = new("/arenas/no-such-arena/battles", UriKind.Relative);

    private static readonly string[] Team = ["lava-crab"];

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetArenas_SeededCatalog_ListsArenasByName()
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        var arenas = await client.GetFromJsonAsync<List<ArenaResponse>>(
            new Uri("/arenas", UriKind.Relative), Cancellation);

        // Assert
        Assert.Equal(
            [
                new ArenaResponse("glacier", "Glacier"),
                new ArenaResponse("moss-grove", "Moss Grove"),
                new ArenaResponse("volcano", "Volcano"),
            ],
            arenas);
    }

    [Theory]
    [InlineData("Bot")]
    [InlineData("Human")]
    public async Task PostBattle_UnknownArena_Returns404(string opponent)
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            UnknownArenaBattles,
            new { playerId = "player-1", monsterSpeciesIds = Team, opponent },
            Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBattles_UnknownArena_Returns404()
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        using var response = await client.GetAsync(UnknownArenaBattles, Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
