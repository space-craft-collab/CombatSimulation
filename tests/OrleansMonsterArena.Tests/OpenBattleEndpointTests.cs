using System.Net;
using System.Net.Http.Json;
using Battles.Features.GetBattle;
using Battles.Features.ListBattles;
using Battles.Grains.Abstractions;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Plays the "wait for a human" mode over HTTP (ADR-0007): open a
/// battle, list it, let a second human or the bot join.
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class OpenBattleEndpointTests(AppHostFixture host)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Uri Participants(Guid battleId) => new($"/battles/{battleId}/participants", UriKind.Relative);

    private static Uri Bot(Guid battleId) => new($"/battles/{battleId}/bot", UriKind.Relative);

    [Fact]
    public async Task PostBattle_OpponentHuman_Returns201WaitingForPlayers()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var arena = NewArena();

        // Act
        var battle = await OpenAsync(client, arena);

        // Assert
        Assert.Equal(BattleStatus.WaitingForPlayers, battle.Status);
        Assert.Equal(new CombatantResponse(0, "player-1", false, "lava-crab", 60, 60), Assert.Single(battle.Combatants));
    }

    [Fact]
    public async Task GetBattles_OpenBattle_ListsItAsWaiting()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var arena = NewArena();
        var battle = await OpenAsync(client, arena);

        // Act
        var listed = await client.GetFromJsonAsync<List<BattleSummaryResponse>>(arena, Cancellation);

        // Assert
        Assert.NotNull(listed);
        var entry = Assert.Single(listed);
        Assert.Equal(battle.BattleId, entry.BattleId);
        Assert.Equal(BattleStatus.WaitingForPlayers, entry.Status);
        Assert.Equal(["player-1"], entry.ParticipantIds);
    }

    [Fact]
    public async Task PostParticipant_SecondHuman_Returns200InProgress()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var battle = await OpenAsync(client, NewArena());

        // Act
        using var response = await client.PostAsJsonAsync(
            Participants(battle.BattleId), TeamBody("player-2", "ember-fox"), Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var started = await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation);
        Assert.NotNull(started);
        Assert.Equal(BattleStatus.InProgress, started.Status);
        Assert.Equal(1, started.Round);
        Assert.Equal(new CombatantResponse(1, "player-2", false, "ember-fox", 40, 40), started.Combatants[1]);
    }

    [Fact]
    public async Task PostBot_OpenBattle_Returns200AndBotMirrorsHost()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var battle = await OpenAsync(client, NewArena());

        // Act
        using var response = await client.PostAsync(Bot(battle.BattleId), null, Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var started = await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation);
        Assert.NotNull(started);
        Assert.Equal(BattleStatus.InProgress, started.Status);
        Assert.Equal(new CombatantResponse(1, "bot", true, "lava-crab", 60, 60), started.Combatants[1]);
    }

    [Fact]
    public async Task PostParticipant_SeatAlreadyTaken_Returns409()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var battle = await OpenAsync(client, NewArena());
        using var first = await client.PostAsync(Bot(battle.BattleId), null, Cancellation);
        first.EnsureSuccessStatusCode();

        // Act
        using var response = await client.PostAsJsonAsync(
            Participants(battle.BattleId), TeamBody("player-2", "ember-fox"), Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostParticipant_HostJoinsAgain_Returns400()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var battle = await OpenAsync(client, NewArena());

        // Act
        using var response = await client.PostAsJsonAsync(
            Participants(battle.BattleId), TeamBody("player-1", "ember-fox"), Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostParticipant_UnknownBattle_Returns404()
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            Participants(Guid.NewGuid()), TeamBody("player-2", "ember-fox"), Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostAction_WhileWaitingForPlayers_Returns409()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var battle = await OpenAsync(client, NewArena());

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri($"/battles/{battle.BattleId}/actions", UriKind.Relative),
            new { participantId = "player-1", actorSlot = 0, targetSlot = 1 },
            Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static Uri NewArena() => new($"/arenas/test-{Guid.NewGuid()}/battles", UriKind.Relative);

    private static object TeamBody(string playerId, string speciesId) =>
        new { playerId, monsterSpeciesIds = new[] { speciesId } };

    private static async Task<BattleResponse> OpenAsync(HttpClient client, Uri arena)
    {
        using var response = await client.PostAsJsonAsync(
            arena, new { playerId = "player-1", monsterSpeciesIds = new[] { "lava-crab" }, opponent = "Human" }, Cancellation);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation)
            ?? throw new InvalidOperationException("The start endpoint returned no battle.");
    }
}
