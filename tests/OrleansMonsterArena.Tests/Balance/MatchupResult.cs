namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// The tally of one matchup: two teams played against each other
/// many times, in both seat orders.
/// </summary>
/// <param name="First">The species of the first team.</param>
/// <param name="Second">The species of the second team.</param>
/// <param name="FirstWins">Battles the first team won.</param>
/// <param name="SecondWins">Battles the second team won.</param>
/// <param name="Undecided">Battles still running at the round limit.</param>
/// <param name="Rounds">The rounds of all battles together.</param>
/// <param name="Longest">The rounds of the longest battle.</param>
internal sealed record MatchupResult(
    IReadOnlyList<string> First,
    IReadOnlyList<string> Second,
    int FirstWins,
    int SecondWins,
    int Undecided,
    int Rounds,
    int Longest)
{
    /// <summary>
    /// The battles played.
    /// </summary>
    public int Battles => FirstWins + SecondWins + Undecided;

    /// <summary>
    /// The share of battles the first team won, 0 to 1.
    /// </summary>
    public double FirstWinRate => (double)FirstWins / Battles;

    /// <summary>
    /// The average length of a battle in rounds.
    /// </summary>
    public double AverageRounds => (double)Rounds / Battles;
}
