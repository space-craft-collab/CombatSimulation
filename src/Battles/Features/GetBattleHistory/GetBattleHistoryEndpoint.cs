using Battles.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Battles.Features.GetBattleHistory;

/// <summary>
/// <c>GET /players/{playerId}/battles</c>: the completed battles a
/// player fought in, newest first, read from the cold store
/// (ADR-0003). The route sits under <c>/players</c>, but the data
/// belongs to Battles, so the slice lives here. A player without a
/// completed battle gets an empty list; live battles are not in it.
/// </summary>
internal static class GetBattleHistoryEndpoint
{
    /// <summary>
    /// The most battles one answer returns.
    /// </summary>
    public const int Limit = 50;

    /// <summary>
    /// Maps the endpoint.
    /// </summary>
    public static IEndpointRouteBuilder MapGetBattleHistoryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/players/{playerId}/battles", HandleAsync);

        return endpoints;
    }

    private static async Task<Ok<IReadOnlyList<BattleHistoryEntryResponse>>> HandleAsync(
        string playerId,
        BattleSnapshotReadRepository snapshots,
        CancellationToken cancellationToken)
    {
        var battles = await snapshots.ListForParticipantAsync(playerId, Limit, cancellationToken);

        return TypedResults.Ok<IReadOnlyList<BattleHistoryEntryResponse>>([.. battles.Select(battle =>
            new BattleHistoryEntryResponse(
                battle.BattleId,
                battle.ArenaId,
                battle.CompletedAt,
                battle.Round,
                battle.WinnerId,
                [.. battle.Combatants
                    .OrderBy(combatant => combatant.Slot)
                    .Select(combatant => combatant.ParticipantId)
                    .Distinct()]))]);
    }
}
