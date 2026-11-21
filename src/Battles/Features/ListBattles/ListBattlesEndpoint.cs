using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Battles.Features.ListBattles;

/// <summary>
/// <c>GET /arenas/{arenaId}/battles</c>: the arena's open and running
/// battles, so a visitor can pick one that waits for players; 404 if
/// the arena is not in the Catalog.
/// </summary>
internal static class ListBattlesEndpoint
{
    /// <summary>
    /// Maps the endpoint.
    /// </summary>
    public static IEndpointRouteBuilder MapListBattlesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/arenas/{arenaId}/battles", HandleAsync);

        return endpoints;
    }

    private static async Task<Results<Ok<IReadOnlyList<BattleSummaryResponse>>, NotFound>> HandleAsync(
        string arenaId,
        ListBattlesService service) =>
        await service.ListAsync(arenaId) is { } battles
            ? TypedResults.Ok(battles)
            : TypedResults.NotFound();
}
