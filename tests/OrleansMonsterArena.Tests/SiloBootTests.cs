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
        var battleId = Guid.NewGuid();
        var cluster = host.Factory.Services.GetRequiredService<IClusterClient>();

        // Act
        var state = await cluster.GetGrain<ILiveBattleGrain>(battleId).GetStateAsync();

        // Assert: a fresh activation answers with its key and initial state.
        Assert.Equal(battleId, state.BattleId);
        Assert.Equal(BattleStatus.Created, state.Status);
    }
}
