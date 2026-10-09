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
    // Lava Crab (60 HP, 10 ATK, 8 DEF) beats Ember Fox (40 HP, 14 ATK,
    // 4 DEF): both deal 6 per hit, the fox falls in round 7.
    private static readonly ImmutableArray<ParticipantSetup> OneVsOne =
    [
        new("player-1", false, ["lava-crab"]),
        new("bot-1", true, ["ember-fox"]),
    ];

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
    public async Task SubmitActionAsync_UntilOneSideFalls_CompletesWithWinner()
    {
        // Arrange
        var arena = NewArena();
        var battleId = await arena.CreateBattleAsync(OneVsOne);
        var battle = Grains.GetGrain<ILiveBattleGrain>(battleId);

        // Act
        for (var round = 0; round < 20 && (await battle.GetStateAsync()).Status == BattleStatus.InProgress; round++)
        {
            await battle.SubmitActionAsync(new BattleAction("player-1", 0, 1, "strike"));
            if ((await battle.GetStateAsync()).Status == BattleStatus.InProgress)
            {
                await battle.SubmitActionAsync(new BattleAction("bot-1", 1, 0, "strike"));
            }
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
        var battle = Grains.GetGrain<ILiveBattleGrain>(await NewArena().CreateBattleAsync(OneVsOne));
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
}
