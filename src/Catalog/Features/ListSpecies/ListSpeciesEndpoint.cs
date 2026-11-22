using Catalog.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Features.ListSpecies;

/// <summary>
/// <c>GET /species</c>: every species a visitor can field, with
/// the base stats a team is picked by.
/// </summary>
internal static class ListSpeciesEndpoint
{
    /// <summary>
    /// Maps the endpoint.
    /// </summary>
    public static IEndpointRouteBuilder MapListSpeciesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/species", HandleAsync);

        return endpoints;
    }

    private static async Task<Ok<IReadOnlyList<SpeciesResponse>>> HandleAsync(
        SpeciesReadRepository species,
        CancellationToken cancellationToken) =>
        TypedResults.Ok<IReadOnlyList<SpeciesResponse>>(
            [.. (await species.ListAsync(cancellationToken)).Select(found => new SpeciesResponse(
                found.Id,
                found.Name,
                found.Element,
                found.MaxHitPoints,
                found.Attack,
                found.Defense,
                found.Speed))]);
}
