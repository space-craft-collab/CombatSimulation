using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared.Infrastructure.Telemetry;

/// <summary>
/// Registers OpenTelemetry tracing and metrics (ADR-0006, roadmap
/// Phase 1). OTLP export targets a future collector (Phase 7
/// Grafana stack); console export gives local visibility now.
/// </summary>
public static class TelemetryExtensions
{
    private const string ServiceName = "OrleansMonsterArena";

    /// <summary>
    /// Adds OpenTelemetry tracing and metrics with ASP.NET Core,
    /// HTTP-client and runtime instrumentation, exported via OTLP
    /// and to the console.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddArenaTelemetry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter());

        return services;
    }
}
