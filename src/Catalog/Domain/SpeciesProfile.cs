namespace Catalog.Domain;

/// <summary>
/// A species together with the abilities it knows.
/// </summary>
/// <param name="Species">The species and its base stats.</param>
/// <param name="Abilities">Its abilities in their order, the basic attack first.</param>
internal sealed record SpeciesProfile(MonsterSpecies Species, IReadOnlyList<Ability> Abilities);
