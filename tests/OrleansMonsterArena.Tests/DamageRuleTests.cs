using Battles.Domain;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Unit tests for the damage rule: attack minus defense, floored at
/// 1 so that no matchup can stall a battle.
/// </summary>
public sealed class DamageRuleTests
{
    [Theory]
    [InlineData(14, 4, 10)]
    [InlineData(10, 8, 2)]
    [InlineData(9, 8, 1)]
    public void Calculate_AttackAboveDefense_ReturnsDifference(int attack, int defense, int expected)
    {
        // Act
        var damage = DamageRule.Calculate(attack, defense);

        // Assert
        Assert.Equal(expected, damage);
    }

    [Theory]
    [InlineData(8, 8)]
    [InlineData(8, 10)]
    [InlineData(0, 50)]
    public void Calculate_DefenseAtLeastAttack_ReturnsOne(int attack, int defense)
    {
        // Act
        var damage = DamageRule.Calculate(attack, defense);

        // Assert
        Assert.Equal(1, damage);
    }
}
