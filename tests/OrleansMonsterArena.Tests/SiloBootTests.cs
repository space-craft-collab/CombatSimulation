using Battles.Grains.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Proves ADR-0002: the silo runs inside the web host and web-side
/// code reaches grains through the local <see cref="IClusterClient"/>.
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class SiloBootTests(AppHostFixture host)
{
    [Fact]
    public async Task GetGrain_CoHostedSilo_AnswersGrainCall()
    {
        // Arrange
        const string key = "boot-probe";
        var cluster = host.Factory.Services.GetRequiredService<IClusterClient>();

        // Act
        var echoed = await cluster.GetGrain<IPingGrain>(key).PingAsync();

        // Assert
        Assert.Equal(key, echoed);
    }
}
