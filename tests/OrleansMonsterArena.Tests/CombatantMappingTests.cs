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
    public void From_WeakenedFighter_StoresItsWeakenedFullHealthAndAttack()
    {
        // Arrange
        var weakened = EasyHandicap.Weaken(Fighter.Enter(3, "bot", Species));

        // Act
        var combatant = Combatant.From(weakened, isBot: true, Species);

        // Assert: full health is the fighter's, not the species', so
        // the monster enters at full health and heals against it.
        Assert.Equal(weakened.MaxHitPoints, combatant.MaxHitPoints);
        Assert.Equal(combatant.MaxHitPoints, combatant.HitPoints);
        Assert.Equal(weakened.Attack, combatant.Attack);
        Assert.Equivalent(weakened, combatant.ToFighter(), strict: true);
    }

    [Fact]
    public void Take_FighterAfterARound_TakesOverHitPointsCooldownsAndEffects()
    {
        // Arrange
        var fighter = Fighter.Enter(0, "player-1", Species);
        var combatant = Combatant.From(fighter, isBot: false, Species);
        var after = (fighter with { HitPoints = 17 })
            .WithCooldown("ember-bite", 3)
            .With(new ActiveEffect(AbilityEffect.Burn, 4, 2))
            .With(new ActiveEffect(AbilityEffect.Guard, 50, 1));

        // Act
        combatant.Take(after);

        // Assert
        Assert.Equivalent(after, combatant.ToFighter(), strict: true);
    }

    [Fact]
    public void Take_EffectsHaveRunOut_ClearsThem()
    {
        // Arrange
        var fighter = Fighter.Enter(0, "player-1", Species);
        var combatant = Combatant.From(fighter.With(new ActiveEffect(AbilityEffect.Slow, 50, 1)), isBot: false, Species);

        // Act
        combatant.Take(fighter);

        // Assert
        Assert.Empty(combatant.Effects);
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
        Assert.Empty(fighter.Effects);
    }
}
