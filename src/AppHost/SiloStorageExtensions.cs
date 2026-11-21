using Azure.Data.Tables;

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
    /// Registers the <c>Default</c> grain storage and the reminder
    /// service: Azure Table Storage when the
    /// <see cref="TableStorageConnectionName"/> connection string is
    /// set, in-memory otherwise — the default for tests and a bare
    /// <c>dotnet run</c>.
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

        return silo
            .AddAzureTableGrainStorage("Default", options =>
            {
                options.TableServiceClient = new TableServiceClient(connectionString);

                // ClearStateAsync deletes the row instead of blanking it:
                // a completed battle's hot rows must go once it is in the
                // cold store, since Table Storage has no TTL (ADR-0003).
                options.DeleteStateOnClear = true;
            })
            .UseAzureTableReminderService(options =>
                options.TableServiceClient = new TableServiceClient(connectionString));
    }
}
