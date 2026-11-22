using Battles.Domain;
using Catalog.Contracts;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Unit tests for the damage rule: defense scales a hit down, the
/// ability's power and the element scale it, and no hit deals less
/// than 1.
/// </summary>
public sealed class DamageRuleTests
{
    [Theory]
    [InlineData(10, 0, 10)]
    [InlineData(14, 4, 10)]
    [InlineData(14, 10, 7)]
    [InlineData(20, 30, 5)]
    public void Calculate_BasicAttack_ScalesDownByDefense(int attack, int defense, int expected)
    {
        // Act
        var damage = DamageRule.Calculate(attack, defense);

        // Assert
        Assert.Equal(expected, damage);
    }

    [Fact]
    public void Calculate_DefenseAtTheScale_HalvesTheHit()
    {
        // Act
        var damage = DamageRule.Calculate(40, DamageRule.DefenseScale);

        // Assert
        Assert.Equal(20, damage);
    }

    [Theory]
    [InlineData(8, 4, 6)]
    [InlineData(3, 10, 2)]
    [InlineData(9, 8, 5)]
    public void Calculate_FractionalResult_RoundsHalvesUp(int attack, int defense, int expected)
    {
        // Act: 5.71, 1.5 and 5.0.
        var damage = DamageRule.Calculate(attack, defense);

        // Assert
        Assert.Equal(expected, damage);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 50)]
    [InlineData(2, 1000)]
    public void Calculate_HitBelowOnePoint_ReturnsOne(int attack, int defense)
    {
        // Act
        var damage = DamageRule.Calculate(attack, defense);

        // Assert
        Assert.Equal(1, damage);
    }

    [Theory]
    [InlineData(100, 10)]
    [InlineData(150, 15)]
    [InlineData(50, 5)]
    public void Calculate_AbilityPower_ScalesTheHit(int power, int expected)
    {
        // Act
        var damage = DamageRule.Calculate(20, DamageRule.DefenseScale, power);

        // Assert
        Assert.Equal(expected, damage);
    }

    [Theory]
    [InlineData(Element.Fire, Element.Nature, 30)]
    [InlineData(Element.Fire, Element.Fire, 20)]
    [InlineData(Element.Fire, Element.Frost, 15)]
    public void Calculate_Element_ScalesTheHit(Element attack, Element target, int expected)
    {
        // Arrange
        var effectiveness = ElementChart.Of(attack, target);

        // Act
        var damage = DamageRule.Calculate(40, DamageRule.DefenseScale, 100, effectiveness);

        // Assert
        Assert.Equal(expected, damage);
    }

    [Fact]
    public void Calculate_NegativeAttack_ThrowsArgumentOutOfRangeException()
    {
        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => DamageRule.Calculate(-1, 5));
    }
}
