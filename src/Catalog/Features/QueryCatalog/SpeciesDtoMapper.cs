using Catalog.Contracts;
using Catalog.Domain;

namespace Catalog.Features.QueryCatalog;

/// <summary>
/// Turns the Catalog's own species into the contract other modules
/// read.
/// </summary>
internal static class SpeciesDtoMapper
{
    /// <summary>
    /// Maps a species and its abilities.
    /// </summary>
    /// <param name="profile">The species with its abilities.</param>
    /// <returns>The species as the contract shows it.</returns>
    public static MonsterSpeciesDto ToDto(SpeciesProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var species = profile.Species;

        return new MonsterSpeciesDto(
            species.Id,
            species.Name,
            species.Element,
            species.MaxHitPoints,
            species.Attack,
            species.Defense,
            species.Speed,
            [.. profile.Abilities.Select(ability => new AbilityDto(
                ability.Id,
                ability.Name,
                ability.Element,
                ability.Power,
                ability.Cooldown,
                ability.Effect,
                ability.EffectValue,
                ability.EffectRounds))]);
    }
}
