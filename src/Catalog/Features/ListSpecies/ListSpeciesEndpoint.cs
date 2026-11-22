using Catalog.Domain;
using Catalog.Features.QueryCatalog;
using Catalog.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Features.ListSpecies;

/// <summary>
/// <c>GET /species</c>: every species a visitor can field, with
/// the base stats and abilities a team is picked by.
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
            [.. (await species.ListAsync(cancellationToken)).Select(ToResponse)]);

    private static SpeciesResponse ToResponse(SpeciesProfile profile)
    {
        var dto = SpeciesDtoMapper.ToDto(profile);

        return new SpeciesResponse(
            dto.Id,
            dto.Name,
            dto.Element,
            dto.MaxHitPoints,
            dto.Attack,
            dto.Defense,
            dto.Speed,
            [.. dto.Abilities.Select(ability => new AbilityResponse(
                ability.Id,
                ability.Name,
                ability.Element,
                ability.Power,
                ability.Cooldown,
                ability.Effect,
                ability.EffectValue,
                ability.EffectRounds,
                ability.TargetsSelf))]);
    }
}
