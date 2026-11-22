namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// Counts, per species and ability, in how many of its appearances
/// a monster used the ability at least once.
/// </summary>
/// <param name="roster">The species that are played.</param>
internal sealed class UsageTally(IReadOnlyList<RosterEntry> roster)
{
    private readonly Dictionary<string, int> _appearances = roster.ToDictionary(entry => entry.Id, _ => 0);

    private readonly Dictionary<(string SpeciesId, string AbilityId), int> _used = roster
        .SelectMany(entry => entry.AbilityIds.Select(abilityId => (entry.Id, abilityId)))
        .ToDictionary(key => key, _ => 0);

    /// <summary>
    /// Adds one battle to the count.
    /// </summary>
    /// <param name="lineup">The species in slot order.</param>
    /// <param name="battle">How the battle went.</param>
    public void Count(IReadOnlyList<RosterEntry> lineup, SimulatedBattle battle)
    {
        foreach (var entry in lineup)
        {
            _appearances[entry.Id]++;
        }

        foreach (var use in battle.Uses.Distinct())
        {
            _used[(lineup[use.Slot].Id, use.AbilityId)]++;
        }
    }

    /// <summary>
    /// The count so far, one entry per species and ability.
    /// </summary>
    /// <returns>The usage of every ability.</returns>
    public List<AbilityUsage> ToList() =>
        [.. _used.Select(pair => new AbilityUsage(
            pair.Key.SpeciesId, pair.Key.AbilityId, _appearances[pair.Key.SpeciesId], pair.Value))];
}
