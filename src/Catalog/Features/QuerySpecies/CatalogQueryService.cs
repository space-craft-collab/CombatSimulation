using Catalog.Contracts;
using Catalog.Infrastructure;

namespace Catalog.Features.QuerySpecies;

/// <summary>
/// Serves <see cref="ICatalogQueryService"/> from the species table.
/// </summary>
/// <param name="species">Reads the species table.</param>
internal sealed class CatalogQueryService(SpeciesReadRepository species) : ICatalogQueryService
{
    /// <inheritdoc />
    public async Task<MonsterSpeciesDto?> FindSpeciesAsync(
        string speciesId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(speciesId);

        var found = await species.FindAsync(speciesId, cancellationToken);

        return found is null
            ? null
            : new MonsterSpeciesDto(
                found.Id,
                found.Name,
                found.MaxHitPoints,
                found.Attack,
                found.Defense,
                found.Speed);
    }
}
