using System.Globalization;
using System.Text;

namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// Writes a <see cref="BalanceReport"/> as the Markdown document
/// committed under <c>docs/balance.md</c>.
/// </summary>
internal static class BalanceReportWriter
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>
    /// Renders the report.
    /// </summary>
    /// <param name="report">What the simulation measured.</param>
    /// <returns>The Markdown document, with LF line endings.</returns>
    public static string Write(BalanceReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var names = report.Roster.ToDictionary(entry => entry.Id, entry => entry.Name);
        var text = new StringBuilder();

        text.Append(
            """
            # Balance report

            Written by `BalanceReportTests` from a simulation on the
            pure combat rules; never edited by hand. After a change
            to the rules or to the Catalog seed, refresh it:

            ```bash
            UPDATE_BALANCE=1 dotnet run --project tests/OrleansMonsterArena.Tests \
              -- -class "OrleansMonsterArena.Tests.Balance.BalanceReportTests"
            ```

            **Not measured:** the Easy difficulty
            ([ADR-0013](adr/0013-bot-difficulty.md)) weakens the
            bot's monsters as they enter a battle. It is a
            deliberate advantage for the human and lies outside
            these numbers and their guard rails.


            """);
        text.Append(Invariant, $"""
            **How it is measured:** every pair of different species
            fights one on one, and every team of {BalanceSimulation.TeamSize} different
            species fights every other such team. Each matchup is
            played {report.GamesPerSeat} times in each seat order. Both sides follow
            the bot rule and replace {Percent(BalanceSimulation.Exploration)} of their moves with
            a random legal one; the random source is seeded per
            battle, so a rerun gives the same numbers.


            """);

        text.Append("## Species\n\n");
        text.Append("Share of its battles a species won.\n\n");
        text.Append("| Species | One on one | In a team |\n|---|---:|---:|\n");
        foreach (var entry in report.Roster)
        {
            text.Append(Invariant, $"| {entry.Name} ");
            text.Append(Invariant, $"| {Percent(BalanceReport.WinRate(entry.Id, report.Duels))} ");
            text.Append(Invariant, $"| {Percent(BalanceReport.WinRate(entry.Id, report.TeamBattles))} |\n");
        }

        text.Append("\n## One on one\n\n");
        text.Append("Wins of the row species against the column species.\n\n");
        AppendDuelTable(text, report, (duels, row, column) => Percent(duels.DuelWinRate(row, column)));
        text.Append("\nAverage length in rounds.\n\n");
        AppendDuelTable(text, report, (duels, row, column) => DuelOf(duels, row, column).AverageRounds.ToString("0.0", Invariant));
        AppendLengths(text, report.Duels);

        text.Append(Invariant, $"\n## Teams of {BalanceSimulation.TeamSize}\n");
        AppendLengths(text, report.TeamBattles);

        text.Append("\n## Ability use\n\n");
        text.Append("Share of a species' appearances in which it used the\nability at least once, over all battles above.\n\n");
        text.Append("| Species | Ability | Used in |\n|---|---|---:|\n");
        foreach (var usage in report.AbilityUsage)
        {
            text.Append(Invariant, $"| {names[usage.SpeciesId]} | `{usage.AbilityId}` | {Percent(usage.Rate)} |\n");
        }

        return text.ToString().ReplaceLineEndings("\n");
    }

    private static void AppendDuelTable(
        StringBuilder text,
        BalanceReport report,
        Func<BalanceReport, string, string, string> cell)
    {
        text.Append("| |");
        foreach (var column in report.Roster)
        {
            text.Append(Invariant, $" {column.Name} |");
        }

        text.Append("\n|---|");
        text.Append(string.Concat(report.Roster.Select(_ => "---:|")));
        text.Append('\n');

        foreach (var row in report.Roster)
        {
            text.Append(Invariant, $"| **{row.Name}** |");
            foreach (var column in report.Roster)
            {
                text.Append(Invariant, $" {(row.Id == column.Id ? "–" : cell(report, row.Id, column.Id))} |");
            }

            text.Append('\n');
        }
    }

    private static MatchupResult DuelOf(BalanceReport report, string rowId, string columnId) =>
        report.Duels.Single(duel =>
            (duel.First[0] == rowId && duel.Second[0] == columnId)
            || (duel.First[0] == columnId && duel.Second[0] == rowId));

    private static void AppendLengths(StringBuilder text, IReadOnlyList<MatchupResult> matchups)
    {
        var battles = matchups.Sum(matchup => matchup.Battles);

        text.Append(Invariant, $"\n- Battles: {battles}\n");
        text.Append(Invariant, $"- Average length: {BalanceReport.AverageRounds(matchups):0.0} rounds\n");
        text.Append(Invariant, $"- Longest battle: {matchups.Max(matchup => matchup.Longest)} rounds\n");
        text.Append(Invariant, $"- Undecided after {BattleSimulator.RoundLimit} rounds: {matchups.Sum(matchup => matchup.Undecided)}\n");
    }

    private static string Percent(double share) =>
        Math.Round(share * 100, MidpointRounding.AwayFromZero).ToString("0", Invariant) + " %";
}
