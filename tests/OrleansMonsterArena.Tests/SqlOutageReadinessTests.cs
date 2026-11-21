using System.Net;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// The probes of a host whose SQL Server is unreachable: not ready,
/// but alive. The host runs as Production, where it applies no
/// migrations on startup and so boots without its database.
/// </summary>
[Collection(IntegrationCollectionDefinition.Name)]
public sealed class SqlOutageReadinessTests
{
    // Nothing listens on port 1; the short connect timeout keeps
    // SQL Server's client from retrying for its default 15 seconds.
    private const string UnreachableSql =
        "Server=127.0.0.1,1;Database=Nowhere;Integrated Security=false;User Id=nobody;Password=none;Connect Timeout=1";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetHealthReady_SqlUnreachable_Returns503WhileLivenessReturns200()
    {
        // Arrange
        await using var host = AppHostFixture.CreateFactory(UnreachableSql, string.Empty, TimeSpan.FromSeconds(15));
        var production = host.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = production.CreateClient();

        // Act
        using var ready = await client.GetAsync(new Uri("/health/ready", UriKind.Relative), Cancellation);
        using var live = await client.GetAsync(new Uri("/health", UriKind.Relative), Cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }
}
