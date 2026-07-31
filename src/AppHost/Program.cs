using Battles;
using Catalog;
using Players;
using Shared.Infrastructure.Logging;
using Shared.Infrastructure.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddArenaLogging();
builder.Services.AddArenaTelemetry(builder.Environment);

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
