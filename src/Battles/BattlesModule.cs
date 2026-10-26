using Battles.Features.GetBattle;
using Battles.Features.StartBattle;
using Battles.Features.SubmitAction;
using Battles.Grains;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Battles;

/// <summary>
/// Composition entry point of the Battles module (ADR-0009) —
/// the only surface the AppHost calls.
/// </summary>
public static class BattlesModule
{
    /// <summary>
    /// Registers the Battles module's services.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddBattlesModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<BattleTimingOptions>().BindConfiguration(BattleTimingOptions.SectionName);
        services.TryAddSingleton(TimeProvider.System);

        // The slices reach grains through the co-hosted silo's
        // IGrainFactory; no Orleans type leaves this module (ADR-0005).
        services.AddSingleton<StartBattleService>();
        services.AddSingleton<SubmitActionService>();
        services.AddSingleton<GetBattleService>();

        return services;
    }

    /// <summary>
    /// Maps the Battles module's Minimal API endpoints.
    /// </summary>
    /// <param name="endpoints">The route builder to map onto.</param>
    /// <returns>The same route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapBattlesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints
            .MapStartBattleEndpoint()
            .MapSubmitActionEndpoint()
            .MapGetBattleEndpoint();
    }
}
