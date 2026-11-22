using Battles.Domain;
using Xunit;

namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// The balance guard rails of ADR-0012, checked against the
/// simulation of the species the Catalog seeds. A change to the rules
/// or to the seed that breaks one of them fails here; the report in
/// <c>docs/balance.md</c> shows where.
/// </summary>
public sealed class BalanceGuardRailTests
{
    /// <summary>
    /// The least and the most of its team battles a species may win.
    /// </summary>
    private const double LowestWinRate = 0.40;
    private const double HighestWinRate = 0.60;

    /// <summary>
    /// The least of its one-on-one battles a species must win against
    /// any species that does not have the element advantage over it.
    /// </summary>
    private const double LowestDuelWinRate = 0.30;

    /// <summary>
    /// The shortest and the longest a one-on-one may be on average,
    /// in rounds.
    /// </summary>
    private const double ShortestDuelAverage = 4;
    private const double LongestDuelAverage = 9;

    /// <summary>
    /// The shortest and the longest a battle of three against three
    /// may be on average, in rounds. Felling three monsters takes
    /// about three times as long as felling one, so the two kinds
    /// of battle cannot share one range.
    /// </summary>
    private const double ShortestTeamAverage = 8;
    private const double LongestTeamAverage = 14;

    /// <summary>
    /// The longest any single battle may be, in rounds.
    /// </summary>
    private const int LongestBattle = 30;

    /// <summary>
    /// The least share of its appearances in which a monster must use
    /// each of its abilities.
    /// </summary>
    private const double LowestUse = 0.10;

    private static BalanceReport Report => SeededBalance.Report;

    [Fact]
    public void TeamBattles_EverySpecies_WinsBetweenFortyAndSixtyPercent()
    {
        // Act
        var outside = Report.Roster
            .Select(entry => (entry.Name, Rate: BalanceReport.WinRate(entry.Id, Report.TeamBattles)))
            .Where(species => species.Rate is < LowestWinRate or > HighestWinRate)
            .Select(species => $"{species.Name} wins {species.Rate:P0}")
            .ToList();

        // Assert
        Assert.True(outside.Count == 0, string.Join("; ", outside));
    }

    [Fact]
    public void Duels_NoSpecies_LosesWorseThanThirtySeventyExceptToItsElementCounter()
    {
        // Act: every species against every other one that does not
        // beat it by element.
        var lopsided = (
            from species in Report.Roster
            from opponent in Report.Roster
            where species.Id != opponent.Id
            where ElementChart.Of(opponent.Element, species.Element) != Effectiveness.Strong
            let rate = Report.DuelWinRate(species.Id, opponent.Id)
            where rate < LowestDuelWinRate
            select $"{species.Name} wins {rate:P0} against {opponent.Name}").ToList();

        // Assert
        Assert.True(lopsided.Count == 0, string.Join("; ", lopsided));
    }

    [Fact]
    public void Duels_OnAverage_LastBetweenFourAndNineRounds()
    {
        // Act
        var average = BalanceReport.AverageRounds(Report.Duels);

        // Assert
        Assert.InRange(average, ShortestDuelAverage, LongestDuelAverage);
    }

    [Fact]
    public void TeamBattles_OnAverage_LastBetweenEightAndFourteenRounds()
    {
        // Act
        var average = BalanceReport.AverageRounds(Report.TeamBattles);

        // Assert
        Assert.InRange(average, ShortestTeamAverage, LongestTeamAverage);
    }

    [Fact]
    public void Battles_EverySingleOne_EndsWithinThirtyRounds()
    {
        // Act
        var all = Report.Duels.Concat(Report.TeamBattles).ToList();

        // Assert
        Assert.Equal(0, all.Sum(matchup => matchup.Undecided));
        Assert.True(all.Max(matchup => matchup.Longest) <= LongestBattle);
    }

    [Fact]
    public void Abilities_EveryOne_IsUsedInAtLeastATenthOfItsMonstersBattles()
    {
        // Act
        var unused = Report.AbilityUsage
            .Where(usage => usage.Rate < LowestUse)
            .Select(usage => $"{usage.SpeciesId} uses {usage.AbilityId} in {usage.Rate:P0}")
            .ToList();

        // Assert
        Assert.True(unused.Count == 0, string.Join("; ", unused));
    }
}
