using System.Collections.Immutable;
using System.Net.Http.Json;
using Battles.Domain;
using Battles.Features.GetBattle;
using Battles.Grains.Abstractions;
using Battles.Infrastructure;
using Catalog.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Players.Features.GetPlayerStats;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// The Easy difficulty through the grains (ADR-0013): a bot on Easy
/// enters with weakened monsters, nobody else does, and the battle
/// remembers its difficulty in both stores. The expected numbers are
/// taken from the Catalog, so the tests hold under any balance.
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class BattleDifficultyTests(AppHostFixture host)
{
    // Three monsters against one: the bigger side wins, whatever the
    // numbers.
    private static readonly ImmutableArray<string> Three = ["moss-golem", "lava-crab", "ember-fox"];

    private static readonly ImmutableArray<string> One = ["frost-owl"];

    private static readonly ImmutableArray<string> Crab = ["lava-crab"];

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private IGrainFactory Grains => host.Factory.Services.GetRequiredService<IClusterClient>();

    private GrainBattleDriver Driver => new(host.Factory.Services);

    [Fact]
    public async Task CreateBattleAsync_BotOnEasy_WeakensOnlyTheBotsMonsters()
    {
        // Arrange
        var crab = await SpeciesAsync("lava-crab");

        // Act: the same species on both sides.
        var battleId = await CreateAsync(
            new("player-1", false, Crab),
            new("bot", true, Crab, Difficulty.Easy));

        // Assert
        var state = await Battle(battleId).GetStateAsync();
        Assert.Equal(Difficulty.Easy, state.Difficulty);
        Assert.Equal((crab.MaxHitPoints, crab.MaxHitPoints), (state.Combatants[0].HitPoints, state.Combatants[0].MaxHitPoints));
        var weakened = Weakened(crab.MaxHitPoints);
        Assert.True(weakened < crab.MaxHitPoints);
        Assert.Equal((weakened, weakened), (state.Combatants[1].HitPoints, state.Combatants[1].MaxHitPoints));
    }

    [Fact]
    public async Task CreateBattleAsync_BotOnEasy_TellsTheMonsterGrainTheWeakenedStats()
    {
        // Arrange
        var crab = await SpeciesAsync("lava-crab");

        // Act
        var battleId = await CreateAsync(
            new("player-1", false, Crab),
            new("bot", true, Crab, Difficulty.Easy));

        // Assert
        var player = await Grains.GetGrain<IMonsterInstanceGrain>($"{battleId}:0").GetSnapshotAsync();
        var bot = await Grains.GetGrain<IMonsterInstanceGrain>($"{battleId}:1").GetSnapshotAsync();
        Assert.Equal(crab.MaxHitPoints, player.MaxHitPoints);
        Assert.Equal(Weakened(crab.MaxHitPoints), bot.MaxHitPoints);
        Assert.Equal(bot.MaxHitPoints, bot.HitPoints);
    }

    [Fact]
    public async Task CreateBattleAsync_BotOnNormal_FieldsTheSpeciesStats()
    {
        // Arrange
        var crab = await SpeciesAsync("lava-crab");

        // Act
        var battleId = await CreateAsync(new("player-1", false, Crab), new("bot", true, Crab));

        // Assert
        var state = await Battle(battleId).GetStateAsync();
        Assert.Equal(Difficulty.Normal, state.Difficulty);
        Assert.All(state.Combatants, combatant => Assert.Equal(crab.MaxHitPoints, combatant.MaxHitPoints));
        var bot = await Grains.GetGrain<IMonsterInstanceGrain>($"{battleId}:1").GetSnapshotAsync();
        Assert.Equal(crab.MaxHitPoints, bot.MaxHitPoints);
    }

    [Fact]
    public async Task CreateBattleAsync_HumanSaysEasy_IsNeverWeakened()
    {
        // Arrange
        var crab = await SpeciesAsync("lava-crab");

        // Act
        var battleId = await CreateAsync(
            new("player-1", false, Crab, Difficulty.Easy),
            new("player-2", false, Crab, Difficulty.Easy));

        // Assert
        var state = await Battle(battleId).GetStateAsync();
        Assert.Equal(Difficulty.Normal, state.Difficulty);
        Assert.All(state.Combatants, combatant => Assert.Equal(crab.MaxHitPoints, combatant.MaxHitPoints));
    }

    [Fact]
    public async Task JoinAsync_HumanJoinerSaysEasy_IsNeverWeakened()
    {
        // Arrange
        var crab = await SpeciesAsync("lava-crab");
        var battle = Battle(await NewArena().OpenBattleAsync(new("player-1", false, Crab)));

        // Act
        await battle.JoinAsync(new ParticipantSetup("player-2", false, Crab, Difficulty.Easy));

        // Assert
        var state = await battle.GetStateAsync();
        Assert.Equal(Difficulty.Normal, state.Difficulty);
        Assert.All(state.Combatants, combatant => Assert.Equal(crab.MaxHitPoints, combatant.MaxHitPoints));
    }

    [Fact]
    public async Task JoinAsync_BotOnEasy_WeakensOnlyTheBotsMonsters()
    {
        // Arrange
        var crab = await SpeciesAsync("lava-crab");
        var battle = Battle(await NewArena().OpenBattleAsync(new("player-1", false, Crab)));

        // Act
        await battle.JoinAsync(new ParticipantSetup("bot", true, Crab, Difficulty.Easy));

        // Assert
        var state = await battle.GetStateAsync();
        Assert.Equal(Difficulty.Easy, state.Difficulty);
        Assert.Equal(crab.MaxHitPoints, state.Combatants[0].MaxHitPoints);
        Assert.Equal(Weakened(crab.MaxHitPoints), state.Combatants[1].MaxHitPoints);
    }

    [Fact]
    public async Task HandOver_EasyBattle_ColdStoreKeepsTheDifficultyAndTheWeakenedFullHealth()
    {
        // Arrange
        var owl = await SpeciesAsync("frost-owl");
        var player = NewPlayerId();
        var battleId = await CreateAsync(new(player, false, Three), new("bot", true, One, Difficulty.Easy));

        // Act
        var final = await Driver.PlayToTheEndAsync(battleId);

        // Assert: read back from the cold store, the grain is cleared.
        Assert.Equal(BattleStatus.Created, (await Battle(battleId).GetStateAsync()).Status);
        var snapshot = await host.Factory.Services.GetRequiredService<BattleSnapshotReadRepository>()
            .FindAsync(battleId, Cancellation);
        Assert.NotNull(snapshot);
        Assert.Equal(BattleDifficulty.Easy, snapshot.Difficulty);
        Assert.Equal(Difficulty.Easy, final.Difficulty);
        var bot = Assert.Single(final.Combatants, combatant => combatant.IsBot);
        Assert.Equal(Weakened(owl.MaxHitPoints), bot.MaxHitPoints);
    }

    [Fact]
    public async Task HandOver_PlayerBeatsBotOnEasy_CountsTheWinLikeAnyOther()
    {
        // Arrange
        var player = NewPlayerId();
        var battleId = await CreateAsync(new(player, false, Three), new("bot", true, One, Difficulty.Easy));

        // Act
        await Driver.PlayToTheEndAsync(battleId);

        // Assert
        using var client = host.Factory.CreateClient();
        var stats = await client.GetFromJsonAsync<PlayerStatsResponse>(
            new Uri($"/players/{player}", UriKind.Relative), Cancellation);
        Assert.Equal(new PlayerStatsResponse(player, 1, 0), stats);
    }

    [Fact]
    public async Task HandOver_NormalBattle_ColdStoreReadsAsNormal()
    {
        // Arrange
        var battleId = await CreateAsync(new(NewPlayerId(), false, Three), new("bot", true, One));

        // Act
        var final = await Driver.PlayToTheEndAsync(battleId);

        // Assert
        Assert.Equal(BattleStatus.Created, (await Battle(battleId).GetStateAsync()).Status);
        Assert.Equal(Difficulty.Normal, final.Difficulty);
    }

    // The full health a bot's monster has on Easy; the arithmetic
    // itself is pinned in EasyHandicapTests.
    private static int Weakened(int maxHitPoints) => EasyHandicap.Scale(maxHitPoints, EasyHandicap.MaxHitPointsPercent);

    private async Task<MonsterSpeciesDto> SpeciesAsync(string speciesId) =>
        await host.Factory.Services.GetRequiredService<ICatalogQueryService>().FindSpeciesAsync(speciesId, Cancellation)
        ?? throw new InvalidOperationException($"The Catalog seeds no species '{speciesId}'.");

    // Fresh per test, since the host's database is shared.
    private static string NewPlayerId() => $"player-{Guid.NewGuid():N}";

    private ILiveBattleGrain Battle(Guid battleId) => Grains.GetGrain<ILiveBattleGrain>(battleId);

    // A fresh arena per test keeps the battle lists independent.
    private IArenaGrain NewArena() => Grains.GetGrain<IArenaGrain>($"test-{Guid.NewGuid()}");

    private Task<Guid> CreateAsync(ParticipantSetup first, ParticipantSetup second) =>
        NewArena().CreateBattleAsync([first, second]);
}
