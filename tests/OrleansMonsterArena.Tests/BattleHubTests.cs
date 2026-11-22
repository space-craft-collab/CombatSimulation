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

    // Two sturdy monsters: one action per side fells at most one
    // monster each, so the first round cannot end the battle, and a
    // single strike is unlikely to fell the monster the bot acts with.
    private static readonly string[] GolemPair = ["moss-golem", "moss-golem"];

    // Three monsters against one: the bigger side wins, whatever the
    // numbers.
    private static readonly string[] ThreeMonsters = ["moss-golem", "lava-crab", "ember-fox"];

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Watch_RunningBattle_ReturnsCurrentState()
    {
        // Arrange
        await using var connection = await ConnectAsync();
        using var client = host.Factory.CreateClient();
        var battle = await new HttpBattleDriver(client).StartAgainstBotAsync("player-1", CrabTeam);

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
    public async Task RoundResolved_HumanStrikes_SendsDeltaWithBothSidesActions()
    {
        // Arrange: the player holds slots 0-1, the bot 2-3.
        await using var connection = await ConnectAsync();
        var deltas = Collect(connection);
        using var client = host.Factory.CreateClient();
        var driver = new HttpBattleDriver(client);
        var battle = await driver.StartAgainstBotAsync("player-1", GolemPair);
        await connection.InvokeAsync<BattleResponse>("Watch", battle.BattleId, Cancellation);
        var botSlots = battle.Combatants.Where(combatant => combatant.IsBot).Select(combatant => combatant.Slot).ToList();
        var target = battle.Combatants[2];

        // Act
        await driver.StrikeAsync(battle.BattleId, "player-1");

        // Assert
        var delta = await deltas.ReadAsync(Cancellation).AsTask().WaitAsync(WaitLimit, Cancellation);
        Assert.Equal(battle.BattleId, delta.BattleId);
        Assert.Equal(1, delta.Round);
        Assert.Equal(BattleStatus.InProgress, delta.Status);
        Assert.NotNull(delta.TurnDeadline);

        // Assert: the human's strike, from its first monster at the
        // bot's first, which was at full health.
        var strike = Assert.Single(delta.Strikes, entry => entry.ActorSlot == 0);
        Assert.Equal(target.Slot, strike.TargetSlot);
        Assert.Equal("strike", strike.AbilityId);
        Assert.True(strike.Damage > 0, $"Expected the strike to deal damage: {strike}.");
        Assert.Equal(target.MaxHitPoints - strike.Damage, strike.TargetHitPoints);

        // Assert: the bot acted in the same round.
        Assert.Contains(delta.Strikes, entry => botSlots.Contains(entry.ActorSlot));
    }

    [Fact]
    public async Task RoundResolved_NobodyActsBeforeDeadline_SendsAbandonedDelta()
    {
        // Arrange
        await using var connection = await ConnectAsync();
        var deltas = Collect(connection);
        using var client = host.Factory.CreateClient();
        var battle = await new HttpBattleDriver(client).StartAgainstBotAsync("player-1", CrabTeam);
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
        // Arrange: two humans, three monsters against one, so player-1
        // wins; both act every round.
        await using var connection = await ConnectAsync();
        var deltas = Collect(connection);
        using var client = host.Factory.CreateClient();
        var driver = new HttpBattleDriver(client);
        var opened = await driver.OpenAsync("player-1", ThreeMonsters);
        await connection.InvokeAsync<BattleResponse>("Watch", opened.BattleId, Cancellation);
        await driver.JoinAsync(opened.BattleId, "player-2", "frost-owl");

        // Act
        var final = await driver.PlayToTheEndAsync(opened.BattleId);
        var received = new List<BattleEventDto>();
        do
        {
            received.Add(await deltas.ReadAsync(Cancellation).AsTask().WaitAsync(WaitLimit, Cancellation));
        }
        while (received[^1].Status != BattleStatus.Completed);

        // Assert: one delta per round, the last one still carrying the
        // result, although the grain clears its state right after the
        // hand-over.
        Assert.Equal(Enumerable.Range(1, final.Round), received.Select(delta => delta.Round));
        Assert.All(received.SkipLast(1), delta => Assert.Equal(BattleStatus.InProgress, delta.Status));
        var last = received[^1];
        Assert.Equal(BattleStatus.Completed, last.Status);
        Assert.Equal("player-1", last.WinnerId);
        Assert.Null(last.TurnDeadline);
        Assert.Contains(last.Strikes, entry => entry.TargetSlot == 3 && entry.TargetHitPoints == 0);
    }

    [Fact]
    public async Task Unwatch_AfterWatch_StopsRoundResolvedDeltas()
    {
        // Arrange: two watchers; the first one leaves again.
        await using var leaving = await ConnectAsync();
        await using var staying = await ConnectAsync();
        var missed = Collect(leaving);
        var received = Collect(staying);
        using var client = host.Factory.CreateClient();
        var driver = new HttpBattleDriver(client);
        var battle = await driver.StartAgainstBotAsync("player-1", CrabTeam);
        await leaving.InvokeAsync<BattleResponse>("Watch", battle.BattleId, Cancellation);
        await staying.InvokeAsync<BattleResponse>("Watch", battle.BattleId, Cancellation);

        // Act
        await leaving.InvokeAsync("Unwatch", battle.BattleId, Cancellation);
        await driver.StrikeAsync(battle.BattleId, "player-1");

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
        var battle = await new HttpBattleDriver(client).OpenAsync("player-1", CrabTeam);
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
}
