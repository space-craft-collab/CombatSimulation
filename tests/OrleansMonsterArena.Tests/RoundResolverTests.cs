using Battles.Domain;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Unit tests for the combat rules of one round, without a silo.
/// </summary>
public sealed class RoundResolverTests
{
    private static Fighter Monster(
        int slot,
        string participantId,
        int hitPoints = 40,
        int attack = 10,
        int defense = 4,
        int speed = 10) =>
        new()
        {
            Slot = slot,
            ParticipantId = participantId,
            Attack = attack,
            Defense = defense,
            Speed = speed,
            HitPoints = hitPoints,
        };

    private static RoundAction Strike(string participantId, int actorSlot, int targetSlot) =>
        new(participantId, actorSlot, targetSlot, Ability.Strike);

    [Fact]
    public void Resolve_BothSidesStrike_BothLoseHitPoints()
    {
        // Arrange
        Fighter[] fighters = [Monster(0, "a", attack: 14), Monster(1, "b", attack: 10)];

        // Act
        var result = RoundResolver.Resolve(fighters, [Strike("a", 0, 1), Strike("b", 1, 0)]);

        // Assert
        Assert.Equal(34, result.Fighters[0].HitPoints);
        Assert.Equal(30, result.Fighters[1].HitPoints);
        Assert.Equal(2, result.Events.Count);
    }

    [Fact]
    public void Resolve_DifferentSpeeds_FasterActsFirst()
    {
        // Arrange
        Fighter[] fighters = [Monster(0, "a", speed: 5), Monster(1, "b", speed: 12)];

        // Act
        var result = RoundResolver.Resolve(fighters, [Strike("a", 0, 1), Strike("b", 1, 0)]);

        // Assert
        Assert.Equal([1, 0], result.Events.Select(roundEvent => roundEvent.ActorSlot));
    }

    [Fact]
    public void Resolve_SameSpeed_LowerSlotActsFirst()
    {
        // Arrange
        Fighter[] fighters = [Monster(0, "a"), Monster(1, "b")];

        // Act
        var result = RoundResolver.Resolve(fighters, [Strike("b", 1, 0), Strike("a", 0, 1)]);

        // Assert
        Assert.Equal([0, 1], result.Events.Select(roundEvent => roundEvent.ActorSlot));
    }

    [Fact]
    public void Resolve_ActorFallsEarlierInRound_ItsActionIsDropped()
    {
        // Arrange: the faster monster fells the slower one first.
        Fighter[] fighters = [Monster(0, "a", speed: 12), Monster(1, "b", hitPoints: 3, speed: 5)];

        // Act
        var result = RoundResolver.Resolve(fighters, [Strike("a", 0, 1), Strike("b", 1, 0)]);

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
            Monster(0, "a", speed: 12),
            Monster(1, "b", speed: 8),
            Monster(2, "c", hitPoints: 2, speed: 1),
        ];

        // Act
        var result = RoundResolver.Resolve(fighters, [Strike("a", 0, 2), Strike("b", 1, 2)]);

        // Assert
        var landed = Assert.Single(result.Events);
        Assert.Equal(0, landed.ActorSlot);
    }

    [Fact]
    public void Resolve_DamageAboveHitPoints_ReportsHitPointsActuallyLost()
    {
        // Arrange
        Fighter[] fighters = [Monster(0, "a", attack: 30), Monster(1, "b", hitPoints: 5)];

        // Act
        var result = RoundResolver.Resolve(fighters, [Strike("a", 0, 1)]);

        // Assert
        var landed = Assert.Single(result.Events);
        Assert.Equal(5, landed.Damage);
        Assert.Equal(0, landed.TargetHitPoints);
    }

    [Fact]
    public void Resolve_NoActions_LeavesFightersUntouched()
    {
        // Arrange
        Fighter[] fighters = [Monster(0, "a"), Monster(1, "b")];

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
        Fighter[] fighters = [Monster(0, "a"), Monster(1, "b")];

        // Act + Assert
        Assert.Throws<ArgumentException>(() => RoundResolver.Resolve(fighters, [Strike("a", 0, 7)]));
    }
}
