using System.Net.Http.Json;
using Battles.Features.GetBattle;
using Battles.Features.ListBattles;
using Battles.Grains.Abstractions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Proves that a live battle survives a host restart on Table
/// Storage: its state, its round and its pending turn deadline,
/// and an open battle its place in the arena's list and its free
/// seat.
/// </summary>
/// <param name="azurite">The Table Storage both hosts share.</param>
/// <param name="sql">The SQL Server both hosts share.</param>
[Collection(IntegrationCollectionDefinition.Name)]
[Trait("Category", TestCategories.Integration)]
public sealed class HostRestartTests(AzuriteFixture azurite, SqlServerFixture sql) : IClassFixture<AzuriteFixture>
{
    // Long enough that the deadline cannot pass while the first host
    // shuts down and the second one boots.
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);

    private static readonly string[] CrabTeam = ["lava-crab"];

    private static readonly string[] FoxTeam = ["ember-fox"];

    // Two monsters a side: one action per side fells at most one
    // monster each, so the battle is still running after round 1.
    private static readonly string[] Pair = ["lava-crab", "moss-golem"];

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RestartHost_BattleInProgress_KeepsStateAndFiresPendingDeadline()
    {
        // Arrange: play one round on the first host, then shut it down.
        BattleResponse beforeRestart;
        await using (var first = CreateFactory())
        {
            using var client = first.CreateClient();
            var driver = new HttpBattleDriver(client);
            var battle = await driver.StartAgainstBotAsync("player-1", Pair);
            beforeRestart = await driver.StrikeAsync(battle.BattleId, "player-1");
        }

        // Act
        await using var second = CreateFactory();
        using var restartedClient = second.CreateClient();
        var afterRestart = await new HttpBattleDriver(restartedClient).GetAsync(beforeRestart.BattleId);

        // Assert: state and round come back from storage unchanged.
        Assert.Equal(BattleStatus.InProgress, beforeRestart.Status);
        Assert.Equal(2, beforeRestart.Round);
        Assert.Equal(BattleStatus.InProgress, afterRestart.Status);
        Assert.Equal(beforeRestart.Round, afterRestart.Round);
        Assert.Equal(beforeRestart.TurnDeadline, afterRestart.TurnDeadline);
        Assert.Equal(Monsters(beforeRestart), Monsters(afterRestart));

        // Assert: nobody acts in round 2, so the deadline reminder,
        // registered on the first host, abandons the battle on the
        // second one.
        var final = await WaitForCompletionAsync(restartedClient, afterRestart.BattleId);
        Assert.Null(final.WinnerId);
    }

    [Fact]
    public async Task RestartHost_OpenBattleWaiting_StaysListedAndJoinable()
    {
        // Arrange: a human opens a battle on the first host, which
        // shuts down before anyone joins.
        var host = $"player-{Guid.NewGuid():N}";
        BattleResponse opened;
        await using (var first = CreateFactory())
        {
            using var client = first.CreateClient();
            using var response = await client.PostAsJsonAsync(
                TestArenas.VolcanoBattles,
                new { playerId = host, monsterSpeciesIds = CrabTeam, opponent = "Human" },
                Cancellation);
            response.EnsureSuccessStatusCode();
            opened = await ReadBattleAsync(response);
        }

        // Act
        await using var second = CreateFactory();
        using var restartedClient = second.CreateClient();
        var listed = await restartedClient.GetFromJsonAsync<List<BattleSummaryResponse>>(
            TestArenas.VolcanoBattles, Cancellation);
        using var joined = await restartedClient.PostAsJsonAsync(
            new Uri($"/battles/{opened.BattleId}/participants", UriKind.Relative),
            new { playerId = "player-2", monsterSpeciesIds = FoxTeam },
            Cancellation);

        // Assert: the arena grain still lists the battle as waiting.
        Assert.NotNull(listed);
        var entry = Assert.Single(listed, summary => summary.BattleId == opened.BattleId);
        Assert.Equal(BattleStatus.WaitingForPlayers, entry.Status);
        Assert.Equal([host], entry.ParticipantIds);

        // Assert: the seat is still free and the battle starts.
        joined.EnsureSuccessStatusCode();
        var started = await ReadBattleAsync(joined);
        Assert.Equal(BattleStatus.InProgress, started.Status);
        Assert.Equal(1, started.Round);
        Assert.Equal([host, "player-2"], started.Combatants.Select(combatant => combatant.ParticipantId));
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        AppHostFixture.CreateFactory(sql.ConnectionStringFor("HostRestartTests"), azurite.ConnectionString, Deadline);

    // The monster slots as values, so the comparison does not depend
    // on which members CombatantResponse has.
    private static List<(int Slot, string ParticipantId, string SpeciesId, int HitPoints, int MaxHitPoints)> Monsters(
        BattleResponse battle) =>
        [.. battle.Combatants.Select(combatant => (
            combatant.Slot, combatant.ParticipantId, combatant.SpeciesId, combatant.HitPoints, combatant.MaxHitPoints))];

    private static async Task<BattleResponse> GetAsync(HttpClient client, Guid battleId) =>
        await client.GetFromJsonAsync<BattleResponse>(
            new Uri($"/battles/{battleId}", UriKind.Relative), Cancellation)
        ?? throw new InvalidOperationException("The battle endpoint returned no battle.");

    private static async Task<BattleResponse> WaitForCompletionAsync(HttpClient client, Guid battleId)
    {
        // The deadline plus headroom for the reminder service to load
        // the stored reminder and fire it.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Cancellation);
        timeout.CancelAfter(Deadline + TimeSpan.FromSeconds(30));

        while (true)
        {
            var battle = await GetAsync(client, battleId);
            if (battle.Status == BattleStatus.Completed)
            {
                return battle;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), timeout.Token);
        }
    }

    private static async Task<BattleResponse> ReadBattleAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation)
        ?? throw new InvalidOperationException("The endpoint returned no battle.");
}
