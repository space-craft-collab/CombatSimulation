using Battles.Domain;
using Catalog.Contracts;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Unit tests for the combat rules of one round, without a silo.
/// </summary>
public sealed class RoundResolverTests
{
    private static readonly AbilitySpec Fireball = new("fireball", Element.Fire, 150, 2);

    private static readonly AbilitySpec Scorch = new("scorch", Element.Neutral, 50, 2, AbilityEffect.Burn, 4, 3);

    private static readonly AbilitySpec Freeze = new("freeze", Element.Neutral, 50, 2, AbilityEffect.Slow, 50, 2);

    private static readonly AbilitySpec Shield = new("shield", Element.Neutral, 0, 3, AbilityEffect.Guard, 50, 2);

    private static readonly AbilitySpec Mend = new("mend", Element.Neutral, 0, 3, AbilityEffect.Heal, 25);

    [Fact]
    public void Resolve_BothSidesStrike_BothLoseHitPoints()
    {
        // Arrange
        Fighter[] fighters = [Monsters.Create(0, "a", attack: 14), Monsters.Create(1, "b", attack: 10)];

        // Act
        var result = RoundResolver.Resolve(fighters, [Monsters.Strike("a", 0, 1), Monsters.Strike("b", 1, 0)]);

        // Assert
        Assert.Equal(30, result.Fighters[0].HitPoints);
        Assert.Equal(26, result.Fighters[1].HitPoints);
        Assert.Equal(2, result.Events.Count);
    }

    [Fact]
    public void Resolve_TargetHasDefense_ScalesTheHitDown()
    {
        // Arrange: defense 10 halves a hit.
        Fighter[] fighters = [Monsters.Create(0, "a", attack: 20), Monsters.Create(1, "b", defense: 10)];

        // Act
        var result = RoundResolver.Resolve(fighters, [Monsters.Strike("a", 0, 1)]);

        // Assert
        Assert.Equal(10, Assert.Single(result.Events).Damage);
    }

    [Fact]
    public void Resolve_DifferentSpeeds_FasterActsFirst()
    {
        // Arrange
        Fighter[] fighters = [Monsters.Create(0, "a", speed: 5), Monsters.Create(1, "b", speed: 12)];

        // Act
        var result = RoundResolver.Resolve(fighters, [Monsters.Strike("a", 0, 1), Monsters.Strike("b", 1, 0)]);

        // Assert
        Assert.Equal([1, 0], result.Events.Select(roundEvent => roundEvent.ActorSlot));
    }

    [Fact]
    public void Resolve_SameSpeed_LowerSlotActsFirst()
    {
        // Arrange
        Fighter[] fighters = [Monsters.Create(0, "a"), Monsters.Create(1, "b")];

        // Act
        var result = RoundResolver.Resolve(fighters, [Monsters.Strike("b", 1, 0), Monsters.Strike("a", 0, 1)]);

        // Assert
        Assert.Equal([0, 1], result.Events.Select(roundEvent => roundEvent.ActorSlot));
    }

    [Fact]
    public void Resolve_ActorFallsEarlierInRound_ItsActionIsDropped()
    {
        // Arrange: the faster monster fells the slower one first.
        Fighter[] fighters = [Monsters.Create(0, "a", speed: 12), Monsters.Create(1, "b", hitPoints: 3, speed: 5)];

        // Act
        var result = RoundResolver.Resolve(fighters, [Monsters.Strike("a", 0, 1), Monsters.Strike("b", 1, 0)]);

        // Assert
        var landed = Assert.Single(result.Events);
        Assert.Equal(0, landed.ActorSlot);
        Assert.Equal(40, result.Fighters[0].HitPoints);
        Assert.True(result.Fighters[1].IsDefeated);
    }

    [Fact]
    public void Resolve_TargetFallsEarlierInRound_LaterActionOnItIsDropped()
    {
        // Arrange: three sides; both attackers pick the same weak target.
        Fighter[] fighters =
        [
            Monsters.Create(0, "a", speed: 12),
            Monsters.Create(1, "b", speed: 8),
            Monsters.Create(2, "c", hitPoints: 2, speed: 1),
        ];

        // Act
        var result = RoundResolver.Resolve(fighters, [Monsters.Strike("a", 0, 2), Monsters.Strike("b", 1, 2)]);

        // Assert
        var landed = Assert.Single(result.Events);
        Assert.Equal(0, landed.ActorSlot);
    }

    [Fact]
    public void Resolve_DamageAboveHitPoints_ReportsHitPointsActuallyLost()
    {
        // Arrange
        Fighter[] fighters = [Monsters.Create(0, "a", attack: 30), Monsters.Create(1, "b", hitPoints: 5)];

        // Act
        var result = RoundResolver.Resolve(fighters, [Monsters.Strike("a", 0, 1)]);

        // Assert
        var landed = Assert.Single(result.Events);
        Assert.Equal(5, landed.Damage);
        Assert.Equal(0, landed.TargetHitPoints);
    }

    [Fact]
    public void Resolve_NoActions_LeavesFightersUntouched()
    {
        // Arrange
        Fighter[] fighters = [Monsters.Create(0, "a"), Monsters.Create(1, "b")];

        // Act
        var result = RoundResolver.Resolve(fighters, []);

        // Assert
        Assert.Empty(result.Events);
        Assert.Equal(fighters, result.Fighters);
    }

    [Fact]
    public void Resolve_UnknownSlot_ThrowsArgumentException()
    {
        // Arrange
        Fighter[] fighters = [Monsters.Create(0, "a"), Monsters.Create(1, "b")];

        // Act + Assert
        Assert.Throws<ArgumentException>(() => RoundResolver.Resolve(fighters, [Monsters.Strike("a", 0, 7)]));
    }

    [Fact]
    public void Resolve_AbilityWithPower_DealsThatShareOfTheAttack()
    {
        // Arrange
        Fighter[] fighters = [Monsters.Create(0, "a", abilities: Fireball), Monsters.Create(1, "b")];

        // Act
        var result = RoundResolver.Resolve(fighters, [new RoundAction("a", 0, 1, "fireball")]);

        // Assert
        var landed = Assert.Single(result.Events);
        Assert.Equal("fireball", landed.AbilityId);
        Assert.Equal(15, landed.Damage);
        Assert.Equal(Effectiveness.Normal, landed.Effectiveness);
    }

    [Theory]
    [InlineData(Element.Nature, 23)]
    [InlineData(Element.Fire, 15)]
    [InlineData(Element.Frost, 11)]
    public void Resolve_ElementalAbility_ScalesWithTheTargetsElement(Element target, int expected)
    {
        // Arrange: 15 damage, ×1.5 against Nature, ×0.75 against Frost.
        Fighter[] fighters =
        [
            Monsters.Create(0, "a", abilities: Fireball),
            Monsters.Create(1, "b", element: target),
        ];

        // Act
        var result = RoundResolver.Resolve(fighters, [new RoundAction("a", 0, 1, "fireball")]);

        // Assert
        Assert.Equal(expected, Assert.Single(result.Events).Damage);
    }

    [Fact]
    public void Resolve_BasicAttack_IgnoresTheAttackersElement()
    {
        // Arrange: a Fire monster strikes a Nature monster.
        Fighter[] fighters =
        [
            Monsters.Create(0, "a", element: Element.Fire),
            Monsters.Create(1, "b", element: Element.Nature),
        ];

        // Act
        var result = RoundResolver.Resolve(fighters, [Monsters.Strike("a", 0, 1)]);

        // Assert
        var landed = Assert.Single(result.Events);
        Assert.Equal(10, landed.Damage);
        Assert.Equal(Effectiveness.Normal, landed.Effectiveness);
    }

    [Fact]
    public void Resolve_AbilityWithCooldown_IsNotReadyForThatManyRounds()
    {
        // Arrange: a cooldown of 2.
        IReadOnlyList<Fighter> fighters = [Monsters.Create(0, "a", abilities: Fireball), Monsters.Create(1, "b", hitPoints: 400)];

        // Act: used in round 1, then two rounds of basic attacks.
        var afterUse = RoundResolver.Resolve(fighters, [new RoundAction("a", 0, 1, "fireball")]).Fighters;
        var oneLater = RoundResolver.Resolve(afterUse, [Monsters.Strike("a", 0, 1)]).Fighters;
        var twoLater = RoundResolver.Resolve(oneLater, [Monsters.Strike("a", 0, 1)]).Fighters;

        // Assert
        Assert.Equal(2, afterUse[0].Find("fireball")!.CooldownLeft);
        Assert.Equal(1, oneLater[0].Find("fireball")!.CooldownLeft);
        Assert.True(twoLater[0].Find("fireball")!.IsReady);
    }

    [Fact]
    public void Resolve_AbilityOnCooldown_ThrowsArgumentException()
    {
        // Arrange
        Fighter[] fighters =
        [
            Monsters.Create(0, "a", abilities: Fireball).WithCooldown("fireball", 1),
            Monsters.Create(1, "b"),
        ];

        // Act + Assert
        Assert.Throws<ArgumentException>(
            () => RoundResolver.Resolve(fighters, [new RoundAction("a", 0, 1, "fireball")]));
    }

    [Fact]
    public void Resolve_MonsterDoesNotAct_ItsCooldownStillRunsDown()
    {
        // Arrange: slot 0 waits out its cooldown while slot 1 fights.
        Fighter[] fighters =
        [
            Monsters.Create(0, "a", abilities: Fireball).WithCooldown("fireball", 2),
            Monsters.Create(1, "a"),
            Monsters.Create(2, "b"),
        ];

        // Act
        var result = RoundResolver.Resolve(fighters, [Monsters.Strike("a", 1, 2)]);

        // Assert
        Assert.Equal(1, result.Fighters[0].Find("fireball")!.CooldownLeft);
    }

    [Fact]
    public void Resolve_DroppedAction_DoesNotSpendItsCooldown()
    {
        // Arrange: the slower monster falls before its fireball.
        Fighter[] fighters =
        [
            Monsters.Create(0, "a", attack: 50, speed: 12),
            Monsters.Create(1, "b", hitPoints: 10, speed: 5, abilities: Fireball),
        ];

        // Act
        var result = RoundResolver.Resolve(
            fighters, [Monsters.Strike("a", 0, 1), new RoundAction("b", 1, 0, "fireball")]);

        // Assert
        Assert.Single(result.Events);
        Assert.True(result.Fighters[1].Find("fireball")!.IsReady);
    }

    [Fact]
    public void Resolve_BurningAbility_HitsThenBurnsAtTheEndOfEachRound()
    {
        // Arrange: 5 damage, then 4 per round for three rounds.
        IReadOnlyList<Fighter> fighters = [Monsters.Create(0, "a", abilities: Scorch), Monsters.Create(1, "b")];

        // Act
        var first = RoundResolver.Resolve(fighters, [new RoundAction("a", 0, 1, "scorch")]);
        var second = RoundResolver.Resolve(first.Fighters, []);
        var third = RoundResolver.Resolve(second.Fighters, []);
        var fourth = RoundResolver.Resolve(third.Fighters, []);

        // Assert
        Assert.Collection(
            first.Events,
            hit =>
            {
                Assert.Equal(RoundEventKind.Ability, hit.Kind);
                Assert.Equal(5, hit.Damage);
                Assert.Equal(AbilityEffect.Burn, hit.Effect);
            },
            burn =>
            {
                Assert.Equal(RoundEventKind.Burn, burn.Kind);
                Assert.Null(burn.AbilityId);
                Assert.Equal((1, 1), (burn.ActorSlot, burn.TargetSlot));
                Assert.Equal(4, burn.Damage);
                Assert.Equal(31, burn.TargetHitPoints);
            });
        Assert.Equal([31, 27, 23, 23], new[] { first, second, third, fourth }.Select(round => round.Fighters[1].HitPoints));
        Assert.Equal(2, Assert.Single(first.Fighters[1].Effects).RoundsLeft);
        Assert.Empty(third.Fighters[1].Effects);
        Assert.Empty(fourth.Events);
    }

    [Fact]
    public void Resolve_BurnAppliedAgain_ReplacesTheOldOneInsteadOfStacking()
    {
        // Arrange: the target burns for one more round at 9 per round.
        Fighter[] fighters =
        [
            Monsters.Create(0, "a", abilities: Scorch),
            Monsters.Create(1, "b").With(new ActiveEffect(AbilityEffect.Burn, 9, 1)),
        ];

        // Act
        var result = RoundResolver.Resolve(fighters, [new RoundAction("a", 0, 1, "scorch")]);

        // Assert: one burn, the new one, already ticked once.
        var burn = Assert.Single(result.Fighters[1].Effects);
        Assert.Equal((4, 2), (burn.Value, burn.RoundsLeft));
        Assert.Equal(40 - 5 - 4, result.Fighters[1].HitPoints);
    }

    [Fact]
    public void Resolve_BurnFellsTheLastMonster_DecidesTheBattle()
    {
        // Arrange
        Fighter[] fighters =
        [
            Monsters.Create(0, "a"),
            Monsters.Create(1, "b", hitPoints: 3).With(new ActiveEffect(AbilityEffect.Burn, 4, 2)),
        ];

        // Act
        var result = RoundResolver.Resolve(fighters, []);

        // Assert
        var burn = Assert.Single(result.Events);
        Assert.Equal(3, burn.Damage);
        Assert.True(result.Fighters[1].IsDefeated);
        Assert.Empty(result.Fighters[1].Effects);
        Assert.True(RoundResolver.IsDecided(result.Fighters));
    }

    [Fact]
    public void Resolve_BothLastMonstersWouldBurnDown_OnlyTheLowerSlotFalls()
    {
        // Arrange: the battle is over the moment one side is gone, so
        // the second burn is never dealt and somebody wins.
        Fighter[] fighters =
        [
            Monsters.Create(0, "a", hitPoints: 2).With(new ActiveEffect(AbilityEffect.Burn, 4, 2)),
            Monsters.Create(1, "b", hitPoints: 2).With(new ActiveEffect(AbilityEffect.Burn, 4, 2)),
        ];

        // Act
        var result = RoundResolver.Resolve(fighters, []);

        // Assert
        Assert.Single(result.Events);
        Assert.True(result.Fighters[0].IsDefeated);
        Assert.Equal(2, result.Fighters[1].HitPoints);
    }

    [Fact]
    public void Resolve_BattleDecidedByAHit_DealsNoBurnAfterwards()
    {
        // Arrange: the winner burns, but the battle is already over.
        Fighter[] fighters =
        [
            Monsters.Create(0, "a", attack: 50).With(new ActiveEffect(AbilityEffect.Burn, 4, 2)),
            Monsters.Create(1, "b", hitPoints: 10),
        ];

        // Act
        var result = RoundResolver.Resolve(fighters, [Monsters.Strike("a", 0, 1)]);

        // Assert
        Assert.Single(result.Events);
        Assert.Equal(40, result.Fighters[0].HitPoints);
    }

    [Fact]
    public void Resolve_AbilityFellsItsTarget_PutsNoEffectOnIt()
    {
        // Arrange
        Fighter[] fighters = [Monsters.Create(0, "a", abilities: Scorch), Monsters.Create(1, "b", hitPoints: 5)];

        // Act
        var result = RoundResolver.Resolve(fighters, [new RoundAction("a", 0, 1, "scorch")]);

        // Assert
        var hit = Assert.Single(result.Events);
        Assert.Equal(AbilityEffect.None, hit.Effect);
        Assert.Empty(result.Fighters[1].Effects);
    }

    [Fact]
    public void Resolve_Guard_HalvesHitsWhileItLasts()
    {
        // Arrange: a guard of 50 % for two rounds, used by the faster monster.
        IReadOnlyList<Fighter> fighters =
        [
            Monsters.Create(0, "a", speed: 12, abilities: Shield),
            Monsters.Create(1, "b", speed: 5),
        ];

        // Act
        var first = RoundResolver.Resolve(fighters, [new RoundAction("a", 0, 0, "shield"), Monsters.Strike("b", 1, 0)]);
        var second = RoundResolver.Resolve(first.Fighters, [Monsters.Strike("b", 1, 0)]);
        var third = RoundResolver.Resolve(second.Fighters, [Monsters.Strike("b", 1, 0)]);

        // Assert: 5 and 5 under the guard, 10 once it has run out.
        var guard = first.Events[0];
        Assert.Equal(AbilityEffect.Guard, guard.Effect);
        Assert.Equal((0, 0, 0), (guard.TargetSlot, guard.Damage, guard.Healing));
        Assert.Equal([35, 30, 20], new[] { first, second, third }.Select(round => round.Fighters[0].HitPoints));
        Assert.Empty(second.Fighters[0].Effects);
    }

    [Fact]
    public void Resolve_GuardUsedByTheSlowerMonster_DoesNotBluntTheHitBeforeIt()
    {
        // Arrange
        Fighter[] fighters =
        [
            Monsters.Create(0, "a", speed: 5, abilities: Shield),
            Monsters.Create(1, "b", speed: 12),
        ];

        // Act
        var result = RoundResolver.Resolve(fighters, [new RoundAction("a", 0, 0, "shield"), Monsters.Strike("b", 1, 0)]);

        // Assert
        Assert.Equal(30, result.Fighters[0].HitPoints);
    }

    [Fact]
    public void Resolve_Guard_DoesNotBluntABurn()
    {
        // Arrange
        Fighter[] fighters =
        [
            Monsters.Create(0, "a")
                .With(new ActiveEffect(AbilityEffect.Guard, 50, 2))
                .With(new ActiveEffect(AbilityEffect.Burn, 4, 2)),
            Monsters.Create(1, "b"),
        ];

        // Act
        var result = RoundResolver.Resolve(fighters, []);

        // Assert
        Assert.Equal(36, result.Fighters[0].HitPoints);
    }

    [Fact]
    public void Resolve_SlowedMonster_ActsAfterAMonsterItWouldOutrun()
    {
        // Arrange: speed 12 at half is 6, below 10.
        Fighter[] fighters =
        [
            Monsters.Create(0, "a", speed: 10),
            Monsters.Create(1, "b", speed: 12).With(new ActiveEffect(AbilityEffect.Slow, 50, 2)),
        ];

        // Act
        var result = RoundResolver.Resolve(fighters, [Monsters.Strike("a", 0, 1), Monsters.Strike("b", 1, 0)]);

        // Assert
        Assert.Equal([0, 1], result.Events.Select(roundEvent => roundEvent.ActorSlot));
    }

    [Fact]
    public void Resolve_SlowingAbility_TakesHoldFromTheNextRoundAndRunsOut()
    {
        // Arrange: a slow of two rounds, this one included.
        IReadOnlyList<Fighter> fighters =
        [
            Monsters.Create(0, "a", speed: 10, abilities: Freeze),
            Monsters.Create(1, "b", speed: 12, hitPoints: 400),
        ];

        // Act
        var first = RoundResolver.Resolve(fighters, [new RoundAction("a", 0, 1, "freeze"), Monsters.Strike("b", 1, 0)]);
        var second = RoundResolver.Resolve(first.Fighters, [Monsters.Strike("a", 0, 1), Monsters.Strike("b", 1, 0)]);
        var third = RoundResolver.Resolve(second.Fighters, [Monsters.Strike("a", 0, 1), Monsters.Strike("b", 1, 0)]);

        // Assert: the order is set when the round begins.
        Assert.Equal([1, 0], first.Events.Select(roundEvent => roundEvent.ActorSlot));
        Assert.Equal(AbilityEffect.Slow, first.Events[1].Effect);
        Assert.Equal([0, 1], second.Events.Select(roundEvent => roundEvent.ActorSlot));
        Assert.Equal([1, 0], third.Events.Select(roundEvent => roundEvent.ActorSlot));
    }

    [Fact]
    public void Resolve_Heal_RestoresAShareOfFullHealth()
    {
        // Arrange: 25 % of 80.
        Fighter[] fighters =
        [
            Monsters.Create(0, "a", hitPoints: 30, maxHitPoints: 80, abilities: Mend),
            Monsters.Create(1, "b"),
        ];

        // Act
        var result = RoundResolver.Resolve(fighters, [new RoundAction("a", 0, 0, "mend")]);

        // Assert
        var heal = Assert.Single(result.Events);
        Assert.Equal(AbilityEffect.Heal, heal.Effect);
        Assert.Equal((20, 0, 50), (heal.Healing, heal.Damage, heal.TargetHitPoints));
        Assert.Equal(50, result.Fighters[0].HitPoints);
        Assert.Equal(3, result.Fighters[0].Find("mend")!.CooldownLeft);
    }

    [Fact]
    public void Resolve_HealNearFullHealth_StopsAtFullHealth()
    {
        // Arrange
        Fighter[] fighters =
        [
            Monsters.Create(0, "a", hitPoints: 75, maxHitPoints: 80, abilities: Mend),
            Monsters.Create(1, "b"),
        ];

        // Act
        var result = RoundResolver.Resolve(fighters, [new RoundAction("a", 0, 0, "mend")]);

        // Assert
        Assert.Equal(5, Assert.Single(result.Events).Healing);
        Assert.Equal(80, result.Fighters[0].HitPoints);
    }

    [Fact]
    public void Resolve_HealerFallsBeforeItActs_HealIsDropped()
    {
        // Arrange
        Fighter[] fighters =
        [
            Monsters.Create(0, "a", hitPoints: 5, maxHitPoints: 80, speed: 5, abilities: Mend),
            Monsters.Create(1, "b", speed: 12),
        ];

        // Act
        var result = RoundResolver.Resolve(fighters, [new RoundAction("a", 0, 0, "mend"), Monsters.Strike("b", 1, 0)]);

        // Assert
        Assert.Single(result.Events);
        Assert.True(result.Fighters[0].IsDefeated);
    }

    [Theory]
    [InlineData(40, 40, false)]
    [InlineData(40, 0, true)]
    [InlineData(0, 0, true)]
    public void IsDecided_TwoSides_TellsWhetherBothStillStand(int first, int second, bool expected)
    {
        // Arrange
        Fighter[] fighters = [Monsters.Create(0, "a", hitPoints: first), Monsters.Create(1, "b", hitPoints: second)];

        // Act
        var decided = RoundResolver.IsDecided(fighters);

        // Assert
        Assert.Equal(expected, decided);
    }
}
