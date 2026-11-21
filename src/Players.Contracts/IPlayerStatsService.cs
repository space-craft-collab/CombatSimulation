namespace Players.Contracts;

/// <summary>
/// Write access to the player stats for other modules (ADR-0005):
/// <c>Battles</c> reports every decided battle here.
/// </summary>
public interface IPlayerStatsService
{
    /// <summary>
    /// Counts a win for the winner and a loss for each loser. A
    /// player seen for the first time starts at zero. Abandoned
    /// battles are not reported, so they count for nobody.
    /// </summary>
    /// <param name="result">The decided battle's sides.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes once every side is counted.</returns>
    /// <exception cref="ArgumentException">A player id is blank, too
    /// long, repeated, or there is no loser.</exception>
    Task RecordResultAsync(BattleResultDto result, CancellationToken cancellationToken = default);
}
