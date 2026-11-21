using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// The probes of a host on Table Storage: ready while Azurite
/// answers, not ready — but still alive — once it is gone.
/// </summary>
/// <param name="azurite">The Table Storage the hot state lives in.</param>
/// <param name="sql">The SQL Server the cold store lives in.</param>
[Collection(IntegrationCollectionDefinition.Name)]
[Trait("Category", TestCategories.Integration)]
public sealed class TableStorageOutageReadinessTests(AzuriteFixture azurite, SqlServerFixture sql)
    : IClassFixture<AzuriteFixture>
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetHealthReady_TableStorageStops_Returns503WhileLivenessReturns200()
    {
        // Arrange: ready on the running Azurite, with the Table
        // Storage check among the ones that passed.
        await using var host = AppHostFixture.CreateFactory(
            sql.ConnectionStringFor("TableStorageOutageReadinessTests"),
            azurite.ConnectionString,
            TimeSpan.FromSeconds(15));
        using var client = host.CreateClient();
        var before = await host.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync(Cancellation);
        Assert.Equal(HealthStatus.Healthy, before.Status);
        Assert.Contains(SiloStorageExtensions.HealthCheckName, before.Entries.Keys);

        // Act
        await azurite.StopAsync();
        using var ready = await client.GetAsync(new Uri("/health/ready", UriKind.Relative), Cancellation);
        using var live = await client.GetAsync(new Uri("/health", UriKind.Relative), Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }
}
