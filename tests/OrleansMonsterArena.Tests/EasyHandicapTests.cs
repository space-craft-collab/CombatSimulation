using Battles.Domain;
using Catalog.Contracts;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// The Easy difficulty's handicap (ADR-0013): how a bot's monster is
/// weakened as it enters a battle.
/// </summary>
public sealed class EasyHandicapTests
{
    [Theory]
    [InlineData(100, 75, 75)]
    [InlineData(44, 75, 33)]
    [InlineData(21, 75, 16)]
    [InlineData(14, 75, 11)]
    [InlineData(2, 75, 2)]
    [InlineData(1, 75, 1)]
    [InlineData(1, 10, 1)]
    [InlineData(0, 75, 0)]
    [InlineData(40, 100, 40)]
    public void Scale_Stat_RoundsHalfUpAndKeepsAPositiveStatAtLeastOne(int value, int percent, int expected)
    {
        // Act
        var scaled = EasyHandicap.Scale(value, percent);

        // Assert
        Assert.Equal(expected, scaled);
    }

    [Theory]
    [InlineData(-1, 75)]
    [InlineData(40, -1)]
    public void Scale_NegativeInput_ThrowsArgumentOutOfRangeException(int value, int percent)
    {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => EasyHandicap.Scale(value, percent));
    }

    [Fact]
    public void Weaken_FreshFighter_ScalesFullHealthAndAttackAndEntersAtFullHealth()
    {
        // Arrange
        var fighter = Monsters.Create(3, "bot", hitPoints: 40, attack: 10, defense: 4, speed: 12, element: Element.Fire);

        // Act
        var weakened = EasyHandicap.Weaken(fighter);

        // Assert: 75 % of 40 is 30, of 10 it is 7.5, rounded up to 8.
        Assert.Equal(30, weakened.MaxHitPoints);
        Assert.Equal(30, weakened.HitPoints);
        Assert.Equal(8, weakened.Attack);
    }

    [Fact]
    public void Weaken_FreshFighter_KeepsEverythingElse()
    {
        // Arrange
        var fighter = Monsters.Create(3, "bot", defense: 4, speed: 12, element: Element.Frost)
            .With(new ActiveEffect(AbilityEffect.Guard, 50, 1));

        // Act
        var weakened = EasyHandicap.Weaken(fighter);

        // Assert
        Assert.Equivalent(
            fighter,
            weakened with { MaxHitPoints = fighter.MaxHitPoints, HitPoints = fighter.HitPoints, Attack = fighter.Attack },
            strict: true);
    }

    [Fact]
    public void Weaken_Null_ThrowsArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => EasyHandicap.Weaken(null!));
    }
}
