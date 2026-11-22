using Battles.Grains.Abstractions;
using Battles.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Players.Contracts;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// The second half of the hot→cold bridge failing (ADR-0003): the
/// snapshot is stored, then the stats report throws. The accepted
/// outcome is that the battle stays uncounted, the cold store keeps
/// the snapshot and the hot state stays behind. The test boots a
/// host of its own, since it swaps the Players contract for a
/// failing one.
/// </summary>
/// <param name="sql">The SQL Server the cold store lives in.</param>
[Collection(IntegrationCollectionDefinition.Name)]
public sealed class HandOverStatsFailureTests(SqlServerFixture sql)
{
    private static readonly string[] FoxTeam = ["ember-fox"];

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandOver_PlayerStatsFails_KeepsSnapshotAndHotState()
    {
        // Arrange: a battle against the bot; whoever wins, the result
        // counts for the player, so the stats report is called.
        await using var host = AppHostFixture.CreateFactory(
            sql.ConnectionStringFor("HandOverStatsFailureTests"), string.Empty, TimeSpan.FromSeconds(15));
        var failing = host.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPlayerStatsService>();
            services.AddSingleton<IPlayerStatsService, FailingPlayerStats>();
        }));
        using var client = failing.CreateClient();
        var player = $"player-{Guid.NewGuid():N}";
        var driver = new HttpBattleDriver(client);
        var battleId = (await driver.StartAgainstBotAsync(player, FoxTeam)).BattleId;

        // Act: the last strike completes the battle and triggers the
        // hand-over; its failure must not reach the player.
        await driver.PlayToTheEndAsync(battleId);

        // Assert: the snapshot made it into the cold store.
        var snapshot = await failing.Services.GetRequiredService<BattleSnapshotReadRepository>()
            .FindAsync(battleId, Cancellation);
        Assert.NotNull(snapshot);
        Assert.Contains(snapshot.WinnerId, new[] { player, "bot" });

        // Assert: the hot state was not cleared, so the battle and
        // its monsters still read from the grains, and agree with it.
        var grains = failing.Services.GetRequiredService<IClusterClient>();
        var hot = await grains.GetGrain<ILiveBattleGrain>(battleId).GetStateAsync();
        Assert.Equal(BattleStatus.Completed, hot.Status);
        Assert.Equal(snapshot.WinnerId, hot.WinnerId);
        var monster = await grains.GetGrain<IMonsterInstanceGrain>($"{battleId}:0").GetSnapshotAsync();
        Assert.Equal(hot.Combatants[0].HitPoints, monster.HitPoints);

        // Assert: the battle endpoint still answers with the result.
        var read = await driver.GetAsync(battleId);
        Assert.Equal(BattleStatus.Completed, read.Status);
        Assert.Equal(snapshot.WinnerId, read.WinnerId);
    }

    // Stands in for a Players module whose database is down.
    private sealed class FailingPlayerStats : IPlayerStatsService
    {
        public Task RecordResultAsync(BattleResultDto result, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The player stats store is unavailable.");
    }
}
