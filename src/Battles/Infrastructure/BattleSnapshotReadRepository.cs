using Battles.Domain;
using Microsoft.EntityFrameworkCore;

namespace Battles.Infrastructure;

/// <summary>
/// Read side of the battle snapshots: completed battles whose hot
/// state is gone (ADR-0003). Every call uses its own short-lived,
/// non-tracking context.
/// </summary>
/// <param name="contexts">Creates the Battles contexts.</param>
internal sealed class BattleSnapshotReadRepository(IDbContextFactory<BattlesDbContext> contexts)
{
    /// <summary>
    /// Looks up a completed battle.
    /// </summary>
    /// <param name="battleId">The battle id.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The snapshot, or <see langword="null"/> if the cold
    /// store does not know the battle.</returns>
    public async Task<BattleSnapshot?> FindAsync(Guid battleId, CancellationToken cancellationToken)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);

        return await context.Snapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(battle => battle.BattleId == battleId, cancellationToken);
    }

    /// <summary>
    /// Lists the completed battles a participant fought in, newest
    /// first.
    /// </summary>
    /// <param name="participantId">The player (or bot) id.</param>
    /// <param name="limit">The most battles to return.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The snapshots, at most <paramref name="limit"/>.</returns>
    public async Task<IReadOnlyList<BattleSnapshot>> ListForParticipantAsync(
        string participantId,
        int limit,
        CancellationToken cancellationToken)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);

        return await context.Snapshots
            .AsNoTracking()
            .Where(battle => battle.Combatants.Any(combatant => combatant.ParticipantId == participantId))
            .OrderByDescending(battle => battle.CompletedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
