using Azure.Data.Tables;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Chooses the silo's grain storage and reminder service from
/// configuration (Phase 3).
/// </summary>
public static class SiloStorageExtensions
{
    /// <summary>
    /// The connection string name that switches the silo to Azure
    /// Table Storage (Azurite locally).
    /// </summary>
    public const string TableStorageConnectionName = "TableStorage";

    /// <summary>
    /// The name of the Table Storage health check.
    /// </summary>
    public const string HealthCheckName = "table-storage";

    // A readiness probe has to answer quickly; the storage client
    // would retry a failed call for much longer.
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Registers the <c>Default</c> grain storage and the reminder
    /// service: Azure Table Storage when the
    /// <see cref="TableStorageConnectionName"/> connection string is
    /// set, in-memory otherwise — the default for tests and a bare
    /// <c>dotnet run</c>. On Table Storage a health check named
    /// <see cref="HealthCheckName"/> is registered as well.
    /// </summary>
    /// <param name="silo">The silo being configured.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The same silo builder, for chaining.</returns>
    public static ISiloBuilder AddArenaStorage(this ISiloBuilder silo, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(silo);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(TableStorageConnectionName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return silo
                .AddMemoryGrainStorage("Default")
                .UseInMemoryReminderService();
        }

        // One client for everything: it is thread-safe and holds no
        // state beyond the account it talks to.
        var tables = new TableServiceClient(connectionString);

        silo.Services.AddHealthChecks()
            .AddCheck(HealthCheckName, new TableStorageHealthCheck(tables), timeout: HealthCheckTimeout);

        return silo
            .AddAzureTableGrainStorage("Default", options =>
            {
                options.TableServiceClient = tables;

                // ClearStateAsync deletes the row instead of blanking it:
                // a completed battle's hot rows must go once it is in the
                // cold store, since Table Storage has no TTL (ADR-0003).
                options.DeleteStateOnClear = true;
            })
            .UseAzureTableReminderService(options => options.TableServiceClient = tables);
    }
}
