using System.Collections.Immutable;
using Battles.Grains.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Drives the battle grains through the co-hosted silo: arena,
/// live battle and monster instances working together (ADR-0007).
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class BattleGrainTests(AppHostFixture host)
{
    // Human Lava Crab (60 HP, 10 ATK, 8 DEF) beats the bot's Ember Fox
    // (40 HP, 14 ATK, 4 DEF): both deal 6 per hit, the fox falls in
    // round 7.
    private static readonly ImmutableArray<ParticipantSetup> OneVsOne =
    [
        new("player-1", false, ["lava-crab"]),
        new("bot-1", true, ["ember-fox"]),
    ];

    // Two humans: a round stays open until both have acted.
    private static readonly ImmutableArray<ParticipantSetup> HumanVsHuman =
    [
        new("player-1", false, ["lava-crab"]),
        new("player-2", false, ["ember-fox"]),
    ];

    // No human at all: the grain timer plays every round. Same
    // matchup as OneVsOne, so the crab wins in round 7.
    private static readonly ImmutableArray<ParticipantSetup> BotVsBot =
    [
        new("bot-1", true, ["lava-crab"]),
        new("bot-2", true, ["ember-fox"]),
    ];

    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    private IGrainFactory Grains => host.Factory.Services.GetRequiredService<IClusterClient>();

    // A fresh arena per test keeps the battle lists independent.
    private IArenaGrain NewArena() => Grains.GetGrain<IArenaGrain>($"test-{Guid.NewGuid()}");

    [Fact]
    public async Task CreateBattleAsync_OneVsOne_StartsInProgressAndIsListed()
    {
        // Arrange
        var arena = NewArena();

        // Act
        var battleId = await arena.CreateBattleAsync(OneVsOne);

        // Assert
        var state = await Grains.GetGrain<ILiveBattleGrain>(battleId).GetStateAsync();
        Assert.Equal(BattleStatus.InProgress, state.Status);
        Assert.Equal(1, state.Round);
        var listed = Assert.Single(await arena.GetBattlesAsync());
        Assert.Equal(new BattleSummary(battleId, BattleStatus.InProgress, 2), listed);
    }

    [Fact]
    public async Task SubmitActionAsync_HumanActs_BotAnswersInSameRound()
    {
        // Arrange
        var battleId = await NewArena().CreateBattleAsync(OneVsOne);
        var battle = Grains.GetGrain<ILiveBattleGrain>(battleId);

        // Act
        await battle.SubmitActionAsync(new BattleAction("player-1", 0, 1, "strike"));

        // Assert
        Assert.Equal(2, (await battle.GetStateAsync()).Round);
        var crab = await Grains.GetGrain<IMonsterInstanceGrain>($"{battleId}:0").GetSnapshotAsync();
        Assert.Equal(54, crab.HitPoints);
    }

    [Fact]
    public async Task SubmitActionAsync_HumanVsBotUntilOneSideFalls_CompletesWithWinner()
    {
        // Arrange
        var arena = NewArena();
        var battleId = await arena.CreateBattleAsync(OneVsOne);
        var battle = Grains.GetGrain<ILiveBattleGrain>(battleId);

        // Act
        for (var round = 0; round < 20 && (await battle.GetStateAsync()).Status == BattleStatus.InProgress; round++)
        {
            await battle.SubmitActionAsync(new BattleAction("player-1", 0, 1, "strike"));
        }

        // Assert
        var state = await battle.GetStateAsync();
        Assert.Equal(BattleStatus.Completed, state.Status);
        Assert.Equal("player-1", state.WinnerId);
        Assert.Equal(7, state.Round);
        var fox = await Grains.GetGrain<IMonsterInstanceGrain>($"{battleId}:1").GetSnapshotAsync();
        Assert.True(fox.IsDefeated);
        Assert.Empty(await arena.GetBattlesAsync());
    }

    [Fact]
    public async Task SubmitActionAsync_SameParticipantTwiceInRound_Throws()
    {
        // Arrange
        var battle = Grains.GetGrain<ILiveBattleGrain>(await NewArena().CreateBattleAsync(HumanVsHuman));
        await battle.SubmitActionAsync(new BattleAction("player-1", 0, 1, "strike"));

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => battle.SubmitActionAsync(new BattleAction("player-1", 0, 1, "strike")));
    }

    [Fact]
    public async Task SubmitActionAsync_TargetingOwnMonster_ThrowsArgumentException()
    {
        // Arrange
        var battle = Grains.GetGrain<ILiveBattleGrain>(await NewArena().CreateBattleAsync(OneVsOne));

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => battle.SubmitActionAsync(new BattleAction("player-1", 0, 0, "strike")));
    }

    [Fact]
    public async Task SubmitActionAsync_AsBot_ThrowsArgumentException()
    {
        // Arrange
        var battle = Grains.GetGrain<ILiveBattleGrain>(await NewArena().CreateBattleAsync(OneVsOne));

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => battle.SubmitActionAsync(new BattleAction("bot-1", 1, 0, "strike")));
    }

    [Fact]
    public async Task SubmitActionAsync_BeforeStart_Throws()
    {
        // Arrange
        var battle = Grains.GetGrain<ILiveBattleGrain>(Guid.NewGuid());

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => battle.SubmitActionAsync(new BattleAction("player-1", 0, 1, "strike")));
    }

    [Fact]
    public async Task StartAsync_UnknownSpecies_ThrowsArgumentException()
    {
        // Arrange
        var battle = Grains.GetGrain<ILiveBattleGrain>(Guid.NewGuid());
        var setup = new BattleSetup("volcano", [new("player-1", false, ["no-such-monster"]), new("bot-1", true, ["ember-fox"])]);

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(() => battle.StartAsync(setup));
    }

    [Fact]
    public async Task StartAsync_Twice_Throws()
    {
        // Arrange
        var battle = Grains.GetGrain<ILiveBattleGrain>(Guid.NewGuid());
        var setup = new BattleSetup("volcano", OneVsOne);
        await battle.StartAsync(setup);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => battle.StartAsync(setup));
    }

    [Fact]
    public async Task CreateBattleAsync_BotVsBot_PlaysToTheEndOnItsOwn()
    {
        // Arrange
        var battle = Grains.GetGrain<ILiveBattleGrain>(await NewArena().CreateBattleAsync(BotVsBot));

        // Act
        var state = await WaitForAsync(battle, state => state.Status == BattleStatus.Completed);

        // Assert
        Assert.Equal("bot-1", state.WinnerId);
        Assert.Equal(7, state.Round);
        Assert.Null(state.TurnDeadline);
    }

    [Fact]
    public async Task CreateBattleAsync_WithHuman_SetsTurnDeadline()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var state = await Grains.GetGrain<ILiveBattleGrain>(await NewArena().CreateBattleAsync(OneVsOne)).GetStateAsync();

        // Assert
        Assert.NotNull(state.TurnDeadline);
        Assert.InRange(state.TurnDeadline.Value, before + AppHostFixture.TurnDeadline, DateTimeOffset.UtcNow + AppHostFixture.TurnDeadline);
    }

    [Fact]
    public async Task ReceiveReminder_OneHumanMissesDeadline_RoundResolvesWithoutThem()
    {
        // Arrange
        var battleId = await NewArena().CreateBattleAsync(HumanVsHuman);
        var battle = Grains.GetGrain<ILiveBattleGrain>(battleId);
        await battle.SubmitActionAsync(new BattleAction("player-1", 0, 1, "strike"));

        // Act
        var state = await WaitForAsync(battle, state => state.Round == 2);

        // Assert
        Assert.Equal(BattleStatus.InProgress, state.Status);
        var crab = await Grains.GetGrain<IMonsterInstanceGrain>($"{battleId}:0").GetSnapshotAsync();
        var fox = await Grains.GetGrain<IMonsterInstanceGrain>($"{battleId}:1").GetSnapshotAsync();
        Assert.Equal(60, crab.HitPoints);
        Assert.Equal(34, fox.HitPoints);
    }

    [Fact]
    public async Task ReceiveReminder_NoHumanActsBeforeDeadline_AbandonsBattleWithoutWinner()
    {
        // Arrange
        var arena = NewArena();
        var battle = Grains.GetGrain<ILiveBattleGrain>(await arena.CreateBattleAsync(OneVsOne));

        // Act
        var state = await WaitForAsync(battle, state => state.Status == BattleStatus.Completed);

        // Assert
        Assert.Null(state.WinnerId);
        Assert.Equal(1, state.Round);
        Assert.Null(state.TurnDeadline);
        Assert.Empty(await arena.GetBattlesAsync());
    }

    [Fact]
    public async Task ApplyDamageAsync_MoreThanHitPoints_ClampsAtZeroAndDefeats()
    {
        // Arrange
        var monster = Grains.GetGrain<IMonsterInstanceGrain>($"{Guid.NewGuid()}:0");
        await monster.InitializeAsync(new MonsterStats("ember-fox", 40, 14, 4, 12));

        // Act
        var snapshot = await monster.ApplyDamageAsync(100);

        // Assert
        Assert.Equal(0, snapshot.HitPoints);
        Assert.True(snapshot.IsDefeated);
    }

    // Polls the battle until the condition holds; the round loop
    // runs on reminders and timers, outside the test's own calls.
    private static async Task<BattleState> WaitForAsync(ILiveBattleGrain battle, Func<BattleState, bool> condition)
    {
        var giveUp = DateTimeOffset.UtcNow + WaitLimit;
        while (true)
        {
            var state = await battle.GetStateAsync();
            if (condition(state))
            {
                return state;
            }

            if (DateTimeOffset.UtcNow > giveUp)
            {
                Assert.Fail($"Battle state {state} did not reach the expected condition within {WaitLimit}.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken);
        }
    }
}
