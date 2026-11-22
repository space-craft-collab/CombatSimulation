namespace Battles.Grains.Abstractions;

/// <summary>
/// An effect that lies on a monster for some rounds (ADR-0012).
/// </summary>
public enum StatusEffect
{
    /// <summary>
    /// The monster loses hit points at the end of each round.
    /// </summary>
    Burn = 1,

    /// <summary>
    /// The monster takes less damage from hits.
    /// </summary>
    Guard = 2,

    /// <summary>
    /// The monster's speed is reduced, so it acts later in a round.
    /// </summary>
    Slow = 3,
}
