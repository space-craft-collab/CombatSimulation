using Battles.Domain;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Unit tests for the rule-based bot from ADR-0007.
/// </summary>
public sealed class BotStrategyTests
{
    [Fact]
    public void Decide_SeveralOpponents_TargetsWeakest()
    {
        // Arrange
        Fighter[] fighters =
        [
            new(0, "player-1", 8, 80),
            new(1, "player-1", 12, 35),
            new(2, "bot-1", 14, 40),
        ];

        // Act
        var move = BotStrategy.Decide("bot-1", fighters);

        // Assert
        Assert.Equal(new BotMove(2, 1), move);
    }

    [Fact]
    public void Decide_SeveralOwnMonsters_AttacksWithStrongest()
    {
        // Arrange
        Fighter[] fighters =
        [
            new(0, "player-1", 10, 60),
            new(1, "bot-1", 8, 80),
            new(2, "bot-1", 14, 40),
        ];

        // Act
        var move = BotStrategy.Decide("bot-1", fighters);

        // Assert
        Assert.Equal(new BotMove(2, 0), move);
    }

    [Fact]
    public void Decide_DefeatedMonsters_AreIgnored()
    {
        // Arrange
        Fighter[] fighters =
        [
            new(0, "player-1", 10, 0),
            new(1, "player-1", 12, 35),
            new(2, "bot-1", 20, 0),
            new(3, "bot-1", 8, 80),
        ];

        // Act
        var move = BotStrategy.Decide("bot-1", fighters);

        // Assert
        Assert.Equal(new BotMove(3, 1), move);
    }

    [Fact]
    public void Decide_NoStandingOpponent_ThrowsInvalidOperationException()
    {
        // Arrange
        Fighter[] fighters = [new(0, "player-1", 10, 0), new(1, "bot-1", 14, 40)];

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => BotStrategy.Decide("bot-1", fighters));
    }
}
