using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Phase 1 smoke test: the composed host boots and answers the
/// health probe.
/// </summary>
public sealed class HealthEndpointTests
{
    [Fact]
    public async Task GetHealth_HostBoots_Returns200()
    {
        // Arrange
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
