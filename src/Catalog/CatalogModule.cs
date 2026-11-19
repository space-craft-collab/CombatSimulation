using Catalog.Contracts;
using Catalog.Features.QuerySpecies;
using Catalog.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Persistence;

namespace Catalog;

/// <summary>
/// Composition entry point of the Catalog module (ADR-0009) —
/// the only surface the AppHost calls.
/// </summary>
public static class CatalogModule
{
    /// <summary>
    /// Registers the Catalog module's services and its database in
    /// the <c>catalog</c> schema.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddModuleDatabase<CatalogDbContext>(CatalogDbContext.SchemaName);
        services.AddSingleton<SpeciesReadRepository>();
        services.AddSingleton<ICatalogQueryService, CatalogQueryService>();

        return services;
    }

    /// <summary>
    /// Maps the Catalog module's Minimal API endpoints.
    /// </summary>
    /// <param name="endpoints">The route builder to map onto.</param>
    /// <returns>The same route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints;
    }
}
