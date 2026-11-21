using System.Net.Http.Json;
using Battles.Features.GetBattle;
using Battles.Features.GetBattleHistory;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// <c>GET /players/{playerId}/battles</c>: a player's completed
/// battles from the cold store. Every test uses a fresh player id,
/// since the host's database is shared.
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class BattleHistoryEndpointTests(AppHostFixture host)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetHistory_TwoCompletedAndOneLive_ListsCompletedNewestFirst()
    {
        // Arrange
        var player = NewPlayerId();
        using var client = host.Factory.CreateClient();
        var first = await PlayWonBattleAsync(client, player);
        var second = await PlayWonBattleAsync(client, player);
        await StartAsync(client, player);

        // Act
        var history = await GetHistoryAsync(client, player);

        // Assert
        Assert.Equal([second, first], history.Select(entry => entry.BattleId));
        var latest = history[0];
        Assert.Equal(TestArenas.Volcano, latest.ArenaId);
        Assert.Equal(player, latest.WinnerId);
        Assert.Equal(4, latest.Round);
        Assert.Equal([player, "bot"], latest.ParticipantIds);
    }

    [Fact]
    public async Task GetHistory_NoCompletedBattle_ReturnsEmptyList()
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        var history = await GetHistoryAsync(client, NewPlayerId());

        // Assert
        Assert.Empty(history);
    }

    private static string NewPlayerId() => $"player-{Guid.NewGuid():N}";

    private static async Task<BattleResponse> StartAsync(HttpClient client, string player)
    {
        using var response = await client.PostAsJsonAsync(
            TestArenas.VolcanoBattles,
            new { playerId = player, monsterSpeciesIds = new[] { "ember-fox" } },
            Cancellation);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation)
            ?? throw new InvalidOperationException("The start endpoint returned no battle.");
    }

    // Mirrored foxes deal 10 per hit, 40 HP each; the player wins the
    // speed tie on the lower slot, in round 4.
    private static async Task<Guid> PlayWonBattleAsync(HttpClient client, string player)
    {
        var battle = await StartAsync(client, player);
        for (var round = 0; round < 4; round++)
        {
            using var played = await client.PostAsJsonAsync(
                new Uri($"/battles/{battle.BattleId}/actions", UriKind.Relative),
                new { participantId = player, actorSlot = 0, targetSlot = 1 },
                Cancellation);
            played.EnsureSuccessStatusCode();
        }

        return battle.BattleId;
    }

    private static async Task<IReadOnlyList<BattleHistoryEntryResponse>> GetHistoryAsync(
        HttpClient client,
        string player) =>
        await client.GetFromJsonAsync<IReadOnlyList<BattleHistoryEntryResponse>>(
            new Uri($"/players/{player}/battles", UriKind.Relative), Cancellation)
        ?? throw new InvalidOperationException("The history endpoint returned no list.");
}
