using System.Net;
using System.Net.Http.Json;
using Battles.Domain;
using Battles.Features.GetBattle;
using Battles.Features.ListBattles;
using Battles.Grains.Abstractions;
using Catalog.Domain;
using Catalog.Infrastructure;
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
    private static readonly string[] CrabTeam = ["lava-crab"];

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Uri Participants(Guid battleId) => new($"/battles/{battleId}/participants", UriKind.Relative);

    private static Uri Bot(Guid battleId) => new($"/battles/{battleId}/bot", UriKind.Relative);

    private static Uri Bot(Guid battleId, string difficulty) =>
        new($"/battles/{battleId}/bot?difficulty={difficulty}", UriKind.Relative);

    [Fact]
    public async Task PostBattle_OpponentHuman_Returns201WaitingForPlayers()
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        var battle = await OpenAsync(client);

        // Assert
        Assert.Equal(BattleStatus.WaitingForPlayers, battle.Status);
        AssertFresh(Assert.Single(battle.Combatants), 0, "player-1", false, "lava-crab");
    }

    [Fact]
    public async Task PostBattle_OpponentHumanOnEasy_IgnoresTheDifficulty()
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            TestArenas.VolcanoBattles,
            new { playerId = "player-1", monsterSpeciesIds = CrabTeam, opponent = "Human", difficulty = "Easy" },
            Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var battle = await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation);
        Assert.NotNull(battle);
        Assert.Equal(Difficulty.Normal, battle.Difficulty);
        Assert.Equal(Seeded("lava-crab").MaxHitPoints, Assert.Single(battle.Combatants).MaxHitPoints);
    }

    [Fact]
    public async Task GetBattles_OpenBattle_ListsItAsWaiting()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var arena = TestArenas.VolcanoBattles;
        var battle = await OpenAsync(client);

        // Act
        var listed = await client.GetFromJsonAsync<List<BattleSummaryResponse>>(arena, Cancellation);

        // Assert
        Assert.NotNull(listed);
        var entry = Assert.Single(listed, summary => summary.BattleId == battle.BattleId);
        Assert.Equal(BattleStatus.WaitingForPlayers, entry.Status);
        Assert.Equal(["player-1"], entry.ParticipantIds);
    }

    [Fact]
    public async Task PostParticipant_SecondHuman_Returns200InProgress()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var battle = await OpenAsync(client);

        // Act
        using var response = await client.PostAsJsonAsync(
            Participants(battle.BattleId), TeamBody("player-2", "ember-fox"), Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var started = await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation);
        Assert.NotNull(started);
        Assert.Equal(BattleStatus.InProgress, started.Status);
        Assert.Equal(1, started.Round);
        AssertFresh(started.Combatants[1], 1, "player-2", false, "ember-fox");
    }

    [Fact]
    public async Task PostBot_OpenBattle_Returns200AndBotFieldsATeamOfTheHostsSize()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var battle = await OpenAsync(client);

        // Act
        using var response = await client.PostAsync(Bot(battle.BattleId), null, Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var started = await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation);
        Assert.NotNull(started);
        Assert.Equal(BattleStatus.InProgress, started.Status);
        Assert.Equal(2, started.Combatants.Count);
        Assert.Equal(Difficulty.Normal, started.Difficulty);
        AssertFresh(started.Combatants[1], 1, "bot", true, BotPick(0));
        Assert.Equal(Seeded(BotPick(0)).MaxHitPoints, started.Combatants[1].MaxHitPoints);
    }

    [Fact]
    public async Task PostBot_DifficultyEasy_Returns200EasyAndWeakensOnlyTheBotsMonster()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var battle = await OpenAsync(client);

        // Act
        using var response = await client.PostAsync(Bot(battle.BattleId, "Easy"), null, Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var started = await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation);
        Assert.NotNull(started);
        Assert.Equal(Difficulty.Easy, started.Difficulty);
        Assert.Equal(Seeded("lava-crab").MaxHitPoints, started.Combatants[0].MaxHitPoints);
        AssertFresh(started.Combatants[1], 1, "bot", true, BotPick(0));
        Assert.Equal(
            EasyHandicap.Scale(Seeded(BotPick(0)).MaxHitPoints, EasyHandicap.MaxHitPointsPercent),
            started.Combatants[1].MaxHitPoints);
    }

    [Theory]
    [InlineData("Hard")]
    [InlineData("7")]
    public async Task PostBot_UnknownDifficulty_Returns400AndKeepsTheSeatOpen(string difficulty)
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var battle = await OpenAsync(client);

        // Act
        using var response = await client.PostAsync(Bot(battle.BattleId, difficulty), null, Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var unchanged = await new HttpBattleDriver(client).GetAsync(battle.BattleId);
        Assert.Equal(BattleStatus.WaitingForPlayers, unchanged.Status);
    }

    [Fact]
    public async Task PostParticipant_SeatAlreadyTaken_Returns409()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var battle = await OpenAsync(client);
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
        var battle = await OpenAsync(client);

        // Act
        using var response = await client.PostAsJsonAsync(
            Participants(battle.BattleId), TeamBody("player-1", "ember-fox"), Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData("bot", 1)]
    [InlineData("player-2 ", 1)]
    [InlineData("player-2", 0)]
    [InlineData("player-2", 4)]
    public async Task PostParticipant_InvalidRequest_Returns400(string playerId, int teamSize)
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var battle = await OpenAsync(client);
        var team = Enumerable.Repeat("ember-fox", teamSize).ToArray();

        // Act
        using var response = await client.PostAsJsonAsync(
            Participants(battle.BattleId), new { playerId, monsterSpeciesIds = team }, Cancellation);

        // Assert: rejected, and the seat is still free.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var unchanged = await client.GetFromJsonAsync<BattleResponse>(
            new Uri($"/battles/{battle.BattleId}", UriKind.Relative), Cancellation);
        Assert.NotNull(unchanged);
        Assert.Equal(BattleStatus.WaitingForPlayers, unchanged.Status);
    }

    [Fact]
    public async Task PostBot_BattleAlreadyRunning_Returns409()
    {
        // Arrange
        using var client = host.Factory.CreateClient();
        var battle = await OpenAsync(client);
        using var first = await client.PostAsync(Bot(battle.BattleId), null, Cancellation);
        first.EnsureSuccessStatusCode();

        // Act
        using var response = await client.PostAsync(Bot(battle.BattleId), null, Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostBot_UnknownBattle_Returns404()
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        using var response = await client.PostAsync(Bot(Guid.NewGuid()), null, Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
        var battle = await OpenAsync(client);

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri($"/battles/{battle.BattleId}/actions", UriKind.Relative),
            new { participantId = "player-1", actorSlot = 0, targetSlot = 1 },
            Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static object TeamBody(string playerId, string speciesId) =>
        new { playerId, monsterSpeciesIds = new[] { speciesId } };

    private static Task<BattleResponse> OpenAsync(HttpClient client) =>
        new HttpBattleDriver(client).OpenAsync("player-1", CrabTeam);

    // A monster as a battle fields it: in its slot, for its side, at
    // full health.
    // The bot's team in the test hosts: it draws the first species
    // left each time (AppHostFixture.FirstOfWhatIsLeft), so it fields
    // the species in the order of their names.
    private static string BotPick(int position) =>
        SpeciesSeed.All.OrderBy(seeded => seeded.Name, StringComparer.Ordinal).ElementAt(position).Id;

    private static MonsterSpecies Seeded(string speciesId) =>
        SpeciesSeed.All.Single(seeded => seeded.Id == speciesId);

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
