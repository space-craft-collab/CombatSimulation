using System.Net;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Smoke test: the composed host boots and answers the health
/// probe.
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class HealthEndpointTests(AppHostFixture host)
{
    [Fact]
    public async Task GetHealth_HostBoots_Returns200()
    {
        // Arrange
        using var client = host.Factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/health", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
