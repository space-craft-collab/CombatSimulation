using System.Net.Http.Json;
using System.Threading.Channels;
using Battles.Features.GetBattle;
using Battles.Features.WatchBattle;
using Battles.Grains.Abstractions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Watches battles over the SignalR hub and checks the round deltas
/// the battle grain pushes (ADR-0007).
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class BattleHubTests(AppHostFixture host)
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    private static readonly string[] CrabTeam = ["lava-crab"];

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Watch_RunningBattle_ReturnsCurrentState()
    {
        // Arrange
        await using var connection = await ConnectAsync();
        var battle = await StartAsync("lava-crab");

        // Act
        var watched = await connection.InvokeAsync<BattleResponse>("Watch", battle.BattleId, Cancellation);

        // Assert
        Assert.Equal(battle.BattleId, watched.BattleId);
        Assert.Equal(1, watched.Round);
        Assert.Equal(BattleStatus.InProgress, watched.Status);
    }

    [Fact]
    public async Task Watch_UnknownBattle_Throws()
    {
        // Arrange
        await using var connection = await ConnectAsync();

        // Act & Assert
        await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync<BattleResponse>("Watch", Guid.NewGuid(), Cancellation));
    }

    [Fact]
    public async Task RoundResolved_HumanStrikes_SendsDeltaWithBothStrikes()
    {
        // Arrange
        await using var connection = await ConnectAsync();
        var deltas = Collect(connection);
        var battle = await StartAsync("lava-crab");
        await connection.InvokeAsync<BattleResponse>("Watch", battle.BattleId, Cancellation);

        // Act
        using var client = host.Factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            new Uri($"/battles/{battle.BattleId}/actions", UriKind.Relative),
            new { participantId = "player-1", actorSlot = 0, targetSlot = 1 },
            Cancellation);
        response.EnsureSuccessStatusCode();

        // Assert: mirrored crabs, equal speed, the lower slot strikes first.
        var delta = await deltas.ReadAsync(Cancellation).AsTask().WaitAsync(WaitLimit, Cancellation);
        Assert.Equal(battle.BattleId, delta.BattleId);
        Assert.Equal(1, delta.Round);
        Assert.Equal(BattleStatus.InProgress, delta.Status);
        Assert.NotNull(delta.TurnDeadline);
        Assert.Collection(
            delta.Strikes,
            first => Assert.Equal(new StrikeDto(0, 1, "strike", 2, 58), first),
            second => Assert.Equal(new StrikeDto(1, 0, "strike", 2, 58), second));
    }

    [Fact]
    public async Task RoundResolved_NobodyActsBeforeDeadline_SendsAbandonedDelta()
    {
        // Arrange
        await using var connection = await ConnectAsync();
        var deltas = Collect(connection);
        var battle = await StartAsync("lava-crab");
        await connection.InvokeAsync<BattleResponse>("Watch", battle.BattleId, Cancellation);

        // Act
        var delta = await deltas.ReadAsync(Cancellation).AsTask().WaitAsync(WaitLimit, Cancellation);

        // Assert
        Assert.Equal(1, delta.Round);
        Assert.Equal(BattleStatus.Completed, delta.Status);
        Assert.Null(delta.WinnerId);
        Assert.Null(delta.TurnDeadline);
        Assert.Empty(delta.Strikes);
    }

    [Fact]
    public async Task RoundResolved_FinalRound_SendsCompletedDeltaWithWinner()
    {
        // Arrange: mirrored foxes, 40 HP each and 10 per hit; the
        // player wins the speed tie on the lower slot, in round 4.
        await using var connection = await ConnectAsync();
        var deltas = Collect(connection);
        var battle = await StartAsync("ember-fox");
        await connection.InvokeAsync<BattleResponse>("Watch", battle.BattleId, Cancellation);

        // Act
        BattleEventDto? last = null;
        for (var round = 1; round <= 4; round++)
        {
            await StrikeAsync(battle.BattleId);
            last = await deltas.ReadAsync(Cancellation).AsTask().WaitAsync(WaitLimit, Cancellation);
        }

        // Assert: the last delta still carries the result, although
        // the grain clears its state right after the hand-over.
        Assert.NotNull(last);
        Assert.Equal(4, last.Round);
        Assert.Equal(BattleStatus.Completed, last.Status);
        Assert.Equal("player-1", last.WinnerId);
        Assert.Null(last.TurnDeadline);
        Assert.Equal(new StrikeDto(0, 1, "strike", 10, 0), Assert.Single(last.Strikes));
    }

    [Fact]
    public async Task Unwatch_AfterWatch_StopsRoundResolvedDeltas()
    {
        // Arrange: two watchers; the first one leaves again.
        await using var leaving = await ConnectAsync();
        await using var staying = await ConnectAsync();
        var missed = Collect(leaving);
        var received = Collect(staying);
        var battle = await StartAsync("lava-crab");
        await leaving.InvokeAsync<BattleResponse>("Watch", battle.BattleId, Cancellation);
        await staying.InvokeAsync<BattleResponse>("Watch", battle.BattleId, Cancellation);

        // Act
        await leaving.InvokeAsync("Unwatch", battle.BattleId, Cancellation);
        await StrikeAsync(battle.BattleId);

        // Assert: once the remaining watcher has the delta, the one
        // that left would have had it too.
        await received.ReadAsync(Cancellation).AsTask().WaitAsync(WaitLimit, Cancellation);
        await Task.Delay(TimeSpan.FromMilliseconds(300), Cancellation);
        Assert.False(missed.TryRead(out _));
    }

    [Fact]
    public async Task BattleStarted_OpponentJoinsOpenBattle_SendsFirstState()
    {
        // Arrange: the host opens a battle and watches it.
        await using var connection = await ConnectAsync();
        var started = new TaskCompletionSource<BattleResponse>();
        connection.On<BattleResponse>(BattleHub.BattleStartedMethod, battle => started.TrySetResult(battle));
        using var client = host.Factory.CreateClient();
        using var opened = await client.PostAsJsonAsync(
            TestArenas.VolcanoBattles,
            new { playerId = "player-1", monsterSpeciesIds = CrabTeam, opponent = "Human" },
            Cancellation);
        var battle = await opened.Content.ReadFromJsonAsync<BattleResponse>(Cancellation);
        Assert.NotNull(battle);
        var watched = await connection.InvokeAsync<BattleResponse>("Watch", battle.BattleId, Cancellation);
        Assert.Equal(BattleStatus.WaitingForPlayers, watched.Status);

        // Act
        using var joined = await client.PostAsync(
            new Uri($"/battles/{battle.BattleId}/bot", UriKind.Relative), null, Cancellation);
        joined.EnsureSuccessStatusCode();

        // Assert
        var first = await started.Task.WaitAsync(WaitLimit, Cancellation);
        Assert.Equal(battle.BattleId, first.BattleId);
        Assert.Equal(BattleStatus.InProgress, first.Status);
        Assert.Equal(2, first.Combatants.Count);
    }

    private static ChannelReader<BattleEventDto> Collect(HubConnection connection)
    {
        var channel = Channel.CreateUnbounded<BattleEventDto>();
        connection.On<BattleEventDto>(BattleHub.RoundResolvedMethod, delta => channel.Writer.TryWrite(delta));

        return channel.Reader;
    }

    private async Task<HubConnection> ConnectAsync()
    {
        var server = host.Factory.Server;
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, BattleHub.Route), options =>
            {
                // The in-memory test server speaks plain HTTP only.
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .Build();
        await connection.StartAsync(Cancellation);

        return connection;
    }

    private async Task StrikeAsync(Guid battleId)
    {
        using var client = host.Factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            new Uri($"/battles/{battleId}/actions", UriKind.Relative),
            new { participantId = "player-1", actorSlot = 0, targetSlot = 1 },
            Cancellation);
        response.EnsureSuccessStatusCode();
    }

    private async Task<BattleResponse> StartAsync(string speciesId)
    {
        using var client = host.Factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            TestArenas.VolcanoBattles,
            new { playerId = "player-1", monsterSpeciesIds = new[] { speciesId } },
            Cancellation);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation)
            ?? throw new InvalidOperationException("The start endpoint returned no battle.");
    }
}
