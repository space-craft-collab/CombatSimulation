using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Players.Contracts;
using Players.Features.GetPlayerStats;
using Players.Features.RecordResult;
using Players.Infrastructure;
using Shared.Infrastructure.Persistence;

namespace Players;

/// <summary>
/// Composition entry point of the Players module (ADR-0009) —
/// the only surface the AppHost calls.
/// </summary>
public static class PlayersModule
{
    // Groups the module's operations in the OpenAPI document.
    private const string ApiTag = "Players";

    /// <summary>
    /// Registers the Players module's services and its database in
    /// the <c>players</c> schema.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddPlayersModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddModuleDatabase<PlayersDbContext>(PlayersDbContext.SchemaName);
        services.AddSingleton<PlayerStatsReadRepository>();
        services.AddSingleton<PlayerStatsWriteRepository>();
        services.AddSingleton<IPlayerStatsService, PlayerStatsService>();

        return services;
    }

    /// <summary>
    /// Maps the Players module's Minimal API endpoints.
    /// </summary>
    /// <param name="endpoints">The route builder to map onto.</param>
    /// <returns>The same route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapPlayersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGroup(string.Empty)
            .WithTags(ApiTag)
            .MapGetPlayerStatsEndpoint();

        return endpoints;
    }
}
