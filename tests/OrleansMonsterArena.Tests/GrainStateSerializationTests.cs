using Battles;
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
    }
}
