using Catalog.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Features.ListArenas;

/// <summary>
/// <c>GET /arenas</c>: every arena a visitor can start or open a
/// battle on.
/// </summary>
internal static class ListArenasEndpoint
{
    /// <summary>
    /// Maps the endpoint.
    /// </summary>
    public static IEndpointRouteBuilder MapListArenasEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/arenas", HandleAsync);

        return endpoints;
    }

    private static async Task<Ok<IReadOnlyList<ArenaResponse>>> HandleAsync(
        ArenaReadRepository arenas,
        CancellationToken cancellationToken) =>
        TypedResults.Ok<IReadOnlyList<ArenaResponse>>(
            [.. (await arenas.ListAsync(cancellationToken)).Select(arena => new ArenaResponse(arena.Id, arena.Name))]);
}
