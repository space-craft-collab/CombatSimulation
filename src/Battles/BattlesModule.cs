using Battles.Features.GetBattle;
using Battles.Features.GetBattleHistory;
using Battles.Features.JoinBattle;
using Battles.Features.ListBattles;
using Battles.Features.StartBattle;
using Battles.Features.SubmitAction;
using Battles.Features.WatchBattle;
using Battles.Grains;
using Battles.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Infrastructure.Persistence;

namespace Battles;

/// <summary>
/// Composition entry point of the Battles module (ADR-0009) —
/// the only surface the AppHost calls.
/// </summary>
public static class BattlesModule
{
    /// <summary>
    /// Registers the Battles module's services and its cold store of
    /// completed battles in the <c>battles</c> schema.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddBattlesModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<BattleTimingOptions>().BindConfiguration(BattleTimingOptions.SectionName);
        services.TryAddSingleton(TimeProvider.System);

        // Completed battles leave the grains for SQL (ADR-0003).
        services.AddModuleDatabase<BattlesDbContext>(BattlesDbContext.SchemaName);
        services.AddSingleton<BattleSnapshotReadRepository>();
        services.AddSingleton<BattleSnapshotWriteRepository>();

        // The slices reach grains through the co-hosted silo's
        // IGrainFactory; no Orleans type leaves this module (ADR-0005).
        services.AddSingleton<StartBattleService>();
        services.AddSingleton<SubmitActionService>();
        services.AddSingleton<GetBattleService>();
        services.AddSingleton<JoinBattleService>();
        services.AddSingleton<ListBattlesService>();

        // Round deltas leave through SignalR (ADR-0007); the grain
        // publishes into the hub of the same process (ADR-0002).
        services.AddSignalR();
        services.AddSingleton<BattleEventPublisher>();

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
            .MapGetBattleEndpoint()
            .MapJoinBattleEndpoint()
            .MapListBattlesEndpoint()
            .MapGetBattleHistoryEndpoint()
            .MapWatchBattleEndpoint();
    }
}
