namespace Catalog.Contracts;

/// <summary>
/// What an ability does besides dealing damage. The numbers are
/// stored in grain state and must not change.
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
