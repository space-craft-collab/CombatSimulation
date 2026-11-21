using Azure.Data.Tables;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Readiness check of the hot path: healthy when Table Storage
/// answers a query. A failed or timed-out query surfaces as an
/// exception, which the health check service reports as unhealthy.
/// </summary>
/// <param name="tables">The Table Storage account the silo persists to.</param>
public sealed class TableStorageHealthCheck(TableServiceClient tables) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // The cheapest call the service has to answer: the first
        // page of the table list, one entry long.
        await foreach (var _ in tables.QueryAsync(maxPerPage: 1, cancellationToken: cancellationToken).AsPages())
        {
            break;
        }

        return HealthCheckResult.Healthy();
    }
}
