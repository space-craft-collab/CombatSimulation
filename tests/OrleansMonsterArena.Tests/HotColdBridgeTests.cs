using System.Net.Http.Json;
using Azure.Data.Tables;
using Battles.Features.GetBattleHistory;
using Players.Features.GetPlayerStats;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// The hot→cold bridge on the real stores (ADR-0003): a finished
/// battle lands in SQL Server and in the player's stats, its rows
/// and its deadline reminder are gone from Table Storage, and <c>GET /battles/{id}</c> still
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

    // Orleans' default table for the Azure Table reminder service.
    private const string ReminderTable = "OrleansReminders";

    // Two monsters a side: one action per side fells at most one
    // monster each, so the battle is still running after round 1.
    private static readonly string[] Pair = ["ember-fox", "lava-crab"];

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CompleteBattle_OnTableStorage_MovesBattleToColdStoreAndDeletesHotRows()
    {
        // Arrange: one round against the bot puts the battle and its
        // deadline into Table Storage.
        await using var factory = AppHostFixture.CreateFactory(
            sql.ConnectionStringFor("HotColdBridgeTests"), azurite.ConnectionString, TimeSpan.FromSeconds(15));
        using var client = factory.CreateClient();
        var driver = new HttpBattleDriver(client);
        var player = $"player-{Guid.NewGuid():N}";
        var battle = await driver.StartAgainstBotAsync(player, Pair);
        await driver.StrikeAsync(battle.BattleId, player);
        Assert.NotEmpty(await RowsAsync(GrainStateTable, battle.BattleId));
        Assert.NotEmpty(await RowsAsync(ReminderTable, battle.BattleId));

        // Act
        await driver.PlayToTheEndAsync(battle.BattleId);

        // Assert: the cold store has the battle, and the player's stats
        // count its result, whoever won.
        var final = await driver.GetAsync(battle.BattleId);
        var won = BattleAssert.Decided(final) == player;
        Assert.Equal(
            new PlayerStatsResponse(player, won ? 1 : 0, won ? 0 : 1),
            await client.GetFromJsonAsync<PlayerStatsResponse>(
                new Uri($"/players/{player}", UriKind.Relative), Cancellation));
        var history = await client.GetFromJsonAsync<IReadOnlyList<BattleHistoryEntryResponse>>(
            new Uri($"/players/{player}/battles", UriKind.Relative), Cancellation);
        Assert.Equal(battle.BattleId, Assert.Single(history!).BattleId);

        // Assert: no hot row of the battle or its monsters is left.
        Assert.Empty(await RowsAsync(GrainStateTable, battle.BattleId));

        // Assert: the turn-deadline reminder went with it, so nothing
        // wakes the cleared grain again.
        Assert.Empty(await RowsAsync(ReminderTable, battle.BattleId));
    }

    // The rows of the battle grain and its monster grains in a table.
    // Their keys embed the battle id ("livebattle/<id:N>",
    // "monsterinstance/<id:D>:<slot>"), so a scan of the small test
    // table finds them without depending on Orleans' key encoding.
    private async Task<List<string>> RowsAsync(string tableName, Guid battleId)
    {
        var table = new TableServiceClient(azurite.ConnectionString).GetTableClient(tableName);
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
