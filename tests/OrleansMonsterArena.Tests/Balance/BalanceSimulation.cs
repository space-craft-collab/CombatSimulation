using System.Globalization;
using Battles.Domain;

namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// Plays the whole roster against itself: every pair of different
/// species one on one, and every team of three different species
/// against every other. Each matchup is played several times in both
/// seat orders, since the lower slots win ties.
/// </summary>
internal static class BalanceSimulation
{
    /// <summary>
    /// The share of random legal moves each side mixes into the bot
    /// rule. It stands in for players who do not always find the
    /// best move, and it is what makes two battles of the same
    /// matchup differ.
    /// </summary>
    public const double Exploration = 0.2;

    /// <summary>
    /// The size of a team in the team battles.
    /// </summary>
    public const int TeamSize = 3;

    private const string SideOne = "side-1";
    private const string SideTwo = "side-2";

    /// <summary>
    /// Runs the simulation.
    /// </summary>
    /// <param name="roster">The species to play.</param>
    /// <param name="gamesPerSeat">The battles per matchup and seat order.</param>
    /// <returns>What was measured.</returns>
    public static BalanceReport Run(IReadOnlyList<RosterEntry> roster, int gamesPerSeat)
    {
        ArgumentNullException.ThrowIfNull(roster);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gamesPerSeat);

        var usage = new UsageTally(roster);

        var duelTeams = roster.Select(entry => (IReadOnlyList<RosterEntry>)[entry]).ToList();
        var duels = PlayAll(duelTeams, gamesPerSeat, usage);

        var teams = Combinations(roster, TeamSize).ToList();
        var teamBattles = PlayAll(teams, gamesPerSeat, usage);

        return new BalanceReport(roster, gamesPerSeat, duels, teamBattles, usage.ToList());
    }

    private static List<MatchupResult> PlayAll(
        List<IReadOnlyList<RosterEntry>> teams,
        int gamesPerSeat,
        UsageTally usage)
    {
        var results = new List<MatchupResult>();
        for (var first = 0; first < teams.Count; first++)
        {
            for (var second = first + 1; second < teams.Count; second++)
            {
                results.Add(PlayMatchup(teams[first], teams[second], gamesPerSeat, usage));
            }
        }

        return results;
    }

    private static MatchupResult PlayMatchup(
        IReadOnlyList<RosterEntry> first,
        IReadOnlyList<RosterEntry> second,
        int gamesPerSeat,
        UsageTally usage)
    {
        var firstWins = 0;
        var secondWins = 0;
        var undecided = 0;
        var rounds = 0;
        var longest = 0;

        foreach (var firstSeated in (bool[])[true, false])
        {
            // The team seated first holds the lower slots.
            var seats = firstSeated ? (Low: first, High: second) : (Low: second, High: first);
            var lineup = seats.Low.Concat(seats.High).ToList();

            for (var game = 0; game < gamesPerSeat; game++)
            {
                var fighters = lineup
                    .Select((entry, slot) => entry.Enter(slot, slot < seats.Low.Count ? SideOne : SideTwo))
                    .ToList();
                var key = string.Join('+', first.Select(entry => entry.Id))
                    + "|" + string.Join('+', second.Select(entry => entry.Id))
                    + "|" + (firstSeated ? "a" : "b")
                    + "|" + game.ToString(CultureInfo.InvariantCulture);
                var policies = new Dictionary<string, Policy>
                {
                    [SideOne] = Policies.Noisy(new Random(Seed(key + "|1")), Exploration),
                    [SideTwo] = Policies.Noisy(new Random(Seed(key + "|2")), Exploration),
                };

                var battle = BattleSimulator.Play(fighters, policies);

                usage.Count(lineup, battle);
                rounds += battle.Rounds;
                longest = Math.Max(longest, battle.Rounds);
                switch (battle.WinnerId)
                {
                    case SideOne when firstSeated:
                    case SideTwo when !firstSeated:
                        firstWins++;
                        break;
                    case null:
                        undecided++;
                        break;
                    default:
                        secondWins++;
                        break;
                }
            }
        }

        return new MatchupResult(
            [.. first.Select(entry => entry.Id)],
            [.. second.Select(entry => entry.Id)],
            firstWins,
            secondWins,
            undecided,
            rounds,
            longest);
    }

    // FNV-1a over the battle's key: the same battle gets the same
    // seed in every run and on every machine, which string.GetHashCode
    // does not promise.
    private static int Seed(string key)
    {
        var hash = 2166136261;
        foreach (var character in key)
        {
            hash = unchecked((hash ^ character) * 16777619);
        }

        return unchecked((int)hash);
    }

    private static IEnumerable<IReadOnlyList<RosterEntry>> Combinations(IReadOnlyList<RosterEntry> roster, int size)
    {
        if (size == 0)
        {
            yield return [];
            yield break;
        }

        for (var index = 0; index <= roster.Count - size; index++)
        {
            foreach (var rest in Combinations([.. roster.Skip(index + 1)], size - 1))
            {
                yield return [roster[index], .. rest];
            }
        }
    }
}
