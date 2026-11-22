using Battles.Features.GetBattle;
using Battles.Grains.Abstractions;
using Xunit;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Assertions on battle outcomes that hold under any balance numbers.
/// </summary>
internal static class BattleAssert
{
    /// <summary>
    /// Asserts that the battle completed with a winner: the winner is
    /// one of the participants and still has a monster standing, every
    /// other side has none.
    /// </summary>
    /// <param name="battle">The battle to check.</param>
    /// <returns>The winner's participant id.</returns>
    public static string Decided(BattleResponse battle)
    {
        ArgumentNullException.ThrowIfNull(battle);

        Assert.Equal(BattleStatus.Completed, battle.Status);
        var winner = Assert.IsType<string>(battle.WinnerId);
        var participants = BattleRounds.ParticipantIds(battle);
        Assert.Contains(winner, participants);
        Assert.NotEmpty(BattleRounds.StandingOf(battle, winner));
        Assert.All(
            participants.Where(participant => participant != winner),
            loser => Assert.Empty(BattleRounds.StandingOf(battle, loser)));

        return winner;
    }
}
