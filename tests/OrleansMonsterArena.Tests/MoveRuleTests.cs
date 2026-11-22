using Battles.Domain;
using Catalog.Contracts;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Unit tests for which moves a side may make.
/// </summary>
public sealed class MoveRuleTests
{
    private static readonly AbilitySpec Fireball = new("fireball", Element.Fire, 150, 2);

    private static readonly AbilitySpec Shield = new("shield", Element.Neutral, 0, 3, AbilityEffect.Guard, 50, 2);

    private static Fighter[] TwoAgainstOne() =>
    [
        Monsters.Create(0, "a", abilities: [Fireball, Shield]),
        Monsters.Create(1, "a"),
        Monsters.Create(2, "b"),
    ];

    [Fact]
    public void Reject_ReadyAbilityOnStandingOpponent_ReturnsNull()
    {
        // Act
        var reason = MoveRule.Reject(TwoAgainstOne(), new RoundAction("a", 0, 2, "fireball"));

        // Assert
        Assert.Null(reason);
    }

    [Theory]
    [InlineData("a", 2, 0, "strike")]
    [InlineData("b", 0, 2, "strike")]
    [InlineData("a", 9, 2, "strike")]
    [InlineData("a", -1, 2, "strike")]
    public void Reject_ActorIsNotTheSidesMonster_ReturnsReason(
        string participantId, int actorSlot, int targetSlot, string abilityId)
    {
        // Act
        var reason = MoveRule.Reject(TwoAgainstOne(), new RoundAction(participantId, actorSlot, targetSlot, abilityId));

        // Assert
        Assert.NotNull(reason);
    }

    [Fact]
    public void Reject_DefeatedActor_ReturnsReason()
    {
        // Arrange
        Fighter[] fighters = [Monsters.Create(0, "a", hitPoints: 0), Monsters.Create(1, "a"), Monsters.Create(2, "b")];

        // Act
        var reason = MoveRule.Reject(fighters, Monsters.Strike("a", 0, 2));

        // Assert
        Assert.NotNull(reason);
    }

    [Theory]
    [InlineData("fireball")]
    [InlineData("Strike")]
    [InlineData("")]
    public void Reject_AbilityTheMonsterDoesNotKnow_ReturnsReason(string abilityId)
    {
        // Act: slot 1 knows only the basic attack.
        var reason = MoveRule.Reject(TwoAgainstOne(), new RoundAction("a", 1, 2, abilityId));

        // Assert
        Assert.NotNull(reason);
    }

    [Fact]
    public void Reject_AbilityOnCooldown_ReturnsReason()
    {
        // Arrange
        var fighters = TwoAgainstOne();
        fighters[0] = fighters[0].WithCooldown("fireball", 2);

        // Act
        var reason = MoveRule.Reject(fighters, new RoundAction("a", 0, 2, "fireball"));

        // Assert
        Assert.NotNull(reason);
        Assert.Contains("2 round", reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    public void Reject_AttackOnOwnOrMissingMonster_ReturnsReason(int targetSlot)
    {
        // Act
        var reason = MoveRule.Reject(TwoAgainstOne(), new RoundAction("a", 0, targetSlot, "fireball"));

        // Assert
        Assert.NotNull(reason);
    }

    [Fact]
    public void Reject_AttackOnDefeatedOpponent_ReturnsReason()
    {
        // Arrange
        Fighter[] fighters = [Monsters.Create(0, "a"), Monsters.Create(1, "b", hitPoints: 0), Monsters.Create(2, "b")];

        // Act
        var reason = MoveRule.Reject(fighters, Monsters.Strike("a", 0, 1));

        // Assert
        Assert.NotNull(reason);
    }

    [Fact]
    public void Reject_SelfAbilityOnItself_ReturnsNull()
    {
        // Act
        var reason = MoveRule.Reject(TwoAgainstOne(), new RoundAction("a", 0, 0, "shield"));

        // Assert
        Assert.Null(reason);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Reject_SelfAbilityOnAnotherMonster_ReturnsReason(int targetSlot)
    {
        // Act
        var reason = MoveRule.Reject(TwoAgainstOne(), new RoundAction("a", 0, targetSlot, "shield"));

        // Assert
        Assert.NotNull(reason);
    }

    [Fact]
    public void LegalMoves_SideWithTwoMonsters_ListsEveryReadyAbilityAndTarget()
    {
        // Arrange: the fireball is on cooldown.
        var fighters = TwoAgainstOne();
        fighters[0] = fighters[0].WithCooldown("fireball", 1);

        // Act
        var moves = MoveRule.LegalMoves("a", fighters);

        // Assert
        Assert.Equal(
            [
                new RoundAction("a", 0, 2, "strike"),
                new RoundAction("a", 0, 0, "shield"),
                new RoundAction("a", 1, 2, "strike"),
            ],
            moves);
    }

    [Fact]
    public void LegalMoves_EveryMove_PassesReject()
    {
        // Arrange
        var fighters = TwoAgainstOne();

        // Act
        var moves = MoveRule.LegalMoves("a", fighters);

        // Assert
        Assert.NotEmpty(moves);
        Assert.All(moves, move => Assert.Null(MoveRule.Reject(fighters, move)));
    }

    [Fact]
    public void LegalMoves_NoStandingOpponent_IsEmpty()
    {
        // Arrange
        Fighter[] fighters = [Monsters.Create(0, "a", abilities: Shield), Monsters.Create(1, "b", hitPoints: 0)];

        // Act
        var moves = MoveRule.LegalMoves("a", fighters);

        // Assert
        Assert.Empty(moves);
    }
}
