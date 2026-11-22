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
    // One human against one bot, for the tests that check which
    // actions the grain accepts; none of them plays a round.
    private static readonly ImmutableArray<ParticipantSetup> OneVsOne =
    [
        new("player-1", false, ["lava-crab"]),
        new("bot-1", true, ["ember-fox"]),
    ];

    // Two monsters a side: one action per side fells at most one
    // monster each, so the first round cannot end the battle.
    private static readonly ImmutableArray<ParticipantSetup> PairVsBot =
    [
        new("player-1", false, ["lava-crab", "moss-golem"]),
        new("bot-1", true, ["lava-crab", "moss-golem"]),
    ];

    // Three monsters against one: the bigger side wins, whatever the
    // numbers.
    private static readonly ImmutableArray<ParticipantSetup> ThreeVsBot =
    [
        new("player-1", false, ["moss-golem", "lava-crab", "ember-fox"]),
        new("bot-1", true, ["frost-owl"]),
    ];

    // Two humans: a round stays open until both have acted.
    private static readonly ImmutableArray<ParticipantSetup> HumanVsHuman =
    [
        new("player-1", false, ["lava-crab"]),
        new("player-2", false, ["ember-fox"]),
    ];

    // Two humans with three monsters each: the first monster to fall
    // leaves both sides standing, and a side that misses its turns
    // survives the few strikes these tests deal.
    private static readonly ImmutableArray<ParticipantSetup> ThreeVsThreeHumans =
    [
        new("player-1", false, ["moss-golem", "lava-crab", "ember-fox"]),
        new("player-2", false, ["frost-owl", "ember-fox", "lava-crab"]),
    ];

    // No human at all: the grain timer plays every round. Three
    // monsters against one, so bot-1 wins.
    private static readonly ImmutableArray<ParticipantSetup> BotVsBot =
    [
        new("bot-1", true, ["moss-golem", "lava-crab", "ember-fox"]),
        new("bot-2", true, ["frost-owl"]),
    ];

    private static readonly ParticipantSetup Host = new("player-1", false, ["lava-crab"]);

    // A host and a bot with two monsters each, so round 1 cannot end
    // the battle (see PairVsBot).
    private static readonly ParticipantSetup HostWithPair = new("player-1", false, ["lava-crab", "moss-golem"]);

    private static readonly ParticipantSetup BotWithPair = new("bot-1", true, ["lava-crab", "moss-golem"]);

    private IGrainFactory Grains => host.Factory.Services.GetRequiredService<IClusterClient>();

    // Plays and reads battles; a completed battle is handed over to
    // the cold store and its grain cleared (ADR-0003).
    private GrainBattleDriver Driver => new(host.Factory.Services);

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
        var battleId = await NewArena().CreateBattleAsync(PairVsBot);

        // Act
        var state = await Driver.StrikeAsync(battleId, "player-1");

        // Assert: the round resolved as soon as the human acted, long
        // before the turn deadline, so the bot decided inline.
        Assert.Equal(BattleStatus.InProgress, state.Status);
        Assert.Equal(2, state.Round);
    }

    [Fact]
    public async Task SubmitActionAsync_HumanVsBotUntilOneSideFalls_CompletesWithWinner()
    {
        // Arrange
        var arena = NewArena();
        var battleId = await arena.CreateBattleAsync(ThreeVsBot);

        // Act
        var state = await Driver.PlayToTheEndAsync(battleId);

        // Assert
        Assert.Equal("player-1", BattleAssert.Decided(state));
        Assert.Equal(0, state.Combatants[3].HitPoints);
        Assert.Empty(await arena.GetBattlesAsync());
    }

    [Fact]
    public async Task SubmitActionAsync_FirstMonsterFalls_BattleContinues()
    {
        // Arrange
        var battleId = await NewArena().CreateBattleAsync(ThreeVsThreeHumans);

        // Act: both sides strike every round until a monster is down.
        var midway = await Driver.PlayUntilAsync(battleId, FirstMonsterDown);

        // Assert: one monster down does not end the battle.
        Assert.Equal(BattleStatus.InProgress, midway.Status);
        Assert.Null(midway.WinnerId);
        Assert.True(midway.Round > 1, $"Expected the round to have advanced, but it is {midway.Round}.");
        Assert.NotEmpty(BattleRounds.StandingOf(midway, "player-1"));
        Assert.NotEmpty(BattleRounds.StandingOf(midway, "player-2"));

        // Act
        var final = await Driver.PlayToTheEndAsync(battleId);

        // Assert: the side with a monster left standing wins.
        BattleAssert.Decided(final);
    }

    [Fact]
    public async Task SubmitActionAsync_DefeatedActorSlot_ThrowsArgumentException()
    {
        // Arrange: play until a monster has fallen; its side is still
        // in the fight and may act, but not with that monster.
        var battleId = await NewArena().CreateBattleAsync(ThreeVsThreeHumans);
        var midway = await Driver.PlayUntilAsync(battleId, FirstMonsterDown);
        var fallen = midway.Combatants.First(combatant => combatant.HitPoints == 0);
        var target = midway.Combatants.First(combatant =>
            combatant.ParticipantId != fallen.ParticipantId && combatant.HitPoints > 0);
        var battle = Grains.GetGrain<ILiveBattleGrain>(battleId);

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => battle.SubmitActionAsync(new BattleAction(fallen.ParticipantId, fallen.Slot, target.Slot, "strike")));
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
        var battleId = await NewArena().CreateBattleAsync(BotVsBot);

        // Act
        var state = await Driver.WaitForAsync(battleId, state => state.Status == BattleStatus.Completed);

        // Assert
        Assert.Equal("bot-1", BattleAssert.Decided(state));
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
        var battleId = await NewArena().CreateBattleAsync(ThreeVsThreeHumans);
        await Driver.StrikeAsync(battleId, "player-1");

        // Act
        var state = await Driver.WaitForAsync(battleId, state => state.Round == 2);

        // Assert: player-1's strike landed on player-2's first monster;
        // player-2 never acted, so player-1's side is untouched.
        Assert.Equal(BattleStatus.InProgress, state.Status);
        Assert.All(
            state.Combatants.Where(combatant => combatant.ParticipantId == "player-1"),
            combatant => Assert.Equal(combatant.MaxHitPoints, combatant.HitPoints));
        var struck = state.Combatants[3];
        Assert.True(struck.HitPoints < struck.MaxHitPoints, $"Expected slot 3 to have taken damage: {struck}.");
    }

    [Fact]
    public async Task ReceiveReminder_HumanAbsentSeveralRounds_ResolvesEachRoundOnDeadline()
    {
        // Arrange: player-2 never acts, so every round has to end
        // on a deadline, and each tick has to arm the next one.
        var battleId = await NewArena().CreateBattleAsync(ThreeVsThreeHumans);

        // Act
        await Driver.StrikeAsync(battleId, "player-1");
        var second = await Driver.WaitForAsync(battleId, state => state.Round == 2);
        await Driver.StrikeAsync(battleId, "player-1");
        var third = await Driver.WaitForAsync(battleId, state => state.Round == 3);

        // Assert: each of player-1's strikes landed, and player-2
        // never struck back.
        Assert.Equal(BattleStatus.InProgress, third.Status);
        Assert.NotNull(second.TurnDeadline);
        Assert.NotNull(third.TurnDeadline);
        Assert.True(third.TurnDeadline > second.TurnDeadline);
        Assert.All(
            third.Combatants.Where(combatant => combatant.ParticipantId == "player-1"),
            combatant => Assert.Equal(combatant.MaxHitPoints, combatant.HitPoints));
        Assert.True(HitPointsOf(second, "player-2") < MaxHitPointsOf(second, "player-2"));
        Assert.True(HitPointsOf(third, "player-2") < HitPointsOf(second, "player-2"));
    }

    [Fact]
    public async Task ReceiveReminder_NoHumanActsBeforeDeadline_AbandonsBattleWithoutWinner()
    {
        // Arrange
        var arena = NewArena();
        var battleId = await arena.CreateBattleAsync(OneVsOne);

        // Act
        var state = await Driver.WaitForAsync(battleId, state => state.Status == BattleStatus.Completed);

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
        var battleId = await NewArena().OpenBattleAsync(HostWithPair);
        await Grains.GetGrain<ILiveBattleGrain>(battleId).JoinAsync(BotWithPair);

        // Act
        var state = await Driver.StrikeAsync(battleId, "player-1");

        // Assert: the round resolved as soon as the host acted, long
        // before the turn deadline, so the bot decided inline.
        Assert.Equal(BattleStatus.InProgress, state.Status);
        Assert.Equal(2, state.Round);
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
        var battleId = await arena.OpenBattleAsync(Host);

        // Act
        var state = await Driver.WaitForAsync(battleId, state => state.Status == BattleStatus.Completed);

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

    // The first monster of the battle has fallen.
    private static bool FirstMonsterDown(BattleResponse battle) =>
        battle.Combatants.Any(combatant => combatant.HitPoints == 0);

    private static int HitPointsOf(BattleResponse battle, string participantId) =>
        battle.Combatants.Where(combatant => combatant.ParticipantId == participantId).Sum(combatant => combatant.HitPoints);

    private static int MaxHitPointsOf(BattleResponse battle, string participantId) =>
        battle.Combatants.Where(combatant => combatant.ParticipantId == participantId).Sum(combatant => combatant.MaxHitPoints);
}
