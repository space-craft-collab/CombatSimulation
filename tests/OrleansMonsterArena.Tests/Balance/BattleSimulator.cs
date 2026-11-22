using Battles.Domain;

namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// Plays one battle to its end on the pure combat rules
/// (<see cref="RoundResolver"/>), without a silo: every standing
/// side moves once per round, as in the battle grain.
/// </summary>
internal static class BattleSimulator
{
    /// <summary>
    /// The round after which a battle counts as undecided. Far above
    /// what a healthy battle needs; it only keeps a stalemate from
    /// running forever.
    /// </summary>
    public const int RoundLimit = 100;

    /// <summary>
    /// Plays the battle.
    /// </summary>
    /// <param name="fighters">Every fighter at the start.</param>
    /// <param name="policies">The decision rule of each side, by participant id.</param>
    /// <returns>How the battle went.</returns>
    public static SimulatedBattle Play(IReadOnlyList<Fighter> fighters, IReadOnlyDictionary<string, Policy> policies)
    {
        ArgumentNullException.ThrowIfNull(fighters);
        ArgumentNullException.ThrowIfNull(policies);

        var uses = new List<AbilityUse>();
        for (var round = 1; round <= RoundLimit; round++)
        {
            var actions = SidesStanding(fighters)
                .Select(side => policies[side](side, fighters))
                .ToList();

            var result = RoundResolver.Resolve(fighters, actions);
            fighters = result.Fighters;
            uses.AddRange(result.Events.Select(landed => new AbilityUse(landed.ActorSlot, landed.AbilityId)));

            var standing = SidesStanding(fighters);
            if (standing.Count <= 1)
            {
                return new SimulatedBattle(standing.SingleOrDefault(), round, uses);
            }
        }

        return new SimulatedBattle(null, RoundLimit, uses);
    }

    private static List<string> SidesStanding(IReadOnlyList<Fighter> fighters) =>
        [.. fighters.Where(fighter => !fighter.IsDefeated).Select(fighter => fighter.ParticipantId).Distinct()];
}
