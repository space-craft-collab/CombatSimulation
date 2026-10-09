using Catalog.Contracts;
using Catalog.Infrastructure;

namespace Catalog.Features.QuerySpecies;

/// <summary>
/// Serves <see cref="ICatalogQueryService"/> from the Phase 2 species seed.
/// </summary>
internal sealed class CatalogQueryService : ICatalogQueryService
{
    /// <inheritdoc />
    public Task<MonsterSpeciesDto?> FindSpeciesAsync(string speciesId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(speciesId);

        return Task.FromResult(SpeciesSeed.All.GetValueOrDefault(speciesId));
    }
}
