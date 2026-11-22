using Battles.Domain;
using Xunit;

namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// Unit tests for the loop that plays one simulated battle.
/// </summary>
public sealed class BattleSimulatorTests
{
    private static readonly Dictionary<string, Policy> BotOnBothSides = new()
    {
        ["a"] = Policies.Bot,
        ["b"] = Policies.Bot,
    };

    private static Fighter Monster(int slot, string participantId, int hitPoints, int attack) =>
        Monsters.Create(slot, participantId, hitPoints, attack);

    [Fact]
    public void Play_StrongerSide_WinsAndReportsRoundsAndUses()
    {
        // Arrange: "a" needs two hits, "b" would need four.
        Fighter[] fighters = [Monster(0, "a", 40, 10), Monster(1, "b", 20, 10)];

        // Act
        var battle = BattleSimulator.Play(fighters, BotOnBothSides);

        // Assert
        Assert.Equal("a", battle.WinnerId);
        Assert.Equal(2, battle.Rounds);
        Assert.Equal(3, battle.Uses.Count);
    }

    [Fact]
    public void Play_NobodyCanFellTheOther_IsUndecidedAtTheRoundLimit()
    {
        // Arrange
        Fighter[] fighters = [Monster(0, "a", 1000, 1), Monster(1, "b", 1000, 1)];

        // Act
        var battle = BattleSimulator.Play(fighters, BotOnBothSides);

        // Assert
        Assert.Null(battle.WinnerId);
        Assert.Equal(BattleSimulator.RoundLimit, battle.Rounds);
    }
}
