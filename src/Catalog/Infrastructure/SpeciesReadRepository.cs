using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

/// <summary>
/// Read side of the species tables: a species always comes with the
/// abilities it knows. Every call uses its own short-lived,
/// non-tracking context.
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
    public async Task<SpeciesProfile?> FindAsync(string speciesId, CancellationToken cancellationToken)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);

        var species = await context.Species
            .AsNoTracking()
            .SingleOrDefaultAsync(species => species.Id == speciesId, cancellationToken);

        // SQL Server ignores trailing spaces when it compares, so
        // "ember-fox " would find "ember-fox" too; an id matches only exactly.
        if (species?.Id != speciesId)
        {
            return null;
        }

        var abilities = await (
            from link in context.SpeciesAbilities.AsNoTracking()
            where link.SpeciesId == speciesId
            join ability in context.Abilities.AsNoTracking() on link.AbilityId equals ability.Id
            orderby link.Position
            select ability).ToListAsync(cancellationToken);

        return new SpeciesProfile(species, abilities);
    }

    /// <summary>
    /// Lists every species, ordered by display name.
    /// </summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>All species.</returns>
    public async Task<IReadOnlyList<SpeciesProfile>> ListAsync(CancellationToken cancellationToken)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);

        var species = await context.Species
            .AsNoTracking()
            .OrderBy(species => species.Name)
            .ToListAsync(cancellationToken);
        var links = await context.SpeciesAbilities
            .AsNoTracking()
            .OrderBy(link => link.Position)
            .ToListAsync(cancellationToken);
        var abilities = await context.Abilities
            .AsNoTracking()
            .ToDictionaryAsync(ability => ability.Id, cancellationToken);
        var known = links.ToLookup(link => link.SpeciesId, link => abilities[link.AbilityId]);

        return [.. species.Select(found => new SpeciesProfile(found, [.. known[found.Id]]))];
    }
}
