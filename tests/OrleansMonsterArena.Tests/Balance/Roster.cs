using Battles.Domain;
using Catalog.Domain;
using Catalog.Features.QueryCatalog;
using Catalog.Infrastructure;

namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// The species the simulator plays with: the Catalog's own seed, so
/// the report measures the numbers the game ships. A monster enters
/// through <see cref="Fighter.Enter"/>, the same way as in the battle
/// grain.
/// </summary>
internal static class Roster
{
    /// <summary>
    /// Every seeded species, ordered by name.
    /// </summary>
    public static IReadOnlyList<RosterEntry> Seeded { get; } =
    [
        .. SpeciesSeed.All
            .OrderBy(species => species.Name, StringComparer.Ordinal)
            .Select(species => SpeciesDtoMapper.ToDto(new SpeciesProfile(species, AbilitiesOf(species.Id))))
            .Select(species => new RosterEntry(
                species.Id,
                species.Name,
                [.. species.Abilities.Select(ability => ability.Id)],
                (slot, participantId) => Fighter.Enter(slot, participantId, species))),
    ];

    private static List<Ability> AbilitiesOf(string speciesId) =>
    [
        .. AbilitySeed.BySpecies
            .Where(link => link.SpeciesId == speciesId)
            .OrderBy(link => link.Position)
            .Select(link => AbilitySeed.All.Single(ability => ability.Id == link.AbilityId)),
    ];
}
