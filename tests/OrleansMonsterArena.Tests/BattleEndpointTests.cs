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
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Uri NewArenaBattles() => new($"/arenas/test-{Guid.NewGuid()}/battles", UriKind.Relative);

    private static Uri Actions(Guid battleId) => new($"/battles/{battleId}/actions", UriKind.Relative);

    [Fact]
    public async Task PostBattle_ValidTeam_Returns201AndBotMirrorsTeam()
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            NewArenaBattles(), StartBody("lava-crab"), Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var battle = await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation);
        Assert.NotNull(battle);
        Assert.Equal(BattleStatus.InProgress, battle.Status);
        Assert.NotNull(battle.TurnDeadline);
        Assert.Equal(new Uri($"/battles/{battle.BattleId}", UriKind.Relative), response.Headers.Location);
        Assert.Collection(
            battle.Combatants,
            player => Assert.Equal(new CombatantResponse(0, "player-1", false, "lava-crab", 60, 60), player),
            bot => Assert.Equal(new CombatantResponse(1, "bot", true, "lava-crab", 60, 60), bot));
    }

    [Fact]
    public async Task PostBattle_UnknownSpecies_Returns400()
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            NewArenaBattles(), StartBody("no-such-monster"), Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("", "lava-crab")]
    [InlineData("bot", "lava-crab")]
    [InlineData("player-1", null)]
    public async Task PostBattle_InvalidRequest_Returns400(string playerId, string? speciesId)
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        string[] team = speciesId is null ? [] : [speciesId];

        // Act
        using var response = await client.PostAsJsonAsync(
            NewArenaBattles(), new { playerId, monsterSpeciesIds = team }, Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAction_HumanStrikes_Returns200WithNextRound()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var battle = await StartAsync(client, "lava-crab");

        // Act
        using var response = await client.PostAsJsonAsync(
            Actions(battle.BattleId), new { participantId = "player-1", actorSlot = 0, targetSlot = 1 }, Cancellation);

        // Assert: both crabs deal 10 - 8 = 2.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var next = await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation);
        Assert.NotNull(next);
        Assert.Equal(2, next.Round);
        Assert.All(next.Combatants, combatant => Assert.Equal(58, combatant.HitPoints));
    }

    [Fact]
    public async Task PostAction_AfterBattleCompleted_Returns409()
    {
        // Arrange: mirrored foxes deal 10 per hit, 40 HP each; the
        // player wins the speed tie on the lower slot, in round 4.
        using var client = host.Factory.CreateClient();
        var battle = await StartAsync(client, "ember-fox");
        var strike = new { participantId = "player-1", actorSlot = 0, targetSlot = 1 };
        for (var round = 0; round < 4; round++)
        {
            using var played = await client.PostAsJsonAsync(Actions(battle.BattleId), strike, Cancellation);
            Assert.Equal(HttpStatusCode.OK, played.StatusCode);
        }

        // Act
        using var response = await client.PostAsJsonAsync(Actions(battle.BattleId), strike, Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var final = await client.GetFromJsonAsync<BattleResponse>(
            new Uri($"/battles/{battle.BattleId}", UriKind.Relative), Cancellation);
        Assert.NotNull(final);
        Assert.Equal(BattleStatus.Completed, final.Status);
        Assert.Equal("player-1", final.WinnerId);
    }

    [Fact]
    public async Task PostAction_TargetingOwnMonster_Returns400()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var battle = await StartAsync(client, "lava-crab");

        // Act
        using var response = await client.PostAsJsonAsync(
            Actions(battle.BattleId), new { participantId = "player-1", actorSlot = 0, targetSlot = 0 }, Cancellation);

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

    private static async Task<BattleResponse> StartAsync(HttpClient client, string speciesId)
    {
        using var response = await client.PostAsJsonAsync(
            NewArenaBattles(), StartBody(speciesId), Cancellation);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation)
            ?? throw new InvalidOperationException("The start endpoint returned no battle.");
    }
}
