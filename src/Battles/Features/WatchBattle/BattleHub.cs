using Battles.Features.GetBattle;
using Microsoft.AspNetCore.SignalR;

namespace Battles.Features.WatchBattle;

/// <summary>
/// SignalR hub at <c>/hubs/battles</c>: a client watches a battle and
/// receives a <see cref="BattleEventDto"/> on
/// <see cref="RoundResolvedMethod"/> after every round. The host of an
/// open battle also learns on <see cref="BattleStartedMethod"/> when an
/// opponent joined.
/// </summary>
/// <param name="battles">Reads the battle's current state.</param>
internal sealed class BattleHub(GetBattleService battles) : Hub
{
    /// <summary>
    /// The route the hub is mapped to.
    /// </summary>
    public const string Route = "/hubs/battles";

    /// <summary>
    /// The client method that receives the round deltas.
    /// </summary>
    public const string RoundResolvedMethod = "RoundResolved";

    /// <summary>
    /// The client method that receives the battle's first state once
    /// an opponent joined an open battle.
    /// </summary>
    public const string BattleStartedMethod = "BattleStarted";

    /// <summary>
    /// The SignalR group of a battle's watchers.
    /// </summary>
    public static string GroupName(Guid battleId) => $"battle-{battleId}";

    /// <summary>
    /// Starts sending the battle's round deltas to the caller. The
    /// caller joins the group before the state is read, so no round
    /// falls between the snapshot and the first delta; a delta whose
    /// round the snapshot already shows can be ignored.
    /// </summary>
    /// <param name="battleId">The battle to watch.</param>
    /// <returns>The battle's state at the time of joining.</returns>
    /// <exception cref="HubException">The battle was never started.</exception>
    [HubMethodName("Watch")]
    public async Task<BattleResponse> WatchAsync(Guid battleId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(battleId), Context.ConnectionAborted);

        if (await battles.FindAsync(battleId) is { } battle)
        {
            return battle;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(battleId), Context.ConnectionAborted);
        throw new HubException($"Battle {battleId} not found.");
    }

    /// <summary>
    /// Stops sending the battle's round deltas to the caller.
    /// </summary>
    /// <param name="battleId">The battle to stop watching.</param>
    /// <returns>A task that completes once the caller left the group.</returns>
    [HubMethodName("Unwatch")]
    public Task UnwatchAsync(Guid battleId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(battleId), Context.ConnectionAborted);
}
