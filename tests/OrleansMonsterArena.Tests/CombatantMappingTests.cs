using Battles.Domain;
using Battles.Grains;
using Catalog.Contracts;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// The battle grain stores a <see cref="Combatant"/> and plays its
/// rounds on a <see cref="Fighter"/>. These tests make sure nothing
/// is lost on the way from one to the other and back.
/// </summary>
public sealed class CombatantMappingTests
{
    private static readonly MonsterSpeciesDto Species = new(
        "ember-fox",
        "Ember Fox",
        Element.Fire,
        40,
        14,
        4,
        12,
        [
            new AbilityDto("strike", "Strike", Element.Neutral, 100, 0, AbilityEffect.None, 0, 0),
            new AbilityDto("ember-bite", "Ember Bite", Element.Fire, 80, 3, AbilityEffect.Burn, 4, 3),
        ]);

    [Fact]
    public void ToFighter_FreshlyStoredFighter_IsTheSameFighter()
    {
        // Arrange
        var fighter = Fighter.Enter(3, "player-1", Species);

        // Act
        var copy = Combatant.From(fighter, isBot: false, Species).ToFighter();

        // Assert
        Assert.Equivalent(fighter, copy, strict: true);
    }

    [Fact]
    public void From_Fighter_KeepsWhatOnlyTheBattleStores()
    {
        // Act
        var combatant = Combatant.From(Fighter.Enter(3, "bot", Species), isBot: true, Species);

        // Assert
        Assert.True(combatant.IsBot);
        Assert.Equal("ember-fox", combatant.SpeciesId);
        Assert.Equal(40, combatant.MaxHitPoints);
    }

    [Fact]
    public void Take_FighterAfterARound_TakesOverTheCooldowns()
    {
        // Arrange
        var fighter = Fighter.Enter(0, "player-1", Species);
        var combatant = Combatant.From(fighter, isBot: false, Species);

        // Act
        combatant.Take(fighter.WithCooldown("ember-bite", 3));

        // Assert
        Assert.Equivalent(fighter.WithCooldown("ember-bite", 3), combatant.ToFighter(), strict: true);
    }

    [Fact]
    public void ToFighter_CombatantStoredBeforeAbilitiesExisted_KnowsTheBasicAttack()
    {
        // Arrange
        var combatant = new Combatant
        {
            Slot = 0,
            ParticipantId = "player-1",
            Attack = 14,
            Defense = 4,
            Speed = 12,
            HitPoints = 40,
            IsBot = false,
            SpeciesId = "ember-fox",
            MaxHitPoints = 40,
        };

        // Act
        var fighter = combatant.ToFighter();

        // Assert
        Assert.Equal(AbilitySpec.StrikeId, Assert.Single(fighter.Abilities).Spec.Id);
    }
}
