using Microsoft.EntityFrameworkCore;
using Players.Domain;

namespace Players.Infrastructure;

/// <summary>
/// Read side of the player stats table. Every call uses its own
/// short-lived, non-tracking context.
/// </summary>
/// <param name="contexts">Creates the Players contexts.</param>
internal sealed class PlayerStatsReadRepository(IDbContextFactory<PlayersDbContext> contexts)
{
    /// <summary>
    /// Looks up a player's stats.
    /// </summary>
    /// <param name="playerId">The player id.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The stats, or <see langword="null"/> if the player
    /// has no decided battle yet.</returns>
    public async Task<PlayerStats?> FindAsync(string playerId, CancellationToken cancellationToken)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);

        return await context.Stats
            .AsNoTracking()
            .SingleOrDefaultAsync(stats => stats.PlayerId == playerId, cancellationToken);
    }
}
