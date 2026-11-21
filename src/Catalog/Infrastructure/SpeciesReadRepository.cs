using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

/// <summary>
/// Read side of the species table. Every call uses its own
/// short-lived, non-tracking context.
/// </summary>
/// <param name="contexts">Creates the Catalog contexts.</param>
internal sealed class SpeciesReadRepository(IDbContextFactory<CatalogDbContext> contexts)
{
    /// <summary>
    /// Looks up a species by id.
    /// </summary>
    /// <param name="speciesId">The species id.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The species, or <see langword="null"/> if it does not exist.</returns>
    public async Task<MonsterSpecies?> FindAsync(string speciesId, CancellationToken cancellationToken)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);

        var species = await context.Species
            .AsNoTracking()
            .SingleOrDefaultAsync(species => species.Id == speciesId, cancellationToken);

        // SQL Server ignores trailing spaces when it compares, so
        // "ember-fox " would find "ember-fox" too; an id matches only exactly.
        return species?.Id == speciesId ? species : null;
    }

    /// <summary>
    /// Lists every species, ordered by display name.
    /// </summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>All species.</returns>
    public async Task<IReadOnlyList<MonsterSpecies>> ListAsync(CancellationToken cancellationToken)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);

        return await context.Species
            .AsNoTracking()
            .OrderBy(species => species.Name)
            .ToListAsync(cancellationToken);
    }
}
