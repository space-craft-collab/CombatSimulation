using System.Collections.Immutable;
using Battles.Features.GetBattle;
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

    // A two-monster side: the bot's Ember Fox (14 ATK) picks the
    // weakest opponent, the player's fox (40 HP, 4 DEF), and fells it
    // in round 4 with 10 per hit. The Moss Golem (80 HP, 10 DEF)
    // takes 4 per hit and deals 4, so it outlasts the bot's fox.
    private static readonly ImmutableArray<ParticipantSetup> TwoVsOne =
    [
        new("player-1", false, ["ember-fox", "moss-golem"]),
        new("bot-1", true, ["ember-fox"]),
    ];

    private static readonly ParticipantSetup Host = new("player-1", false, ["lava-crab"]);

    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    private IGrainFactory Grains => host.Factory.Services.GetRequiredService<IClusterClient>();

    // Reads a battle from either store: a completed battle is handed
    // over to the cold store and its grain cleared (ADR-0003).
    private GetBattleService Battles => host.Factory.Services.GetRequiredService<GetBattleService>();

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
        Assert.Equal(battleId, listed.BattleId);
        Assert.Equal(BattleStatus.InProgress, listed.Status);
        Assert.Equal(["player-1", "bot-1"], listed.ParticipantIds);
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
        var state = await Battles.FindAsync(battleId, TestContext.Current.CancellationToken);
        Assert.NotNull(state);
        Assert.Equal(BattleStatus.Completed, state.Status);
        Assert.Equal("player-1", state.WinnerId);
        Assert.Equal(7, state.Round);
        Assert.Equal(0, state.Combatants[1].HitPoints);
        Assert.Empty(await arena.GetBattlesAsync());
    }

    [Fact]
    public async Task SubmitActionAsync_OneOfTwoMonstersFalls_BattleContinues()
    {
        // Arrange
        var battleId = await NewArena().CreateBattleAsync(TwoVsOne);
        var battle = Grains.GetGrain<ILiveBattleGrain>(battleId);
        var golemStrikes = new BattleAction("player-1", 1, 2, "strike");

        // Act: the player's fox falls in round 4.
        for (var round = 1; round <= 4; round++)
        {
            await battle.SubmitActionAsync(golemStrikes);
        }

        // Assert: one monster down does not end the battle.
        var midway = await battle.GetStateAsync();
        Assert.Equal(BattleStatus.InProgress, midway.Status);
        Assert.Null(midway.WinnerId);
        Assert.Equal(5, midway.Round);
        Assert.Equal([0, 80, 24], midway.Combatants.Select(combatant => combatant.HitPoints));

        // Act: the golem needs six more hits on the bot's fox.
        for (var round = 5; round <= 10; round++)
        {
            await battle.SubmitActionAsync(golemStrikes);
        }

        // Assert: the side with a monster left standing wins.
        var final = await Battles.FindAsync(battleId, TestContext.Current.CancellationToken);
        Assert.NotNull(final);
        Assert.Equal(BattleStatus.Completed, final.Status);
        Assert.Equal("player-1", final.WinnerId);
        Assert.Equal([0, 56, 0], final.Combatants.Select(combatant => combatant.HitPoints));
    }

    [Fact]
    public async Task SubmitActionAsync_DefeatedActorSlot_ThrowsArgumentException()
    {
        // Arrange: play until the player's fox in slot 0 has fallen.
        var battle = Grains.GetGrain<ILiveBattleGrain>(await NewArena().CreateBattleAsync(TwoVsOne));
        for (var round = 1; round <= 4; round++)
        {
            await battle.SubmitActionAsync(new BattleAction("player-1", 1, 2, "strike"));
        }

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => battle.SubmitActionAsync(new BattleAction("player-1", 0, 2, "strike")));
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

    [Theory]
    [InlineData("stranger", 0, 1)]
    [InlineData("player-1", 1, 0)]
    [InlineData("player-1", 5, 1)]
    [InlineData("player-1", -1, 1)]
    [InlineData("player-1", 0, 7)]
    [InlineData("player-1", 0, -1)]
    public async Task SubmitActionAsync_InvalidParticipantOrSlots_ThrowsArgumentException(
        string participantId,
        int actorSlot,
        int targetSlot)
    {
        // Arrange: player-1 holds slot 0, the bot slot 1.
        var battle = Grains.GetGrain<ILiveBattleGrain>(await NewArena().CreateBattleAsync(OneVsOne));

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => battle.SubmitActionAsync(new BattleAction(participantId, actorSlot, targetSlot, "strike")));
        Assert.Equal(1, (await battle.GetStateAsync()).Round);
    }

    [Theory]
    [InlineData("fireball")]
    [InlineData("Strike")]
    [InlineData("")]
    public async Task SubmitActionAsync_UnknownAbility_ThrowsArgumentException(string abilityId)
    {
        // Arrange
        var battle = Grains.GetGrain<ILiveBattleGrain>(await NewArena().CreateBattleAsync(OneVsOne));

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => battle.SubmitActionAsync(new BattleAction("player-1", 0, 1, abilityId)));
        Assert.Equal(1, (await battle.GetStateAsync()).Round);
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
    public async Task StartAsync_SecondParticipantUnknownSpecies_LeavesBattleUntouched()
    {
        // Arrange: the first side is valid, the second is not.
        var battleId = Guid.NewGuid();
        var battle = Grains.GetGrain<ILiveBattleGrain>(battleId);
        ImmutableArray<ParticipantSetup> invalid =
        [
            new("player-1", false, ["lava-crab"]),
            new("player-2", false, ["no-such-monster"]),
        ];

        // Act
        await Assert.ThrowsAsync<ArgumentException>(
            () => battle.StartAsync(new BattleSetup("test-arena", invalid)));

        // Assert: nothing of the first side was added or stored...
        var failed = await battle.GetStateAsync();
        Assert.Equal(BattleStatus.Created, failed.Status);
        Assert.Empty(failed.Combatants);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Grains.GetGrain<IMonsterInstanceGrain>($"{battleId}:0").GetSnapshotAsync());

        // ...so a second, valid start fields exactly the two teams.
        await battle.StartAsync(new BattleSetup("test-arena", HumanVsHuman));
        var started = await battle.GetStateAsync();
        Assert.Equal(["lava-crab", "ember-fox"], started.Combatants.Select(combatant => combatant.SpeciesId));
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
    public async Task ReceiveReminder_HumanAbsentSeveralRounds_ResolvesEachRoundOnDeadline()
    {
        // Arrange: player-2 never acts, so every round has to end
        // on a deadline, and each tick has to arm the next one.
        var battleId = await NewArena().CreateBattleAsync(HumanVsHuman);
        var battle = Grains.GetGrain<ILiveBattleGrain>(battleId);

        // Act
        await battle.SubmitActionAsync(new BattleAction("player-1", 0, 1, "strike"));
        var second = await WaitForAsync(battle, state => state.Round == 2);
        await battle.SubmitActionAsync(new BattleAction("player-1", 0, 1, "strike"));
        var third = await WaitForAsync(battle, state => state.Round == 3);

        // Assert: the crab hit twice for 6, the fox never struck back.
        Assert.Equal(BattleStatus.InProgress, third.Status);
        Assert.NotNull(second.TurnDeadline);
        Assert.NotNull(third.TurnDeadline);
        Assert.True(third.TurnDeadline > second.TurnDeadline);
        Assert.Equal([60, 28], third.Combatants.Select(combatant => combatant.HitPoints));
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
    public async Task OpenBattleAsync_HumanHost_WaitsForPlayersAndIsListed()
    {
        // Arrange
        var arena = NewArena();

        // Act
        var battleId = await arena.OpenBattleAsync(Host);

        // Assert
        var state = await Grains.GetGrain<ILiveBattleGrain>(battleId).GetStateAsync();
        Assert.Equal(BattleStatus.WaitingForPlayers, state.Status);
        Assert.Equal(0, state.Round);
        Assert.NotNull(state.TurnDeadline);
        Assert.Equal("player-1", Assert.Single(state.Combatants).ParticipantId);
        var listed = Assert.Single(await arena.GetBattlesAsync());
        Assert.Equal(BattleStatus.WaitingForPlayers, listed.Status);
        Assert.Equal(["player-1"], listed.ParticipantIds);
    }

    [Fact]
    public async Task JoinAsync_SecondHuman_StartsRoundOne()
    {
        // Arrange
        var arena = NewArena();
        var battle = Grains.GetGrain<ILiveBattleGrain>(await arena.OpenBattleAsync(Host));

        // Act
        await battle.JoinAsync(new ParticipantSetup("player-2", false, ["ember-fox"]));

        // Assert
        var state = await battle.GetStateAsync();
        Assert.Equal(BattleStatus.InProgress, state.Status);
        Assert.Equal(1, state.Round);
        Assert.Equal(["player-1", "player-2"], state.Combatants.Select(combatant => combatant.ParticipantId));
        Assert.Equal(["player-1", "player-2"], Assert.Single(await arena.GetBattlesAsync()).ParticipantIds);
    }

    [Fact]
    public async Task JoinAsync_Bot_ActsInlineOnceTheHostActs()
    {
        // Arrange
        var battle = Grains.GetGrain<ILiveBattleGrain>(await NewArena().OpenBattleAsync(Host));
        await battle.JoinAsync(new ParticipantSetup("bot-1", true, ["lava-crab"]));

        // Act
        await battle.SubmitActionAsync(new BattleAction("player-1", 0, 1, "strike"));

        // Assert: both crabs deal 10 - 8 = 2.
        var state = await battle.GetStateAsync();
        Assert.Equal(2, state.Round);
        Assert.All(state.Combatants, combatant => Assert.Equal(58, combatant.HitPoints));
    }

    [Fact]
    public async Task JoinAsync_HostJoinsOwnBattle_ThrowsArgumentException()
    {
        // Arrange
        var battle = Grains.GetGrain<ILiveBattleGrain>(await NewArena().OpenBattleAsync(Host));

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(() => battle.JoinAsync(Host));
    }

    [Fact]
    public async Task JoinAsync_UnknownSpecies_ThrowsAndKeepsSeatOpen()
    {
        // Arrange
        var battle = Grains.GetGrain<ILiveBattleGrain>(await NewArena().OpenBattleAsync(Host));

        // Act
        await Assert.ThrowsAsync<ArgumentException>(
            () => battle.JoinAsync(new ParticipantSetup("player-2", false, ["ember-fox", "no-such-monster"])));

        // Assert
        var state = await battle.GetStateAsync();
        Assert.Equal(BattleStatus.WaitingForPlayers, state.Status);
        Assert.Single(state.Combatants);
    }

    [Fact]
    public async Task JoinAsync_BattleAlreadyRunning_Throws()
    {
        // Arrange
        var battle = Grains.GetGrain<ILiveBattleGrain>(await NewArena().OpenBattleAsync(Host));
        await battle.JoinAsync(new ParticipantSetup("player-2", false, ["ember-fox"]));

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => battle.JoinAsync(new ParticipantSetup("player-3", false, ["ember-fox"])));
    }

    [Fact]
    public async Task OpenAsync_BotHost_ThrowsArgumentException()
    {
        // Arrange
        var battle = Grains.GetGrain<ILiveBattleGrain>(Guid.NewGuid());

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => battle.OpenAsync("volcano", new ParticipantSetup("bot-1", true, ["lava-crab"])));
    }

    [Fact]
    public async Task ReceiveReminder_NobodyJoinsBeforeDeadline_AbandonsOpenBattle()
    {
        // Arrange
        var arena = NewArena();
        var battle = Grains.GetGrain<ILiveBattleGrain>(await arena.OpenBattleAsync(Host));

        // Act
        var state = await WaitForAsync(battle, state => state.Status == BattleStatus.Completed);

        // Assert
        Assert.Null(state.WinnerId);
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

    [Fact]
    public async Task InitializeAsync_Twice_ThrowsInvalidOperationException()
    {
        // Arrange
        var monster = Grains.GetGrain<IMonsterInstanceGrain>($"{Guid.NewGuid()}:0");
        var stats = new MonsterStats("ember-fox", 40, 14, 4, 12);
        await monster.InitializeAsync(stats);
        await monster.ApplyDamageAsync(10);

        // Act + Assert: a second initialization must not heal it.
        await Assert.ThrowsAsync<InvalidOperationException>(() => monster.InitializeAsync(stats));
        Assert.Equal(30, (await monster.GetSnapshotAsync()).HitPoints);
    }

    // Polls the battle until the condition holds; the round loop
    // runs on reminders and timers, outside the test's own calls.
    private async Task<BattleResponse> WaitForAsync(ILiveBattleGrain battle, Func<BattleResponse, bool> condition)
    {
        var giveUp = DateTimeOffset.UtcNow + WaitLimit;
        while (true)
        {
            var state = await Battles.FindAsync(battle.GetPrimaryKey(), TestContext.Current.CancellationToken)
                ?? throw new InvalidOperationException("Neither store knows the battle.");
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
