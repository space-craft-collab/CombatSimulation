using System.Net.Http.Json;
using Azure.Data.Tables;
using Battles.Features.GetBattle;
using Battles.Features.GetBattleHistory;
using Battles.Grains.Abstractions;
using Players.Features.GetPlayerStats;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// The hot→cold bridge on the real stores (ADR-0003): a finished
/// battle lands in SQL Server and in the player's stats, its rows
/// are gone from Table Storage, and <c>GET /battles/{id}</c> still
/// returns it.
/// </summary>
/// <param name="azurite">The Table Storage the hot state lives in.</param>
/// <param name="sql">The SQL Server the cold store lives in.</param>
[Collection(IntegrationCollectionDefinition.Name)]
[Trait("Category", TestCategories.Integration)]
public sealed class HotColdBridgeTests(AzuriteFixture azurite, SqlServerFixture sql) : IClassFixture<AzuriteFixture>
{
    // Orleans' default table for Azure Table grain storage.
    private const string GrainStateTable = "OrleansGrainState";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CompleteBattle_OnTableStorage_MovesBattleToColdStoreAndDeletesHotRows()
    {
        // Arrange: mirrored foxes, 40 HP each and 10 per hit; the
        // player wins the speed tie on the lower slot, in round 4.
        await using var factory = AppHostFixture.CreateFactory(
            sql.ConnectionStringFor("HotColdBridgeTests"), azurite.ConnectionString, TimeSpan.FromSeconds(15));
        using var client = factory.CreateClient();
        var player = $"player-{Guid.NewGuid():N}";
        var battle = await StartAsync(client, player);
        await StrikeAsync(client, battle.BattleId, player);
        Assert.NotEmpty(await HotRowsAsync(battle.BattleId));

        // Act
        for (var round = 2; round <= 4; round++)
        {
            await StrikeAsync(client, battle.BattleId, player);
        }

        // Assert: the cold store has the battle and the counted win.
        var final = await client.GetFromJsonAsync<BattleResponse>(
            new Uri($"/battles/{battle.BattleId}", UriKind.Relative), Cancellation);
        Assert.NotNull(final);
        Assert.Equal(BattleStatus.Completed, final.Status);
        Assert.Equal(player, final.WinnerId);
        Assert.Equal(
            new PlayerStatsResponse(player, 1, 0),
            await client.GetFromJsonAsync<PlayerStatsResponse>(
                new Uri($"/players/{player}", UriKind.Relative), Cancellation));
        var history = await client.GetFromJsonAsync<IReadOnlyList<BattleHistoryEntryResponse>>(
            new Uri($"/players/{player}/battles", UriKind.Relative), Cancellation);
        Assert.Equal(battle.BattleId, Assert.Single(history!).BattleId);

        // Assert: no hot row of the battle or its monsters is left.
        Assert.Empty(await HotRowsAsync(battle.BattleId));
    }

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

    private static async Task StrikeAsync(HttpClient client, Guid battleId, string player)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri($"/battles/{battleId}/actions", UriKind.Relative),
            new { participantId = player, actorSlot = 0, targetSlot = 1 },
            Cancellation);
        response.EnsureSuccessStatusCode();
    }

    // The grain-state rows of the battle grain and its monster grains.
    // Their keys embed the battle id ("livebattle/<id:N>",
    // "monsterinstance/<id:D>:<slot>"), so a scan of the small test
    // table finds them without depending on Orleans' key encoding.
    private async Task<List<string>> HotRowsAsync(Guid battleId)
    {
        var table = new TableServiceClient(azurite.ConnectionString).GetTableClient(GrainStateTable);
        var rows = new List<string>();
        await foreach (var entity in table.QueryAsync<TableEntity>(cancellationToken: Cancellation))
        {
            var key = $"{entity.PartitionKey}|{entity.RowKey}";
            if (key.Contains(battleId.ToString("N"), StringComparison.OrdinalIgnoreCase)
                || key.Contains(battleId.ToString("D"), StringComparison.OrdinalIgnoreCase))
            {
                rows.Add(key);
            }
        }

        return rows;
    }
}
