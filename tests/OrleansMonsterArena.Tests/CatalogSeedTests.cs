using Catalog.Contracts;
using Catalog.Infrastructure;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Checks the Catalog seed against the assumptions the combat rules
/// make about it, without a database.
/// </summary>
public sealed class CatalogSeedTests
{
    [Fact]
    public void BySpecies_EveryLink_NamesASeededSpeciesAndAbility()
    {
        // Arrange
        var species = SpeciesSeed.All.Select(seeded => seeded.Id).ToHashSet();
        var abilities = AbilitySeed.All.Select(seeded => seeded.Id).ToHashSet();

        // Act + Assert
        Assert.All(AbilitySeed.BySpecies, link =>
        {
            Assert.Contains(link.SpeciesId, species);
            Assert.Contains(link.AbilityId, abilities);
        });
    }

    [Fact]
    public void BySpecies_EverySpecies_KnowsTheBasicAttackFirst()
    {
        // Act + Assert
        Assert.All(SpeciesSeed.All, species =>
        {
            var first = AbilitySeed.BySpecies.Single(link => link.SpeciesId == species.Id && link.Position == 0);
            Assert.Equal(AbilitySeed.Strike, first.AbilityId);
        });
    }

    [Fact]
    public void BySpecies_EverySpecies_KnowsMoreThanTheBasicAttack()
    {
        // Act + Assert
        Assert.All(
            SpeciesSeed.All,
            species => Assert.True(AbilitySeed.BySpecies.Count(link => link.SpeciesId == species.Id) > 1));
    }

    [Fact]
    public void All_EveryAbility_IsKnownBySomeSpecies()
    {
        // Act + Assert
        Assert.All(
            AbilitySeed.All,
            ability => Assert.Contains(AbilitySeed.BySpecies, link => link.AbilityId == ability.Id));
    }

    [Fact]
    public void All_BasicAttack_IsNeutralFullPowerAndAlwaysReady()
    {
        // Act
        var strike = AbilitySeed.All.Single(ability => ability.Id == AbilitySeed.Strike);

        // Assert
        Assert.Equal(Element.Neutral, strike.Element);
        Assert.Equal(100, strike.Power);
        Assert.Equal(0, strike.Cooldown);
        Assert.Equal(AbilityEffect.None, strike.Effect);
    }

    [Fact]
    public void All_EveryAbility_HasNumbersThatFitItsEffect()
    {
        // Act + Assert: a guard or a heal is used on the monster
        // itself and deals no damage; everything else is an attack.
        Assert.All(AbilitySeed.All, ability =>
        {
            Assert.True(ability.Cooldown >= 0);
            switch (ability.Effect)
            {
                case AbilityEffect.Guard:
                    Assert.Equal(0, ability.Power);
                    Assert.InRange(ability.EffectValue, 1, 99);
                    Assert.True(ability.EffectRounds > 0);
                    Assert.True(ability.Cooldown > 0);
                    break;
                case AbilityEffect.Heal:
                    Assert.Equal(0, ability.Power);
                    Assert.InRange(ability.EffectValue, 1, 100);
                    Assert.True(ability.Cooldown > 0);
                    break;
                case AbilityEffect.Burn:
                    Assert.True(ability.Power > 0);
                    Assert.True(ability.EffectValue > 0);
                    Assert.True(ability.EffectRounds > 0);
                    break;
                case AbilityEffect.Slow:
                    Assert.True(ability.Power > 0);
                    Assert.InRange(ability.EffectValue, 1, 99);
                    Assert.True(ability.EffectRounds > 0);
                    break;
                default:
                    Assert.True(ability.Power > 0);
                    Assert.Equal(0, ability.EffectValue);
                    Assert.Equal(0, ability.EffectRounds);
                    break;
            }
        });
    }

    [Fact]
    public void All_EverySpecies_HasAnElementAndPositiveStats()
    {
        // Act + Assert
        Assert.All(SpeciesSeed.All, species =>
        {
            Assert.NotEqual(Element.Neutral, species.Element);
            Assert.True(species.MaxHitPoints > 0);
            Assert.True(species.Attack > 0);
            Assert.True(species.Defense >= 0);
            Assert.True(species.Speed > 0);
        });
    }
}
