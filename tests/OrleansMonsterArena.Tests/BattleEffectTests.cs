using System.Collections.Immutable;
using Battles.Grains.Abstractions;
using Catalog.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Status effects in a running battle (ADR-0012), through the
/// grains: the battle stores them, shows them, and tells the monster
/// grains where their monsters stand after every round. Two humans fight, so
/// the test decides every move; the abilities are found in the
/// Catalog by their effect, so the tests hold when the seed is
/// retuned.
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class BattleEffectTests(AppHostFixture host)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private IGrainFactory Grains => host.Factory.Services.GetRequiredService<IClusterClient>();

    [Fact]
    public async Task SubmitActionAsync_BurningAbility_BurnsTheTargetThroughItsMonsterGrain()
    {
        // Arrange
        var (species, burn) = await FindAsync(AbilityEffect.Burn);
        var battle = await StartAsync(species.Id, "moss-golem");

        // Act: the burning hit, while the target uses its own time to strike.
        await battle.SubmitActionAsync(new BattleAction("player-1", 0, 1, burn.Id));
        await battle.SubmitActionAsync(new BattleAction("player-2", 1, 0, "strike"));
        var burning = await battle.GetStateAsync();
        await PlayRoundOfStrikesAsync(battle);
        var later = await battle.GetStateAsync();

        // Assert: the effect is on the target, one round already used up...
        var effect = Assert.Single(burning.Combatants[1].Effects);
        Assert.Equal(StatusEffect.Burn, effect.Kind);
        Assert.Equal(burn.EffectValue, effect.Value);
        Assert.Equal(burn.EffectRounds - 1, effect.RoundsLeft);

        // ...and the monster grain agrees with the battle about the damage.
        var monster = await Grains.GetGrain<IMonsterInstanceGrain>($"{battle.GetPrimaryKey()}:1").GetSnapshotAsync();
        Assert.Equal(later.Combatants[1].HitPoints, monster.HitPoints);
        Assert.True(later.Combatants[1].HitPoints < burning.Combatants[1].HitPoints);
    }

    [Fact]
    public async Task SubmitActionAsync_Guard_ShowsOnTheMonsterAndBluntsTheNextHit()
    {
        // Arrange: the same strike lands on a guarded and on an
        // unguarded monster of the same species.
        var (species, guard) = await FindAsync(AbilityEffect.Guard);
        var guarded = await StartAsync(species.Id, "ember-fox");
        var open = await StartAsync(species.Id, "ember-fox");

        // Act: round 1 sets the guard up (or not), round 2 is the hit.
        await guarded.SubmitActionAsync(new BattleAction("player-1", 0, 0, guard.Id));
        await guarded.SubmitActionAsync(new BattleAction("player-2", 1, 0, "strike"));
        await PlayRoundOfStrikesAsync(open);
        var guardedBefore = await guarded.GetStateAsync();
        var openBefore = await open.GetStateAsync();
        await PlayRoundOfStrikesAsync(guarded);
        await PlayRoundOfStrikesAsync(open);
        var guardedAfter = await guarded.GetStateAsync();
        var openAfter = await open.GetStateAsync();

        // Assert
        var effect = Assert.Single(guardedBefore.Combatants[0].Effects);
        Assert.Equal(StatusEffect.Guard, effect.Kind);
        Assert.Equal(guard.EffectValue, effect.Value);
        Assert.Empty(openBefore.Combatants[0].Effects);
        Assert.True(
            guardedBefore.Combatants[0].HitPoints - guardedAfter.Combatants[0].HitPoints
            < openBefore.Combatants[0].HitPoints - openAfter.Combatants[0].HitPoints);
    }

    [Fact]
    public async Task SubmitActionAsync_GuardAimedAtAnOpponent_ThrowsArgumentException()
    {
        // Arrange
        var (species, guard) = await FindAsync(AbilityEffect.Guard);
        var battle = await StartAsync(species.Id, "ember-fox");

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => battle.SubmitActionAsync(new BattleAction("player-1", 0, 1, guard.Id)));
    }

    [Fact]
    public async Task SubmitActionAsync_Heal_RestoresHitPointsThroughTheMonsterGrain()
    {
        // Arrange: take some damage first.
        var (species, heal) = await FindAsync(AbilityEffect.Heal);
        var battle = await StartAsync(species.Id, "ember-fox");
        await PlayRoundOfStrikesAsync(battle);
        await PlayRoundOfStrikesAsync(battle);
        var hurt = await battle.GetStateAsync();

        // Act: heal while the opponent uses a move that deals nothing
        // would be ideal; a strike is what every monster has, so the
        // healing is measured on the monster grain's own numbers.
        await battle.SubmitActionAsync(new BattleAction("player-1", 0, 0, heal.Id));
        await battle.SubmitActionAsync(new BattleAction("player-2", 1, 0, "strike"));
        var healed = await battle.GetStateAsync();
        await PlayRoundOfStrikesAsync(battle);
        var struck = await battle.GetStateAsync();

        // Assert: the round with the heal cost less than a round
        // without one, the battle and the monster grain agree, and
        // nothing went above full health.
        var lostWithHeal = hurt.Combatants[0].HitPoints - healed.Combatants[0].HitPoints;
        var lostWithout = healed.Combatants[0].HitPoints - struck.Combatants[0].HitPoints;
        Assert.True(lostWithHeal < lostWithout, $"Lost {lostWithHeal} with the heal, {lostWithout} without.");
        Assert.InRange(healed.Combatants[0].HitPoints, 1, healed.Combatants[0].MaxHitPoints);
        var monster = await Grains.GetGrain<IMonsterInstanceGrain>($"{battle.GetPrimaryKey()}:0").GetSnapshotAsync();
        Assert.Equal(struck.Combatants[0].HitPoints, monster.HitPoints);
        Assert.Equal(heal.Cooldown - 1, struck.Combatants[0].Abilities.Single(ability => ability.AbilityId == heal.Id).RoundsLeft);
    }

    // player-1's monster holds slot 0, player-2's slot 1.
    private async Task<ILiveBattleGrain> StartAsync(string firstSpeciesId, string secondSpeciesId)
    {
        ImmutableArray<ParticipantSetup> sides =
        [
            new("player-1", false, [firstSpeciesId]),
            new("player-2", false, [secondSpeciesId]),
        ];
        var arena = Grains.GetGrain<IArenaGrain>($"test-{Guid.NewGuid()}");

        return Grains.GetGrain<ILiveBattleGrain>(await arena.CreateBattleAsync(sides));
    }

    private static async Task PlayRoundOfStrikesAsync(ILiveBattleGrain battle)
    {
        await battle.SubmitActionAsync(new BattleAction("player-1", 0, 1, "strike"));
        await battle.SubmitActionAsync(new BattleAction("player-2", 1, 0, "strike"));
    }

    // The first seeded species that knows an ability with the effect.
    private async Task<(MonsterSpeciesDto Species, AbilityDto Ability)> FindAsync(AbilityEffect effect)
    {
        var catalog = host.Factory.Services.GetRequiredService<ICatalogQueryService>();
        foreach (var speciesId in Catalog.Infrastructure.SpeciesSeed.All.Select(seeded => seeded.Id))
        {
            var species = await catalog.FindSpeciesAsync(speciesId, Cancellation);
            if (species?.Abilities.FirstOrDefault(ability => ability.Effect == effect) is { } ability)
            {
                return (species, ability);
            }
        }

        throw new InvalidOperationException($"No seeded species knows an ability with the effect {effect}.");
    }
}
