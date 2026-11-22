using Battles.Domain;

namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// The decision rules the simulator plays with.
/// </summary>
internal static class Policies
{
    /// <summary>
    /// The game's own bot rule.
    /// </summary>
    public static RoundAction Bot(string participantId, IReadOnlyList<Fighter> fighters)
    {
        var move = BotStrategy.Decide(participantId, fighters);

        return new RoundAction(participantId, move.ActorSlot, move.TargetSlot, move.AbilityId);
    }

    /// <summary>
    /// The bot rule with a share of random legal moves mixed in, so
    /// a matchup is played many different ways instead of one. The
    /// random source is seeded by the caller, which keeps every
    /// simulated battle repeatable.
    /// </summary>
    /// <param name="random">The seeded random source of this side.</param>
    /// <param name="exploration">The share of random moves, 0 to 1.</param>
    /// <returns>The mixed rule.</returns>
    public static Policy Noisy(Random random, double exploration) =>
        (participantId, fighters) =>
        {
            if (random.NextDouble() >= exploration)
            {
                return Bot(participantId, fighters);
            }

            var moves = MoveRule.LegalMoves(participantId, fighters);

            return moves[random.Next(moves.Count)];
        };
}
