using Battles.Grains.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Guards the parts of the stored grain format that live outside the
/// <c>[Id]</c> numbers: enum values written as numbers and the grain
/// type names that key every stored state and reminder. A failure here
/// means existing Table Storage data would no longer be found or read
/// correctly; see CONTRIBUTING.md.
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class StoredFormatTests(AppHostFixture host)
{
    [Theory]
    [InlineData(BattleStatus.Created, 0)]
    [InlineData(BattleStatus.WaitingForPlayers, 1)]
    [InlineData(BattleStatus.InProgress, 2)]
    [InlineData(BattleStatus.Completed, 3)]
    public void BattleStatus_StoredValue_StaysFrozen(BattleStatus status, int expected)
    {
        // Arrange / Act
        var actual = (int)status;

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BattleStatus_Members_HaveNoUnpinnedAdditions()
    {
        // Arrange / Act
        var count = Enum.GetValues<BattleStatus>().Length;

        // Assert: a new member needs its own InlineData row above.
        Assert.Equal(4, count);
    }

    [Fact]
    public void GetGrainId_EachGrain_KeepsFrozenGrainType()
    {
        // Arrange
        var cluster = host.Factory.Services.GetRequiredService<IClusterClient>();

        // Act
        var battle = cluster.GetGrain<ILiveBattleGrain>(Guid.NewGuid()).GetGrainId().Type.ToString();
        var arena = cluster.GetGrain<IArenaGrain>("volcano").GetGrainId().Type.ToString();
        var monster = cluster.GetGrain<IMonsterInstanceGrain>("m-1").GetGrainId().Type.ToString();

        // Assert
        Assert.Equal("livebattle", battle);
        Assert.Equal("arena", arena);
        Assert.Equal("monsterinstance", monster);
    }
}
