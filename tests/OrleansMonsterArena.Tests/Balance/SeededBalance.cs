namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// The simulation of the seeded roster, run once and shared by the
/// report test and the guard rails.
/// </summary>
internal static class SeededBalance
{
    /// <summary>
    /// Battles per matchup and seat order.
    /// </summary>
    public const int GamesPerSeat = 20;

    private static readonly Lazy<BalanceReport> Simulation = new(
        () => BalanceSimulation.Run(Roster.Seeded, GamesPerSeat));

    /// <summary>
    /// What the simulation of the seeded roster measured.
    /// </summary>
    public static BalanceReport Report => Simulation.Value;
}
