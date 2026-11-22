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

    private static readonly AbilitySpec Shield = new("shield", Element.Neutral, 0, 3, AbilityEffect.Guard, 50, 3);

    private static readonly AbilitySpec Mend = new("mend", Element.Neutral, 0, 3, AbilityEffect.Heal, 40);

    private static readonly AbilitySpec Scorch = new("scorch", Element.Neutral, 90, 2, AbilityEffect.Burn, 4, 3);

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
    public void Decide_CanFellAMonster_FinishesItInsteadOfHittingHarderElsewhere()
    {
        // Arrange: the fireball would take 15 off the healthy monster,
        // a plain strike fells the wounded one.
        Fighter[] fighters =
        [
            Monsters.Create(0, "player-1", hitPoints: 40),
            Monsters.Create(1, "player-1", hitPoints: 8, maxHitPoints: 40),
            Monsters.Create(2, "bot-1", abilities: Fireball),
        ];

        // Act
        var move = BotStrategy.Decide("bot-1", fighters);

        // Assert
        Assert.Equal(1, move.TargetSlot);
    }

    [Fact]
    public void Decide_TwoMonstersItCanFell_TakesTheMoreDangerousOne()
    {
        // Arrange
        Fighter[] fighters =
        [
            Monsters.Create(0, "player-1", hitPoints: 5, attack: 8),
            Monsters.Create(1, "player-1", hitPoints: 5, attack: 14),
            Monsters.Create(2, "bot-1"),
        ];

        // Act
        var move = BotStrategy.Decide("bot-1", fighters);

        // Assert
        Assert.Equal(1, move.TargetSlot);
    }

    [Fact]
    public void Decide_ElementAdvantageOnOneTarget_UsesTheAbilityThere()
    {
        // Arrange: two equal opponents, one of them weak to Fire.
        Fighter[] fighters =
        [
            Monsters.Create(0, "player-1", element: Element.Frost),
            Monsters.Create(1, "player-1", element: Element.Nature),
            Monsters.Create(2, "bot-1", abilities: Fireball),
        ];

        // Act
        var move = BotStrategy.Decide("bot-1", fighters);

        // Assert
        Assert.Equal(new BotMove(2, 1, "fireball"), move);
    }

    [Fact]
    public void Decide_LowOnHitPoints_Heals()
    {
        // Arrange
        Fighter[] fighters =
        [
            Monsters.Create(0, "player-1"),
            Monsters.Create(1, "bot-1", hitPoints: 8, maxHitPoints: 40, abilities: Mend),
        ];

        // Act
        var move = BotStrategy.Decide("bot-1", fighters);

        // Assert: a heal names the monster itself as its target.
        Assert.Equal(new BotMove(1, 1, "mend"), move);
    }

    [Fact]
    public void Decide_AtFullHealth_DoesNotHeal()
    {
        // Arrange
        Fighter[] fighters = [Monsters.Create(0, "player-1"), Monsters.Create(1, "bot-1", abilities: Mend)];

        // Act
        var move = BotStrategy.Decide("bot-1", fighters);

        // Assert
        Assert.Equal(new BotMove(1, 0, "strike"), move);
    }

    [Fact]
    public void Decide_LowButCanFellTheLastOpponent_AttacksInsteadOfHealing()
    {
        // Arrange
        Fighter[] fighters =
        [
            Monsters.Create(0, "player-1", hitPoints: 6, maxHitPoints: 40),
            Monsters.Create(1, "bot-1", hitPoints: 8, maxHitPoints: 40, abilities: Mend),
        ];

        // Act
        var move = BotStrategy.Decide("bot-1", fighters);

        // Assert
        Assert.Equal(new BotMove(1, 0, "strike"), move);
    }

    [Fact]
    public void Decide_FacesAHardHitterAlone_Guards()
    {
        // Arrange: the opponent hits for 30 of the bot's 40, the bot
        // for 5 of the opponent's 200.
        Fighter[] fighters =
        [
            Monsters.Create(0, "player-1", hitPoints: 200, attack: 30),
            Monsters.Create(1, "bot-1", attack: 5, abilities: Shield),
        ];

        // Act
        var move = BotStrategy.Decide("bot-1", fighters);

        // Assert
        Assert.Equal(new BotMove(1, 1, "shield"), move);
    }

    [Fact]
    public void Decide_AlreadyGuarded_DoesNotGuardAgain()
    {
        // Arrange
        Fighter[] fighters =
        [
            Monsters.Create(0, "player-1", hitPoints: 200, attack: 30),
            Monsters.Create(1, "bot-1", attack: 5, abilities: Shield).With(new ActiveEffect(AbilityEffect.Guard, 50, 2)),
        ];

        // Act
        var move = BotStrategy.Decide("bot-1", fighters);

        // Assert
        Assert.Equal(new BotMove(1, 0, "strike"), move);
    }

    [Fact]
    public void Decide_BurnWouldAddToTheHit_UsesTheBurningAbility()
    {
        // Arrange: 9 now and 12 over three rounds beats 10 now.
        Fighter[] fighters = [Monsters.Create(0, "player-1"), Monsters.Create(1, "bot-1", abilities: Scorch)];

        // Act
        var move = BotStrategy.Decide("bot-1", fighters);

        // Assert
        Assert.Equal(new BotMove(1, 0, "scorch"), move);
    }

    [Fact]
    public void Decide_TargetAlreadyBurns_UsesThePlainHit()
    {
        // Arrange
        Fighter[] fighters =
        [
            Monsters.Create(0, "player-1").With(new ActiveEffect(AbilityEffect.Burn, 4, 2)),
            Monsters.Create(1, "bot-1", abilities: Scorch),
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
