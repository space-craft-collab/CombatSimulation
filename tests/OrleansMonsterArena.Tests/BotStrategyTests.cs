using Battles.Domain;
using Catalog.Contracts;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Unit tests for the rule-based bot from ADR-0007.
/// </summary>
public sealed class BotStrategyTests
{
    private static readonly AbilitySpec Fireball = new("fireball", Element.Fire, 150, 2);

    private static readonly AbilitySpec Shield = new("shield", Element.Neutral, 0, 3, AbilityEffect.Guard, 50, 2);

    private static Fighter Monster(int slot, string participantId, int attack, int hitPoints) =>
        Monsters.Create(slot, participantId, hitPoints, attack);

    [Fact]
    public void Decide_SeveralOpponents_TargetsWeakest()
    {
        // Arrange
        Fighter[] fighters =
        [
            Monster(0, "player-1", 8, 80),
            Monster(1, "player-1", 12, 35),
            Monster(2, "bot-1", 14, 40),
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
            Monster(0, "player-1", 10, 60),
            Monster(1, "bot-1", 8, 80),
            Monster(2, "bot-1", 14, 40),
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
            Monster(0, "player-1", 10, 0),
            Monster(1, "player-1", 12, 35),
            Monster(2, "bot-1", 20, 0),
            Monster(3, "bot-1", 8, 80),
        ];

        // Act
        var move = BotStrategy.Decide("bot-1", fighters);

        // Assert
        Assert.Equal(new BotMove(3, 1), move);
    }

    [Fact]
    public void Decide_TiedAttackAndHitPoints_PicksLowestSlots()
    {
        // Arrange: both sides field two identical monsters.
        Fighter[] fighters =
        [
            Monster(0, "player-1", 10, 60),
            Monster(1, "player-1", 10, 60),
            Monster(2, "bot-1", 10, 60),
            Monster(3, "bot-1", 10, 60),
        ];

        // Act
        var move = BotStrategy.Decide("bot-1", fighters);

        // Assert
        Assert.Equal(new BotMove(2, 0), move);
    }

    [Fact]
    public void Decide_StrongerAbilityReady_UsesIt()
    {
        // Arrange
        Fighter[] fighters = [Monsters.Create(0, "player-1"), Monsters.Create(1, "bot-1", abilities: [Fireball, Shield])];

        // Act
        var move = BotStrategy.Decide("bot-1", fighters);

        // Assert
        Assert.Equal(new BotMove(1, 0, "fireball"), move);
    }

    [Fact]
    public void Decide_StrongerAbilityOnCooldown_FallsBackToTheBasicAttack()
    {
        // Arrange
        Fighter[] fighters =
        [
            Monsters.Create(0, "player-1"),
            Monsters.Create(1, "bot-1", abilities: Fireball).WithCooldown("fireball", 2),
        ];

        // Act
        var move = BotStrategy.Decide("bot-1", fighters);

        // Assert
        Assert.Equal(new BotMove(1, 0, "strike"), move);
    }

    [Fact]
    public void Decide_ElementMakesTheAbilityWeaker_UsesTheBasicAttack()
    {
        // Arrange: 120 % of the attack at ×0.75 is less than a plain hit.
        var ember = new AbilitySpec("ember", Element.Fire, 120, 1);
        Fighter[] fighters =
        [
            Monsters.Create(0, "player-1", element: Element.Frost),
            Monsters.Create(1, "bot-1", abilities: ember),
        ];

        // Act
        var move = BotStrategy.Decide("bot-1", fighters);

        // Assert
        Assert.Equal(new BotMove(1, 0, "strike"), move);
    }

    [Fact]
    public void Decide_NoStandingOwnMonster_ThrowsInvalidOperationException()
    {
        // Arrange
        Fighter[] fighters = [Monster(0, "player-1", 10, 60), Monster(1, "bot-1", 14, 0)];

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => BotStrategy.Decide("bot-1", fighters));
    }

    [Fact]
    public void Decide_NoStandingOpponent_ThrowsInvalidOperationException()
    {
        // Arrange
        Fighter[] fighters = [Monster(0, "player-1", 10, 0), Monster(1, "bot-1", 14, 40)];

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => BotStrategy.Decide("bot-1", fighters));
    }
}
