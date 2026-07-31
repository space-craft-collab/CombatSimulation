using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    /// HTTP-client and runtime instrumentation, exported via OTLP.
    /// In the Development environment a console exporter is added
    /// as well, so telemetry is visible without a collector.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="environment">The host environment; decides whether the console exporter is added.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddArenaTelemetry(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);

        var useConsoleExporter = environment.IsDevelopment();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter();

                if (useConsoleExporter)
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter();

                if (useConsoleExporter)
                {
                    metrics.AddConsoleExporter();
                }
            });

        return services;
    }
}
