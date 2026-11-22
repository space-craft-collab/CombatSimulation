using Xunit;

namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// The balance report is committed as <c>docs/balance.md</c>. This
/// test keeps it in step with the rules and the Catalog seed: a
/// change to either fails here until the report is refreshed, so the
/// effect on every matchup shows up in the diff.
/// </summary>
public sealed class BalanceReportTests
{
    /// <summary>
    /// Set to <c>1</c> to write the fresh report over the committed
    /// one instead of comparing them.
    /// </summary>
    private const string UpdateVariable = "UPDATE_BALANCE";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Run_SeededRoster_MatchesTheCommittedReport()
    {
        // Arrange
        var path = RepositoryFiles.PathTo("docs", "balance.md");

        // Act
        var fresh = BalanceReportWriter.Write(SeededBalance.Report);

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            await File.WriteAllTextAsync(path, fresh, Cancellation);
        }

        // Assert
        var committed = (await File.ReadAllTextAsync(path, Cancellation)).ReplaceLineEndings("\n");

        Assert.True(
            fresh == committed,
            $"The simulation no longer gives docs/balance.md. Rerun this test with {UpdateVariable}=1 "
            + "and look at what changed (see CONTRIBUTING.md).");
    }

    [Fact]
    public void Run_SameRosterTwice_GivesTheSameReport()
    {
        // Act
        var first = BalanceReportWriter.Write(BalanceSimulation.Run(Roster.Seeded, 2));
        var second = BalanceReportWriter.Write(BalanceSimulation.Run(Roster.Seeded, 2));

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void Run_SeededRoster_PlaysEveryPairAndEveryTeam()
    {
        // Arrange
        var species = Roster.Seeded.Count;
        var teams = Enumerable.Range(1, BalanceSimulation.TeamSize)
            .Aggregate(1, (count, step) => count * (species - step + 1) / step);

        // Act
        var report = BalanceSimulation.Run(Roster.Seeded, 1);

        // Assert
        Assert.Equal(species * (species - 1) / 2, report.Duels.Count);
        Assert.Equal(teams * (teams - 1) / 2, report.TeamBattles.Count);
        Assert.All(report.Duels, duel => Assert.Equal(2, duel.Battles));
    }
}
