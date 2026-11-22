using Catalog.Contracts;
using Catalog.Infrastructure;

namespace Catalog.Features.QueryCatalog;

/// <summary>
/// Serves <see cref="ICatalogQueryService"/> from the species and
/// arena tables.
/// </summary>
/// <param name="species">Reads the species table.</param>
/// <param name="arenas">Reads the arena table.</param>
internal sealed class CatalogQueryService(
    SpeciesReadRepository species,
    ArenaReadRepository arenas) : ICatalogQueryService
{
    /// <inheritdoc />
    public async Task<MonsterSpeciesDto?> FindSpeciesAsync(
        string speciesId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(speciesId);

        var found = await species.FindAsync(speciesId, cancellationToken);

        return found is null ? null : SpeciesDtoMapper.ToDto(found);
    }

    /// <inheritdoc />
    public async Task<ArenaDto?> FindArenaAsync(
        string arenaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arenaId);

        var found = await arenas.FindAsync(arenaId, cancellationToken);

        return found is null ? null : new ArenaDto(found.Id, found.Name);
    }
}
