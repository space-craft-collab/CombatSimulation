using Battles.Features.GetBattle;
using Microsoft.AspNetCore.SignalR;

namespace Battles.Features.WatchBattle;

/// <summary>
/// Pushes round deltas from the battle grain to the battle's
/// watchers. The silo shares the web host's process (ADR-0002), so
/// the grain reaches the hub context directly; several silos will
/// need a SignalR backplane (Phase 5).
/// </summary>
/// <param name="hub">The hub's connections and groups.</param>
internal sealed class BattleEventPublisher(IHubContext<BattleHub> hub)
{
    /// <summary>
    /// Sends the delta to everyone watching its battle.
    /// </summary>
    /// <param name="battleEvent">The round delta.</param>
    /// <returns>A task that completes once the delta is queued for every watcher.</returns>
    public Task PublishAsync(BattleEventDto battleEvent)
    {
        ArgumentNullException.ThrowIfNull(battleEvent);

        return hub.Clients
            .Group(BattleHub.GroupName(battleEvent.BattleId))
            .SendAsync(BattleHub.RoundResolvedMethod, battleEvent);
    }

    /// <summary>
    /// Tells everyone watching an open battle that an opponent joined
    /// and round 1 began.
    /// </summary>
    /// <param name="battle">The battle's first state.</param>
    /// <returns>A task that completes once the state is queued for every watcher.</returns>
    public Task PublishStartedAsync(BattleResponse battle)
    {
        ArgumentNullException.ThrowIfNull(battle);

        return hub.Clients
            .Group(BattleHub.GroupName(battle.BattleId))
            .SendAsync(BattleHub.BattleStartedMethod, battle);
    }
}
