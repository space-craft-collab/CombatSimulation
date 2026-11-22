using Battles.Domain;
using Catalog.Infrastructure;

namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// The species the simulator plays with: the Catalog's own seed, so
/// the report measures the numbers the game ships.
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
            .Select(species => new RosterEntry(
                species.Id,
                species.Name,
                [Ability.Strike],
                (slot, participantId) => new Fighter
                {
                    Slot = slot,
                    ParticipantId = participantId,
                    Element = species.Element,
                    Attack = species.Attack,
                    Defense = species.Defense,
                    Speed = species.Speed,
                    HitPoints = species.MaxHitPoints,
                })),
    ];
}
