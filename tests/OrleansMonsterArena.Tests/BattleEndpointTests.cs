using System.Net;
using System.Net.Http.Json;
using Battles.Features.GetBattle;
using Battles.Grains.Abstractions;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Plays the Battles feature slices over HTTP: start a battle
/// against the bot, submit turns, read the state back.
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class BattleEndpointTests(AppHostFixture host)
{
    // Two monsters a side: one action per side fells at most one
    // monster each, so the first round cannot end the battle.
    private static readonly string[] Pair = ["lava-crab", "moss-golem"];

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Uri Actions(Guid battleId) => HttpBattleDriver.Actions(battleId);

    [Fact]
    public async Task PostBattle_ValidTeam_Returns201AndBotMirrorsTeam()
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            TestArenas.VolcanoBattles, StartBody("lava-crab"), Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var battle = await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation);
        Assert.NotNull(battle);
        Assert.Equal(BattleStatus.InProgress, battle.Status);
        Assert.NotNull(battle.TurnDeadline);
        Assert.Equal(new Uri($"/battles/{battle.BattleId}", UriKind.Relative), response.Headers.Location);
        Assert.Collection(
            battle.Combatants,
            player => AssertFresh(player, 0, "player-1", false, "lava-crab"),
            bot => AssertFresh(bot, 1, "bot", true, "lava-crab"));
    }

    [Fact]
    public async Task PostBattle_UnknownSpecies_Returns400()
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            TestArenas.VolcanoBattles, StartBody("no-such-monster"), Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("", "lava-crab")]
    [InlineData("bot", "lava-crab")]
    [InlineData("this-player-id-has-sixty-five-characters-one-more-than-allowed-64", "lava-crab")]
    [InlineData("player-1 ", "lava-crab")]
    [InlineData(" player-1", "lava-crab")]
    [InlineData("player-1", "Lava-Crab")]
    [InlineData("player-1", null)]
    public async Task PostBattle_InvalidRequest_Returns400(string playerId, string? speciesId)
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        string[] team = speciesId is null ? [] : [speciesId];

        // Act
        using var response = await client.PostAsJsonAsync(
            TestArenas.VolcanoBattles, new { playerId, monsterSpeciesIds = team }, Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostBattle_FourMonsters_Returns400()
    {
        // Arrange: one more than a side may field.
        using var client = host.Factory.CreateClient();
        string[] team = ["lava-crab", "lava-crab", "lava-crab", "lava-crab"];

        // Act
        using var response = await client.PostAsJsonAsync(
            TestArenas.VolcanoBattles, new { playerId = "player-1", monsterSpeciesIds = team }, Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostBattle_ThreeMonsters_BotSlotsFollowPlayerSlots()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        string[] team = ["ember-fox", "lava-crab", "moss-golem"];

        // Act
        using var response = await client.PostAsJsonAsync(
            TestArenas.VolcanoBattles, new { playerId = "player-1", monsterSpeciesIds = team }, Cancellation);

        // Assert: the player takes slots 0-2 in pick order, the
        // mirroring bot slots 3-5.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var battle = await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation);
        Assert.NotNull(battle);
        Assert.Collection(
            battle.Combatants,
            combatant => AssertFresh(combatant, 0, "player-1", false, "ember-fox"),
            combatant => AssertFresh(combatant, 1, "player-1", false, "lava-crab"),
            combatant => AssertFresh(combatant, 2, "player-1", false, "moss-golem"),
            combatant => AssertFresh(combatant, 3, "bot", true, "ember-fox"),
            combatant => AssertFresh(combatant, 4, "bot", true, "lava-crab"),
            combatant => AssertFresh(combatant, 5, "bot", true, "moss-golem"));
    }

    [Fact]
    public async Task PostAction_HumanStrikes_Returns200WithNextRound()
    {
        // Arrange: the player holds slots 0-1, the bot 2-3.
        using var client = host.Factory.CreateClient();
        var battle = await new HttpBattleDriver(client).StartAgainstBotAsync("player-1", Pair);

        // Act
        using var response = await client.PostAsJsonAsync(
            Actions(battle.BattleId), new { participantId = "player-1", actorSlot = 0, targetSlot = 2 }, Cancellation);

        // Assert: the bot acted inline, so the answer already shows
        // the next round.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var next = await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation);
        Assert.NotNull(next);
        Assert.Equal(BattleStatus.InProgress, next.Status);
        Assert.Equal(2, next.Round);
    }

    [Fact]
    public async Task PostAction_AfterBattleCompleted_Returns409()
    {
        // Arrange: play against the bot until either side has won.
        using var client = host.Factory.CreateClient();
        var driver = new HttpBattleDriver(client);
        var battle = await driver.StartAgainstBotAsync("player-1", "ember-fox");
        await driver.PlayToTheEndAsync(battle.BattleId);

        // Act
        using var response = await client.PostAsJsonAsync(
            Actions(battle.BattleId), new { participantId = "player-1", actorSlot = 0, targetSlot = 1 }, Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var final = await driver.GetAsync(battle.BattleId);
        BattleAssert.Decided(final);
    }

    [Fact]
    public async Task PostAction_TargetingOwnMonster_Returns400()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var battle = await new HttpBattleDriver(client).StartAgainstBotAsync("player-1", "lava-crab");

        // Act
        using var response = await client.PostAsJsonAsync(
            Actions(battle.BattleId), new { participantId = "player-1", actorSlot = 0, targetSlot = 0 }, Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAction_UnknownAbility_Returns400()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var battle = await new HttpBattleDriver(client).StartAgainstBotAsync("player-1", "lava-crab");

        // Act
        using var response = await client.PostAsJsonAsync(
            Actions(battle.BattleId),
            new { participantId = "player-1", actorSlot = 0, targetSlot = 1, abilityId = "fireball" },
            Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAction_UnknownBattle_Returns404()
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            Actions(Guid.NewGuid()), new { participantId = "player-1", actorSlot = 0, targetSlot = 1 }, Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBattle_UnknownBattle_Returns404()
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri($"/battles/{Guid.NewGuid()}", UriKind.Relative), Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static object StartBody(string speciesId) =>
        new { playerId = "player-1", monsterSpeciesIds = new[] { speciesId } };

    // A monster as a battle fields it: in its slot, for its side, at
    // full health.
    private static void AssertFresh(
        CombatantResponse combatant,
        int slot,
        string participantId,
        bool isBot,
        string speciesId)
    {
        Assert.Equal(slot, combatant.Slot);
        Assert.Equal(participantId, combatant.ParticipantId);
        Assert.Equal(isBot, combatant.IsBot);
        Assert.Equal(speciesId, combatant.SpeciesId);
        Assert.True(combatant.MaxHitPoints > 0, $"Expected positive max hit points: {combatant}.");
        Assert.Equal(combatant.MaxHitPoints, combatant.HitPoints);
    }
}
