namespace OrleansMonsterArena.Tests.Balance;

/// <summary>
/// How often a species used one of its abilities.
/// </summary>
/// <param name="SpeciesId">The species.</param>
/// <param name="AbilityId">The ability.</param>
/// <param name="Appearances">How many times a monster of the species took part in a battle.</param>
/// <param name="Used">In how many of those the monster used the ability at least once.</param>
internal sealed record AbilityUsage(string SpeciesId, string AbilityId, int Appearances, int Used)
{
    /// <summary>
    /// The share of appearances with at least one use, 0 to 1.
    /// </summary>
    public double Rate => Appearances == 0 ? 0 : (double)Used / Appearances;
}
