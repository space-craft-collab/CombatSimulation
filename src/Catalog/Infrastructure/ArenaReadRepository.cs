using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

/// <summary>
/// Read side of the arena table. Every call uses its own
/// short-lived, non-tracking context.
/// </summary>
/// <param name="contexts">Creates the Catalog contexts.</param>
internal sealed class ArenaReadRepository(IDbContextFactory<CatalogDbContext> contexts)
{
    /// <summary>
    /// Looks up an arena by id.
    /// </summary>
    /// <param name="arenaId">The arena id.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The arena, or <see langword="null"/> if it does not exist.</returns>
    public async Task<Arena?> FindAsync(string arenaId, CancellationToken cancellationToken)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);

        var arena = await context.Arenas
            .AsNoTracking()
            .SingleOrDefaultAsync(arena => arena.Id == arenaId, cancellationToken);

        // SQL Server ignores trailing spaces when it compares, so
        // "volcano " would find "volcano" too; an id matches only exactly.
        return arena?.Id == arenaId ? arena : null;
    }

    /// <summary>
    /// Lists every arena, ordered by display name.
    /// </summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>All arenas.</returns>
    public async Task<IReadOnlyList<Arena>> ListAsync(CancellationToken cancellationToken)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);

        return await context.Arenas
            .AsNoTracking()
            .OrderBy(arena => arena.Name)
            .ToListAsync(cancellationToken);
    }
}
