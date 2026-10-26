using Battles;
using Catalog;
using Players;
using Shared.Infrastructure.Logging;
using Shared.Infrastructure.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddArenaLogging();
builder.Services.AddArenaTelemetry(builder.Environment);

// The silo shares this process with the web host (ADR-0002).
// Localhost clustering, in-memory grain storage and in-memory
// reminders are the Phase 2 shape; Azure Table Storage replaces
// them in Phase 3/5.
builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering();
    silo.AddMemoryGrainStorage("Default");
    silo.UseInMemoryReminderService();
});

builder.Services
    .AddCatalogModule()
    .AddBattlesModule()
    .AddPlayersModule();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapCatalogEndpoints();
app.MapBattlesEndpoints();
app.MapPlayersEndpoints();

app.MapHealthChecks("/health");

app.Run();

/// <summary>
/// Partial marker that makes the implicit entry-point class
/// visible to <c>WebApplicationFactory</c>-based tests.
/// </summary>
public partial class Program;
