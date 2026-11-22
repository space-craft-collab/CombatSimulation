using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using Battles.Domain;
using Battles.Features.GetBattle;
using Battles.Grains.Abstractions;
using Battles.Infrastructure;
using Catalog.Infrastructure;
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

    // Three monsters against one: the bigger side wins, whatever the
    // numbers.
    private static readonly ImmutableArray<string> Three = ["moss-golem", "lava-crab", "ember-fox"];

    private static readonly ImmutableArray<string> One = ["frost-owl"];

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private IGrainFactory Grains => host.Factory.Services.GetRequiredService<IClusterClient>();

    private GrainBattleDriver Driver => new(host.Factory.Services);

    private BattleSnapshotReadRepository ColdStore =>
        host.Factory.Services.GetRequiredService<BattleSnapshotReadRepository>();

    [Fact]
    public async Task HandOver_PlayerBeatsBot_StoresSnapshotCountsWinAndClearsHotState()
    {
        // Arrange
        var player = NewPlayerId();
        var battleId = await CreateAsync(new(player, false, Three), new("bot", true, One));

        // Act
        var final = await Driver.PlayToTheEndAsync(battleId);

        // Assert: the snapshot holds the result and every monster as
        // the last round left it: the bot's one fell, one of the
        // player's three still stands.
        var snapshot = await ColdStore.FindAsync(battleId, Cancellation);
        Assert.NotNull(snapshot);
        Assert.Equal(player, snapshot.WinnerId);
        Assert.Equal(final.Round, snapshot.Round);
        var combatants = snapshot.Combatants.OrderBy(combatant => combatant.Slot).ToList();
        Assert.Equal([0, 1, 2, 3], combatants.Select(combatant => combatant.Slot));
        Assert.Equal([player, player, player, "bot"], combatants.Select(combatant => combatant.ParticipantId));
        Assert.Equal([false, false, false, true], combatants.Select(combatant => combatant.IsBot));
        Assert.Equal(Three.Concat(One), combatants.Select(combatant => combatant.SpeciesId));
        Assert.All(combatants, combatant => Assert.InRange(combatant.HitPoints, 0, combatant.MaxHitPoints));
        Assert.Contains(combatants, combatant => !combatant.IsBot && combatant.HitPoints > 0);
        Assert.Equal(0, combatants[3].HitPoints);
        Assert.Equal(new PlayerStatsResponse(player, 1, 0), await GetStatsAsync(player));
        Assert.Equal(BattleStatus.Created, (await Grains.GetGrain<ILiveBattleGrain>(battleId).GetStateAsync()).Status);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Grains.GetGrain<IMonsterInstanceGrain>($"{battleId}:0").GetSnapshotAsync());
    }

    [Fact]
    public async Task HandOver_BotBeatsPlayer_CountsLossForPlayer()
    {
        // Arrange: now the bot fields the bigger side.
        var player = NewPlayerId();
        var battleId = await CreateAsync(new(player, false, One), new("bot", true, Three));

        // Act
        await Driver.PlayToTheEndAsync(battleId);

        // Assert
        Assert.Equal("bot", (await ColdStore.FindAsync(battleId, Cancellation))?.WinnerId);
        Assert.Equal(new PlayerStatsResponse(player, 0, 1), await GetStatsAsync(player));
    }

    [Fact]
    public async Task HandOver_HumanVsHuman_CountsWinAndLoss()
    {
        // Arrange: both act every round; the bigger side wins.
        var winner = NewPlayerId();
        var loser = NewPlayerId();
        var battleId = await CreateAsync(new(winner, false, Three), new(loser, false, One));

        // Act
        await Driver.PlayToTheEndAsync(battleId);

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
        // Arrange: the bot with the bigger side wins.
        var strongBot = NewPlayerId();
        var weakBot = NewPlayerId();

        // Act
        var battleId = await CreateAsync(new(strongBot, true, Three), new(weakBot, true, One));
        var snapshot = await WaitForSnapshotAsync(battleId);

        // Assert
        Assert.Equal(strongBot, snapshot.WinnerId);
        Assert.Equal(HttpStatusCode.NotFound, await GetStatsStatusAsync(strongBot));
        Assert.Equal(HttpStatusCode.NotFound, await GetStatsStatusAsync(weakBot));
    }

    [Fact]
    public async Task HandOver_SnapshotWriteFails_KeepsHotStateAndCountsNobody()
    {
        // Arrange: a snapshot under the battle's id is stored already,
        // so the grain's insert fails on the primary key (ADR-0003).
        var player = NewPlayerId();
        var battleId = await CreateAsync(new(player, false, Three), new("bot", true, One));
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
        await Driver.PlayToTheEndAsync(battleId);

        // Assert
        var state = await Grains.GetGrain<ILiveBattleGrain>(battleId).GetStateAsync();
        Assert.Equal(BattleStatus.Completed, state.Status);
        Assert.Equal(player, state.WinnerId);
        var owl = await Grains.GetGrain<IMonsterInstanceGrain>($"{battleId}:3").GetSnapshotAsync();
        Assert.True(owl.IsDefeated);
        Assert.Equal(HttpStatusCode.NotFound, await GetStatsStatusAsync(player));
    }

    [Fact]
    public async Task GetBattle_HotStateCleared_ReturnsBattleFromColdStore()
    {
        // Arrange
        var player = NewPlayerId();
        var battleId = await CreateAsync(new(player, false, Three), new("bot", true, One));
        await Driver.PlayToTheEndAsync(battleId);
        using var client = host.Factory.CreateClient();

        // Act
        var battle = await client.GetFromJsonAsync<BattleResponse>(
            new Uri($"/battles/{battleId}", UriKind.Relative), Cancellation);

        // Assert
        Assert.NotNull(battle);
        Assert.Equal(BattleStatus.Completed, battle.Status);
        Assert.Equal(player, battle.WinnerId);
        Assert.Null(battle.TurnDeadline);
        Assert.Equal(Three.Concat(One), battle.Combatants.Select(combatant => combatant.SpeciesId));
        Assert.Equal(
            battle.Combatants.Select(combatant => SpeciesSeed.All.Single(species => species.Id == combatant.SpeciesId).MaxHitPoints),
            battle.Combatants.Select(combatant => combatant.MaxHitPoints));
    }

    private static string NewPlayerId() => $"player-{Guid.NewGuid():N}";

    // A fresh arena per battle keeps the seeded arenas' lists clean.
    private Task<Guid> CreateAsync(ParticipantSetup first, ParticipantSetup second) =>
        Grains.GetGrain<IArenaGrain>($"test-{Guid.NewGuid()}").CreateBattleAsync(ImmutableArray.Create(first, second));

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
