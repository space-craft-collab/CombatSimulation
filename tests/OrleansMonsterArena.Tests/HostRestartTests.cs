using System.Net.Http.Json;
using Battles.Features.GetBattle;
using Battles.Grains.Abstractions;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Proves that a live battle survives a host restart on Table
/// Storage: its state, its round and its pending turn deadline.
/// </summary>
/// <param name="azurite">The Table Storage both hosts share.</param>
[Collection(ContainersCollectionDefinition.Name)]
[Trait("Category", TestCategories.Containers)]
public sealed class HostRestartTests(AzuriteFixture azurite) : IClassFixture<AzuriteFixture>
{
    // Long enough that the deadline cannot pass while the first host
    // shuts down and the second one boots.
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RestartHost_BattleInProgress_KeepsStateAndFiresPendingDeadline()
    {
        // Arrange: play one round on the first host, then shut it down.
        BattleResponse beforeRestart;
        await using (var first = AppHostFixture.CreateFactory(azurite.ConnectionString, Deadline))
        {
            using var client = first.CreateClient();
            var battle = await StartAsync(client);
            beforeRestart = await StrikeAsync(client, battle.BattleId);
        }

        // Act
        await using var second = AppHostFixture.CreateFactory(azurite.ConnectionString, Deadline);
        using var restartedClient = second.CreateClient();
        var afterRestart = await GetAsync(restartedClient, beforeRestart.BattleId);

        // Assert: state and round come back from storage unchanged.
        Assert.Equal(BattleStatus.InProgress, afterRestart.Status);
        Assert.Equal(2, afterRestart.Round);
        Assert.Equal(beforeRestart.TurnDeadline, afterRestart.TurnDeadline);
        Assert.Equal(beforeRestart.Combatants, afterRestart.Combatants);

        // Assert: nobody acts in round 2, so the deadline reminder,
        // registered on the first host, abandons the battle on the
        // second one.
        var final = await WaitForCompletionAsync(restartedClient, afterRestart.BattleId);
        Assert.Null(final.WinnerId);
    }

    private static async Task<BattleResponse> StartAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri($"/arenas/test-{Guid.NewGuid()}/battles", UriKind.Relative),
            new { playerId = "player-1", monsterSpeciesIds = new[] { "lava-crab" } },
            Cancellation);
        response.EnsureSuccessStatusCode();

        return await ReadBattleAsync(response);
    }

    private static async Task<BattleResponse> StrikeAsync(HttpClient client, Guid battleId)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri($"/battles/{battleId}/actions", UriKind.Relative),
            new { participantId = "player-1", actorSlot = 0, targetSlot = 1 },
            Cancellation);
        response.EnsureSuccessStatusCode();

        return await ReadBattleAsync(response);
    }

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
