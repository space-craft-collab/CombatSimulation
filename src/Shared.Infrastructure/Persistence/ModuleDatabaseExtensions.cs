using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shared.Infrastructure.Persistence;

/// <summary>
/// Registers and migrates the per-module cold-path databases
/// (Phase 3). All modules share one SQL Server database, each in
/// its own schema with its own migrations history table.
/// </summary>
public static class ModuleDatabaseExtensions
{
    /// <summary>
    /// The connection string name of the cold-path SQL Server.
    /// </summary>
    public const string ConnectionName = "Sql";

    /// <summary>
    /// Registers an <see cref="IDbContextFactory{TContext}"/> for a
    /// module context on SQL Server, and its migration for
    /// <see cref="MigrateModuleDatabasesAsync"/>. Callers create a
    /// short-lived context per unit of work, so the factory is safe
    /// to use from singletons and grains.
    /// </summary>
    /// <typeparam name="TContext">The module's context.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="schema">The module's SQL schema.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddModuleDatabase<TContext>(this IServiceCollection services, string schema)
        where TContext : ModuleDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        services.AddDbContextFactory<TContext>((provider, options) =>
            options.UseModuleSqlServer(GetConnectionString(provider), schema));

        services.AddSingleton(new ModuleMigration(typeof(TContext).Name, async (provider, cancellationToken) =>
        {
            var factory = provider.GetRequiredService<IDbContextFactory<TContext>>();
            await using var context = await factory.CreateDbContextAsync(cancellationToken);
            await context.Database.MigrateAsync(cancellationToken);
        }));

        return services;
    }

    /// <summary>
    /// Configures SQL Server with the migrations history table kept
    /// in the module's schema, so modules migrate independently.
    /// Shared by the runtime registration and the design-time
    /// factories.
    /// </summary>
    /// <param name="options">The options being built.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <param name="schema">The module's SQL schema.</param>
    /// <returns>The same options builder for chaining.</returns>
    public static DbContextOptionsBuilder UseModuleSqlServer(
        this DbContextOptionsBuilder options,
        string connectionString,
        string schema)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.UseSqlServer(connectionString, sql =>
            sql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, schema));
    }

    /// <summary>
    /// Applies the pending migrations of every registered module
    /// database, one after another. The database is created on the
    /// first run.
    /// </summary>
    /// <param name="services">The root service provider.</param>
    /// <param name="cancellationToken">Cancels the migration.</param>
    /// <returns>A task that completes once every module is up to date.</returns>
    public static async Task MigrateModuleDatabasesAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(ModuleDatabaseExtensions));

        foreach (var migration in services.GetServices<ModuleMigration>())
        {
            Log.Migrating(logger, migration.Name);
            await migration.ApplyAsync(services, cancellationToken);
        }
    }

    private static string GetConnectionString(IServiceProvider provider)
    {
        var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionName);

        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException(
                $"The connection string '{ConnectionName}' is not set. The cold path needs SQL Server: "
                + "start it with `docker compose up -d` and copy appsettings.Development.example.json.")
            : connectionString;
    }
}
