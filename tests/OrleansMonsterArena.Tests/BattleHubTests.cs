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

    private async Task<BattleResponse> StartAsync(string speciesId)
    {
        using var client = host.Factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            new Uri($"/arenas/test-{Guid.NewGuid()}/battles", UriKind.Relative),
            new { playerId = "player-1", monsterSpeciesIds = new[] { speciesId } },
            Cancellation);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation)
            ?? throw new InvalidOperationException("The start endpoint returned no battle.");
    }
}
