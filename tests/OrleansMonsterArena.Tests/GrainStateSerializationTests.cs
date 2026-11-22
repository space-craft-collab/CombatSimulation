using Battles;
using Battles.Domain;
using Battles.Grains;
using Battles.Grains.Abstractions;
using Catalog.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Proves the battle's stored state survives the Orleans serializer
/// with everything in it, without booting a silo: a member the
/// serializer cannot write would otherwise only fail on the first
/// stored round.
/// </summary>
public sealed class GrainStateSerializationTests
{
    [Fact]
    public void Serialize_LiveBattleState_RoundTripsEveryCombatantMember()
    {
        // Arrange
        using var services = new ServiceCollection()
            .AddSerializer(serializer => serializer
                .AddAssembly(typeof(BattlesModule).Assembly)
                .AddAssembly(typeof(BattleState).Assembly))
            .BuildServiceProvider();
        var serializer = services.GetRequiredService<Serializer>();
        var state = new LiveBattleState
        {
            ArenaId = "volcano",
            Status = BattleStatus.InProgress,
            Round = 3,
            Combatants =
            [
                new Combatant
                {
                    Slot = 0,
                    ParticipantId = "player-1",
                    Attack = 14,
                    Defense = 4,
                    Speed = 12,
                    HitPoints = 31,
                    IsBot = false,
                    SpeciesId = "ember-fox",
                    MaxHitPoints = 40,
                    Element = Element.Fire,
                    Abilities =
                    [
                        new CombatantAbility
                        {
                            AbilityId = "flame-dash",
                            Element = Element.Fire,
                            Power = 150,
                            Cooldown = 2,
                            Effect = AbilityEffect.Burn,
                            EffectValue = 4,
                            EffectRounds = 3,
                            CooldownLeft = 1,
                        },
                    ],
                    Effects = [new CombatantEffect { Kind = AbilityEffect.Slow, Value = 50, RoundsLeft = 2 }],
                },
            ],
            PendingActions = { ["player-1"] = new BattleAction("player-1", 0, 1, "strike") },
            TurnDeadline = DateTimeOffset.UnixEpoch,
        };

        // Act
        var copy = serializer.Deserialize<LiveBattleState>(serializer.SerializeToArray(state));

        // Assert
        Assert.NotNull(copy);
        Assert.Equal(3, copy.Round);
        Assert.Equal(state.PendingActions["player-1"], copy.PendingActions["player-1"]);
        var combatant = Assert.Single(copy.Combatants);
        Assert.Equal(Element.Fire, combatant.Element);
        Assert.Equal(31, combatant.HitPoints);
        Assert.Equal("ember-fox", combatant.SpeciesId);
        var effect = Assert.Single(combatant.Effects);
        Assert.Equal((AbilityEffect.Slow, 50, 2), (effect.Kind, effect.Value, effect.RoundsLeft));
        var ability = Assert.Single(combatant.Abilities);
        Assert.Equal("flame-dash", ability.AbilityId);
        Assert.Equal(Element.Fire, ability.Element);
        Assert.Equal(AbilityEffect.Burn, ability.Effect);
        Assert.Equal((150, 2, 4, 3, 1), (ability.Power, ability.Cooldown, ability.EffectValue, ability.EffectRounds, ability.CooldownLeft));
    }

    [Fact]
    public void Deserialize_CombatantStoredBeforeAbilities_ReadsAsAMonsterWithTheBasicAttack()
    {
        // Arrange: the serializer knows members by their [Id] only,
        // so the old shape's bytes are what an old battle left behind.
        using var services = new ServiceCollection()
            .AddSerializer(serializer => serializer
                .AddAssembly(typeof(BattlesModule).Assembly)
                .AddAssembly(typeof(BattleState).Assembly)
                .AddAssembly(typeof(CombatantBeforeAbilities).Assembly))
            .BuildServiceProvider();
        var serializer = services.GetRequiredService<Serializer>();
        var stored = serializer.SerializeToArray(new CombatantBeforeAbilities
        {
            Slot = 1,
            ParticipantId = "bot",
            Attack = 10,
            Defense = 8,
            Speed = 5,
            HitPoints = 42,
            IsBot = true,
            SpeciesId = "lava-crab",
            MaxHitPoints = 60,
        });

        // Act
        var combatant = serializer.Deserialize<Combatant>(stored);

        // Assert: what was stored is there, what was not reads as
        // "nothing yet", and the rules give the monster its strike.
        Assert.NotNull(combatant);
        Assert.Equal((1, "bot", 42, 60), (combatant.Slot, combatant.ParticipantId, combatant.HitPoints, combatant.MaxHitPoints));
        Assert.Equal(Element.Neutral, combatant.Element);
        Assert.Empty(combatant.Abilities);
        Assert.Empty(combatant.Effects);
        var fighter = combatant.ToFighter();
        Assert.Equal(AbilitySpec.StrikeId, Assert.Single(fighter.Abilities).Spec.Id);
        Assert.Empty(fighter.Effects);
    }
}
