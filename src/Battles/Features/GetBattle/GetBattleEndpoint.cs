using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Battles.Features.GetBattle;

/// <summary>
/// <c>GET /battles/{battleId}</c>: the battle's current state. Live
/// clients watch the SignalR hub instead of polling.
/// </summary>
internal static class GetBattleEndpoint
{
    /// <summary>
    /// Maps the endpoint.
    /// </summary>
    public static IEndpointRouteBuilder MapGetBattleEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/battles/{battleId:guid}", HandleAsync);

        return endpoints;
    }

    private static async Task<Results<Ok<BattleResponse>, NotFound>> HandleAsync(
        Guid battleId,
        GetBattleService service) =>
        await service.FindAsync(battleId) is { } battle
            ? TypedResults.Ok(battle)
            : TypedResults.NotFound();
}
