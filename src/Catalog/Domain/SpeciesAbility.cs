namespace Catalog.Domain;

/// <summary>
/// One ability a species knows.
/// </summary>
/// <param name="SpeciesId">The species.</param>
/// <param name="AbilityId">The ability.</param>
/// <param name="Position">Where the ability stands among the species' abilities, from 0.</param>
internal sealed record SpeciesAbility(string SpeciesId, string AbilityId, int Position);
