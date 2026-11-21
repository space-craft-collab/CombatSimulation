using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Shared.Infrastructure.Persistence;

/// <summary>
/// Readiness check of one module database: healthy when SQL Server
/// accepts a connection for the module's context.
/// </summary>
/// <typeparam name="TContext">The module's context.</typeparam>
/// <param name="contexts">Creates the context to connect with.</param>
internal sealed class ModuleDatabaseHealthCheck<TContext>(IDbContextFactory<TContext> contexts) : IHealthCheck
    where TContext : ModuleDbContext
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await using var database = await contexts.CreateDbContextAsync(cancellationToken);

        return await database.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : new HealthCheckResult(context.Registration.FailureStatus, "SQL Server does not accept a connection.");
    }
}
