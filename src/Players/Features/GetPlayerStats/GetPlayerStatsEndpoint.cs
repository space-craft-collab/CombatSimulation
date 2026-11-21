using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Players.Infrastructure;

namespace Players.Features.GetPlayerStats;

/// <summary>
/// <c>GET /players/{playerId}</c>: a player's wins and losses. A
/// player without a decided battle is unknown to the store, so the
/// answer is 404 rather than an all-zero record.
/// </summary>
internal static class GetPlayerStatsEndpoint
{
    /// <summary>
    /// Maps the endpoint.
    /// </summary>
    public static IEndpointRouteBuilder MapGetPlayerStatsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/players/{playerId}", HandleAsync);

        return endpoints;
    }

    private static async Task<Results<Ok<PlayerStatsResponse>, NotFound>> HandleAsync(
        string playerId,
        PlayerStatsReadRepository stats,
        CancellationToken cancellationToken) =>
        await stats.FindAsync(playerId, cancellationToken) is { } found
            ? TypedResults.Ok(new PlayerStatsResponse(found.PlayerId, found.Wins, found.Losses))
            : TypedResults.NotFound();
}
