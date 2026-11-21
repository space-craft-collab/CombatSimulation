using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Players.Domain;

namespace Players.Infrastructure;

/// <summary>
/// Write side of the player stats table. Counters are raised in the
/// database (<c>UPDATE … SET Wins = Wins + 1</c>), never read and
/// written back, so concurrent battles of one player cannot lose a
/// count.
/// </summary>
/// <param name="contexts">Creates the Players contexts.</param>
internal sealed class PlayerStatsWriteRepository(IDbContextFactory<PlayersDbContext> contexts)
{
    // SQL Server error numbers for a primary key or unique index clash.
    private const int PrimaryKeyViolation = 2627;
    private const int UniqueIndexViolation = 2601;

    /// <summary>
    /// Adds wins and losses to a player's record, creating the record
    /// on the player's first decided battle.
    /// </summary>
    /// <param name="playerId">The player id.</param>
    /// <param name="wins">Wins to add.</param>
    /// <param name="losses">Losses to add.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes once the counts are stored.</returns>
    public async Task IncrementAsync(string playerId, int wins, int losses, CancellationToken cancellationToken)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);

        if (await AddToExistingAsync(context, playerId, wins, losses, cancellationToken) > 0)
        {
            return;
        }

        context.Stats.Add(new PlayerStats(playerId, wins, losses));

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: PrimaryKeyViolation or UniqueIndexViolation })
        {
            // A concurrent first battle of the same player inserted the
            // row in between; the row exists now, so add to it instead.
            await AddToExistingAsync(context, playerId, wins, losses, cancellationToken);
        }
    }

    private static Task<int> AddToExistingAsync(
        PlayersDbContext context,
        string playerId,
        int wins,
        int losses,
        CancellationToken cancellationToken) =>
        context.Stats
            .Where(stats => stats.PlayerId == playerId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(stats => stats.Wins, stats => stats.Wins + wins)
                    .SetProperty(stats => stats.Losses, stats => stats.Losses + losses),
                cancellationToken);
}
