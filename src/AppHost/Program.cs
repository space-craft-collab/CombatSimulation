using Battles;
using Catalog;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Players;
using Shared.Infrastructure.Logging;
using Shared.Infrastructure.Persistence;
using Shared.Infrastructure.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddArenaLogging();
builder.Services.AddArenaTelemetry(builder.Environment);

// The silo shares this process with the web host (ADR-0002).
// Clustering stays on localhost until Phase 5; grain storage and
// reminders follow the configuration (in-memory or Table Storage).
builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering();
    silo.AddArenaStorage(builder.Configuration);
});

builder.Services
    .AddCatalogModule()
    .AddBattlesModule()
    .AddPlayersModule();

// The checks themselves come with what they watch: one per module
// database, and Table Storage when the silo persists to it.
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapCatalogEndpoints();
app.MapBattlesEndpoints();
app.MapPlayersEndpoints();

// Liveness: the process answers. No check runs, so an outage of
// SQL Server or Table Storage never gets the host restarted.
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });

// Readiness: every registered check has to pass.
app.MapHealthChecks("/health/ready");

// Development (and the tests) bring every module schema up to date
// before the silo serves the first grain; deployments apply a
// migration bundle instead.
if (app.Environment.IsDevelopment())
{
    await app.Services.MigrateModuleDatabasesAsync();
}

await app.RunAsync();

/// <summary>
/// Partial marker that makes the implicit entry-point class
/// visible to <c>WebApplicationFactory</c>-based tests.
/// </summary>
public partial class Program;
