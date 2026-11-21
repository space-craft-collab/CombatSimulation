using Battles.Domain;
using Microsoft.EntityFrameworkCore;

namespace Battles.Infrastructure;

/// <summary>
/// Write side of the battle snapshots: the hot→cold bridge's one
/// call into SQL (ADR-0003). Deliberately a plain insert — storing
/// a battle twice fails on the primary key instead of being
/// absorbed, since nothing legitimately writes a battle twice.
/// </summary>
/// <param name="contexts">Creates the Battles contexts.</param>
internal sealed class BattleSnapshotWriteRepository(IDbContextFactory<BattlesDbContext> contexts)
{
    /// <summary>
    /// Stores a completed battle.
    /// </summary>
    /// <param name="snapshot">The battle's final state.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes once the snapshot is stored.</returns>
    /// <exception cref="DbUpdateException">The battle is stored
    /// already, or the store rejected it.</exception>
    public async Task AddAsync(BattleSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        context.Snapshots.Add(snapshot);
        await context.SaveChangesAsync(cancellationToken);
    }
}
