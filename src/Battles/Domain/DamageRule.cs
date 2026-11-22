namespace Battles.Domain;

/// <summary>
/// How much damage one hit deals.
/// </summary>
internal static class DamageRule
{
    /// <summary>
    /// The defense that halves a hit. Defense scales damage by
    /// <c>DefenseScale / (DefenseScale + defense)</c>, so every
    /// point helps a little less than the one before and no amount
    /// of it stops a hit completely.
    /// </summary>
    public const int DefenseScale = 10;

    /// <summary>
    /// The share of its damage an attack deals with the element
    /// advantage, in percent.
    /// </summary>
    public const int StrongPercent = 150;

    /// <summary>
    /// The share of its damage an attack deals against the element
    /// advantage, in percent.
    /// </summary>
    public const int WeakPercent = 75;

    /// <summary>
    /// Attack times the ability's power, scaled down by the target's
    /// defense and guard and up or down by the element. Rounded to the nearest
    /// whole point, halves up, and never less than 1 so every hit
    /// counts. The arithmetic is done in whole numbers, so the same
    /// hit gives the same damage everywhere.
    /// </summary>
    /// <param name="attack">The attacker's attack.</param>
    /// <param name="defense">The target's defense.</param>
    /// <param name="power">The ability's power in percent of the attack; 100 for the basic attack.</param>
    /// <param name="effectiveness">How the attack's element stands against the target's.</param>
    /// <param name="guard">The share of the hit the target's guard takes off, in percent.</param>
    /// <returns>The damage of the hit.</returns>
    public static int Calculate(
        int attack,
        int defense,
        int power = 100,
        Effectiveness effectiveness = Effectiveness.Normal,
        int guard = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(attack);
        ArgumentOutOfRangeException.ThrowIfNegative(defense);
        ArgumentOutOfRangeException.ThrowIfNegative(power);
        ArgumentOutOfRangeException.ThrowIfNegative(guard);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(guard, 100);

        var numerator = (long)attack * power * Percent(effectiveness) * (100 - guard) * DefenseScale;
        var denominator = 100L * 100 * 100 * (DefenseScale + defense);

        return (int)Math.Max(1, ((2 * numerator) + denominator) / (2 * denominator));
    }

    private static int Percent(Effectiveness effectiveness) =>
        effectiveness switch
        {
            Effectiveness.Strong => StrongPercent,
            Effectiveness.Weak => WeakPercent,
            _ => 100,
        };
}
