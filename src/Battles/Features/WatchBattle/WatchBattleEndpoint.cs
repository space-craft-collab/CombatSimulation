using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Battles.Features.WatchBattle;

/// <summary>
/// Maps the <see cref="BattleHub"/> that streams round deltas.
/// </summary>
internal static class WatchBattleEndpoint
{
    /// <summary>
    /// Maps the hub.
    /// </summary>
    public static IEndpointRouteBuilder MapWatchBattleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<BattleHub>(BattleHub.Route);

        return endpoints;
    }
}
