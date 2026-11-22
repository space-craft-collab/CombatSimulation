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
}
