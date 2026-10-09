namespace Catalog.Contracts;

/// <summary>
/// Read access to the Catalog for other modules (ADR-0005).
/// </summary>
public interface ICatalogQueryService
{
    /// <summary>
    /// Looks up a monster species by id.
    /// </summary>
    /// <param name="speciesId">The species id.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The species, or <see langword="null"/> if it does not exist.</returns>
    Task<MonsterSpeciesDto?> FindSpeciesAsync(string speciesId, CancellationToken cancellationToken = default);
}
