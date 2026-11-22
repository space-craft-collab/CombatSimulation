namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// Everything one simulation run measured.
/// </summary>
/// <param name="Roster">The species that were played.</param>
/// <param name="GamesPerSeat">The battles per matchup and seat order.</param>
/// <param name="Duels">Every pair of different species, one on one.</param>
/// <param name="TeamBattles">Every pair of different teams of three.</param>
/// <param name="AbilityUsage">How often each species used each of its abilities, over all battles.</param>
internal sealed record BalanceReport(
    IReadOnlyList<RosterEntry> Roster,
    int GamesPerSeat,
    IReadOnlyList<MatchupResult> Duels,
    IReadOnlyList<MatchupResult> TeamBattles,
    IReadOnlyList<AbilityUsage> AbilityUsage)
{
    /// <summary>
    /// The share of their one-on-one battles the first species won
    /// against the second, 0 to 1.
    /// </summary>
    /// <param name="speciesId">The species whose wins are counted.</param>
    /// <param name="opponentId">The species it fought.</param>
    /// <returns>The win rate.</returns>
    /// <exception cref="ArgumentException">The two never fought one on one.</exception>
    public double DuelWinRate(string speciesId, string opponentId)
    {
        foreach (var duel in Duels)
        {
            if (duel.First[0] == speciesId && duel.Second[0] == opponentId)
            {
                return duel.FirstWinRate;
            }

            if (duel.First[0] == opponentId && duel.Second[0] == speciesId)
            {
                return (double)duel.SecondWins / duel.Battles;
            }
        }

        throw new ArgumentException($"'{speciesId}' and '{opponentId}' never fought one on one.", nameof(opponentId));
    }

    /// <summary>
    /// The average length of the battles of some matchups, in rounds.
    /// </summary>
    /// <param name="matchups">The matchups to count.</param>
    /// <returns>The average length.</returns>
    public static double AverageRounds(IReadOnlyList<MatchupResult> matchups)
    {
        ArgumentNullException.ThrowIfNull(matchups);

        return (double)matchups.Sum(matchup => matchup.Rounds) / matchups.Sum(matchup => matchup.Battles);
    }

    /// <summary>
    /// The share of its battles a species won, 0 to 1, counted over
    /// every team it was part of. A species on both sides of a
    /// battle wins and loses it once.
    /// </summary>
    /// <param name="speciesId">The species.</param>
    /// <param name="matchups">The matchups to count.</param>
    /// <returns>The win rate.</returns>
    public static double WinRate(string speciesId, IReadOnlyList<MatchupResult> matchups)
    {
        ArgumentNullException.ThrowIfNull(matchups);

        var battles = 0;
        var wins = 0;
        foreach (var matchup in matchups)
        {
            if (matchup.First.Contains(speciesId))
            {
                battles += matchup.Battles;
                wins += matchup.FirstWins;
            }

            if (matchup.Second.Contains(speciesId))
            {
                battles += matchup.Battles;
                wins += matchup.SecondWins;
            }
        }

        return battles == 0 ? 0 : (double)wins / battles;
    }
}
