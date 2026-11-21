using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using Battles.Domain;
using Battles.Features.GetBattle;
using Battles.Grains.Abstractions;
using Battles.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Players.Features.GetPlayerStats;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// The hot→cold bridge (ADR-0003): a completed battle lands in the
/// cold store, a decided one in the player stats, and the grains'
/// hot state is cleared. A failed snapshot write keeps the hot state
/// and counts nothing. Every test uses fresh player ids, since the
/// host's database is shared.
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class BattleHandOverTests(AppHostFixture host)
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private IGrainFactory Grains => host.Factory.Services.GetRequiredService<IClusterClient>();

    private BattleSnapshotReadRepository ColdStore =>
        host.Factory.Services.GetRequiredService<BattleSnapshotReadRepository>();

    [Fact]
    public async Task HandOver_PlayerBeatsBot_StoresSnapshotCountsWinAndClearsHotState()
    {
        // Arrange: Lava Crab beats Ember Fox in round 7.
        var player = NewPlayerId();
        var battleId = await CreateAsync(new(player, false, ["lava-crab"]), new("bot", true, ["ember-fox"]));

        // Act
        await PlayToTheEndAsync(battleId, player);

        // Assert
        var snapshot = await ColdStore.FindAsync(battleId, Cancellation);
        Assert.NotNull(snapshot);
        Assert.Equal(player, snapshot.WinnerId);
        Assert.Equal(7, snapshot.Round);
        Assert.Equal(
            [new SnapshotCombatant(0, player, false, "lava-crab", 18, 60), new SnapshotCombatant(1, "bot", true, "ember-fox", 0, 40)],
            snapshot.Combatants.OrderBy(combatant => combatant.Slot));
        Assert.Equal(new PlayerStatsResponse(player, 1, 0), await GetStatsAsync(player));
        Assert.Equal(BattleStatus.Created, (await Grains.GetGrain<ILiveBattleGrain>(battleId).GetStateAsync()).Status);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Grains.GetGrain<IMonsterInstanceGrain>($"{battleId}:0").GetSnapshotAsync());
    }

    [Fact]
    public async Task HandOver_BotBeatsPlayer_CountsLossForPlayer()
    {
        // Arrange: the same matchup the other way round.
        var player = NewPlayerId();
        var battleId = await CreateAsync(new(player, false, ["ember-fox"]), new("bot", true, ["lava-crab"]));

        // Act
        await PlayToTheEndAsync(battleId, player);

        // Assert
        Assert.Equal("bot", (await ColdStore.FindAsync(battleId, Cancellation))?.WinnerId);
        Assert.Equal(new PlayerStatsResponse(player, 0, 1), await GetStatsAsync(player));
    }

    [Fact]
    public async Task HandOver_HumanVsHuman_CountsWinAndLoss()
    {
        // Arrange: both act every round; the crab wins in round 7.
        var winner = NewPlayerId();
        var loser = NewPlayerId();
        var battleId = await CreateAsync(new(winner, false, ["lava-crab"]), new(loser, false, ["ember-fox"]));
        var battle = Grains.GetGrain<ILiveBattleGrain>(battleId);

        // Act
        while ((await battle.GetStateAsync()).Status == BattleStatus.InProgress)
        {
            await battle.SubmitActionAsync(new BattleAction(winner, 0, 1, "strike"));
            await battle.SubmitActionAsync(new BattleAction(loser, 1, 0, "strike"));
        }

        // Assert
        Assert.Equal(new PlayerStatsResponse(winner, 1, 0), await GetStatsAsync(winner));
        Assert.Equal(new PlayerStatsResponse(loser, 0, 1), await GetStatsAsync(loser));
    }

    [Fact]
    public async Task HandOver_Abandoned_StoresSnapshotWithoutWinnerAndCountsNobody()
    {
        // Arrange
        var player = NewPlayerId();
        var battleId = await CreateAsync(new(player, false, ["lava-crab"]), new("bot", true, ["ember-fox"]));

        // Act: nobody acts before the deadline.
        var snapshot = await WaitForSnapshotAsync(battleId);

        // Assert
        Assert.Null(snapshot.WinnerId);
        Assert.Equal(HttpStatusCode.NotFound, await GetStatsStatusAsync(player));
    }

    [Fact]
    public async Task HandOver_BotVsBot_StoresSnapshotAndCountsNobody()
    {
        // Arrange
        var crabBot = NewPlayerId();
        var foxBot = NewPlayerId();

        // Act
        var battleId = await CreateAsync(new(crabBot, true, ["lava-crab"]), new(foxBot, true, ["ember-fox"]));
        var snapshot = await WaitForSnapshotAsync(battleId);

        // Assert
        Assert.Equal(crabBot, snapshot.WinnerId);
        Assert.Equal(HttpStatusCode.NotFound, await GetStatsStatusAsync(crabBot));
        Assert.Equal(HttpStatusCode.NotFound, await GetStatsStatusAsync(foxBot));
    }

    [Fact]
    public async Task HandOver_SnapshotWriteFails_KeepsHotStateAndCountsNobody()
    {
        // Arrange: a snapshot under the battle's id is stored already,
        // so the grain's insert fails on the primary key (ADR-0003).
        var player = NewPlayerId();
        var battleId = await CreateAsync(new(player, false, ["lava-crab"]), new("bot", true, ["ember-fox"]));
        await host.Factory.Services.GetRequiredService<BattleSnapshotWriteRepository>().AddAsync(
            new BattleSnapshot
            {
                BattleId = battleId,
                ArenaId = "test-collision",
                Round = 0,
                WinnerId = null,
                CompletedAt = DateTimeOffset.UtcNow,
                Combatants = [],
            },
            Cancellation);

        // Act
        await PlayToTheEndAsync(battleId, player);

        // Assert
        var state = await Grains.GetGrain<ILiveBattleGrain>(battleId).GetStateAsync();
        Assert.Equal(BattleStatus.Completed, state.Status);
        Assert.Equal(player, state.WinnerId);
        var fox = await Grains.GetGrain<IMonsterInstanceGrain>($"{battleId}:1").GetSnapshotAsync();
        Assert.True(fox.IsDefeated);
        Assert.Equal(HttpStatusCode.NotFound, await GetStatsStatusAsync(player));
    }

    [Fact]
    public async Task GetBattle_HotStateCleared_ReturnsBattleFromColdStore()
    {
        // Arrange
        var player = NewPlayerId();
        var battleId = await CreateAsync(new(player, false, ["lava-crab"]), new("bot", true, ["ember-fox"]));
        await PlayToTheEndAsync(battleId, player);
        using var client = host.Factory.CreateClient();

        // Act
        var battle = await client.GetFromJsonAsync<BattleResponse>(
            new Uri($"/battles/{battleId}", UriKind.Relative), Cancellation);

        // Assert
        Assert.NotNull(battle);
        Assert.Equal(BattleStatus.Completed, battle.Status);
        Assert.Equal(player, battle.WinnerId);
        Assert.Null(battle.TurnDeadline);
        Assert.Equal([60, 40], battle.Combatants.Select(combatant => combatant.MaxHitPoints));
    }

    private static string NewPlayerId() => $"player-{Guid.NewGuid():N}";

    // A fresh arena per battle keeps the seeded arenas' lists clean.
    private Task<Guid> CreateAsync(ParticipantSetup first, ParticipantSetup second) =>
        Grains.GetGrain<IArenaGrain>($"test-{Guid.NewGuid()}").CreateBattleAsync(ImmutableArray.Create(first, second));

    // Strikes with the player's one monster at the bot's one monster
    // until the battle ended and the grain handed it over.
    private async Task PlayToTheEndAsync(Guid battleId, string playerId)
    {
        var battle = Grains.GetGrain<ILiveBattleGrain>(battleId);
        for (var round = 0; round < 20 && (await battle.GetStateAsync()).Status == BattleStatus.InProgress; round++)
        {
            await battle.SubmitActionAsync(new BattleAction(playerId, 0, 1, "strike"));
        }
    }

    // Polls the cold store; deadlines and the bot loop end a battle
    // outside the test's own calls.
    private async Task<BattleSnapshot> WaitForSnapshotAsync(Guid battleId)
    {
        var giveUp = DateTimeOffset.UtcNow + WaitLimit;
        while (true)
        {
            if (await ColdStore.FindAsync(battleId, Cancellation) is { } snapshot)
            {
                return snapshot;
            }

            if (DateTimeOffset.UtcNow > giveUp)
            {
                Assert.Fail($"Battle {battleId} did not reach the cold store within {WaitLimit}.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), Cancellation);
        }
    }

    private async Task<PlayerStatsResponse?> GetStatsAsync(string playerId)
    {
        using var client = host.Factory.CreateClient();

        return await client.GetFromJsonAsync<PlayerStatsResponse>(
            new Uri($"/players/{playerId}", UriKind.Relative), Cancellation);
    }

    private async Task<HttpStatusCode> GetStatsStatusAsync(string playerId)
    {
        using var client = host.Factory.CreateClient();
        using var response = await client.GetAsync(new Uri($"/players/{playerId}", UriKind.Relative), Cancellation);

        return response.StatusCode;
    }
}
