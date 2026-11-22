using Battles.Grains.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Proves the grain DTOs survive the silo's own serializer, so a
/// missing <c>[Id]</c> or an unsupported member type fails here and
/// not on the first real grain call.
/// </summary>
/// <param name="host">The shared booted host.</param>
[Collection(AppHostFixture.CollectionName)]
public sealed class GrainContractSerializationTests(AppHostFixture host)
{
    [Fact]
    public void Serialize_BattleSetup_RoundTripsNestedParticipants()
    {
        // Arrange
        var serializer = host.Factory.Services.GetRequiredService<Serializer>();
        var setup = new BattleSetup(
            "volcano",
            [
                new ParticipantSetup("player-1", false, ["ember-fox", "lava-crab"]),
                new ParticipantSetup("bot-1", true, ["frost-owl"], Difficulty.Easy),
            ]);

        // Act
        var copy = serializer.Deserialize<BattleSetup>(serializer.SerializeToArray(setup));

        // Assert
        Assert.NotNull(copy);
        Assert.Equal(setup.ArenaId, copy.ArenaId);
        Assert.Equal(2, copy.Participants.Length);
        Assert.Equal(setup.Participants[0].MonsterSpeciesIds, copy.Participants[0].MonsterSpeciesIds);
        Assert.True(copy.Participants[1].IsBot);
        Assert.Equal(Difficulty.Normal, copy.Participants[0].Difficulty);
        Assert.Equal(Difficulty.Easy, copy.Participants[1].Difficulty);
    }

    [Fact]
    public void Serialize_BattleState_RoundTripsAllFields()
    {
        // Arrange
        var serializer = host.Factory.Services.GetRequiredService<Serializer>();
        var state = new BattleState(
            Guid.NewGuid(),
            "volcano",
            BattleStatus.Completed,
            3,
            "player-1",
            DateTimeOffset.UnixEpoch,
            [
                new CombatantSnapshot(
                    0,
                    "player-1",
                    false,
                    "lava-crab",
                    42,
                    60,
                    [new AbilityCooldown("magma-claw", 2)],
                    [new ActiveEffectSnapshot(StatusEffect.Guard, 50, 2)]),
            ],
            Difficulty.Easy);

        // Act
        var copy = serializer.Deserialize<BattleState>(serializer.SerializeToArray(state));

        // Assert: ImmutableArray compares by reference, so the
        // combatants are checked element-wise.
        Assert.NotNull(copy);
        Assert.Equal(state with { Combatants = [] }, copy with { Combatants = [] });
        var combatant = Assert.Single(copy.Combatants);
        Assert.Equal(
            state.Combatants[0] with { Abilities = [], Effects = [] },
            combatant with { Abilities = [], Effects = [] });
        Assert.Equal(state.Combatants[0].Abilities, combatant.Abilities);
        Assert.Equal(state.Combatants[0].Effects, combatant.Effects);
    }
}
