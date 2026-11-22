namespace Catalog.Contracts;

/// <summary>
/// What an ability does besides dealing damage. A battle copies
/// its monsters' abilities into its grain state, so the numbers are
/// stored format and must not change (CONTRIBUTING.md); the Catalog
/// stores the names in SQL, so a rename needs a migration.
/// </summary>
public enum AbilityEffect
{
    /// <summary>
    /// Nothing; the ability only deals its damage.
    /// </summary>
    None = 0,

    /// <summary>
    /// The target loses hit points at the end of each round.
    /// </summary>
    Burn = 1,

    /// <summary>
    /// The monster using it takes less damage from hits.
    /// </summary>
    Guard = 2,

    /// <summary>
    /// The target's speed drops, so it acts later in a round.
    /// </summary>
    Slow = 3,

    /// <summary>
    /// The monster using it restores hit points at once.
    /// </summary>
    Heal = 4,
}
