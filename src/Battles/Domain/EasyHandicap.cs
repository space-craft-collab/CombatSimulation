namespace Battles.Domain;

/// <summary>
/// The handicap of the Easy difficulty (ADR-0013): the bot's
/// monsters enter the battle with less hit points and attack than
/// their species. It is applied once, as a monster enters; the
/// combat rules never see a difficulty. Easy lies outside the
/// balance guard rails on purpose, which measure the rules on the
/// species' own numbers.
/// </summary>
internal static class EasyHandicap
{
    /// <summary>
    /// The share of its species' full hit points a weakened monster
    /// has, in percent.
    /// </summary>
    public const int MaxHitPointsPercent = 75;

    /// <summary>
    /// The share of its species' attack a weakened monster has, in
    /// percent.
    /// </summary>
    public const int AttackPercent = 75;

    /// <summary>
    /// Weakens a monster as it enters the battle: full hit points and
    /// attack scaled down, at full health. Defense, speed, element
    /// and abilities stay as they are.
    /// </summary>
    /// <param name="fighter">The fresh monster, at full health.</param>
    /// <returns>The weakened monster.</returns>
    public static Fighter Weaken(Fighter fighter)
    {
        ArgumentNullException.ThrowIfNull(fighter);

        var maxHitPoints = Scale(fighter.MaxHitPoints, MaxHitPointsPercent);

        return fighter with
        {
            MaxHitPoints = maxHitPoints,
            HitPoints = maxHitPoints,
            Attack = Scale(fighter.Attack, AttackPercent),
        };
    }

    /// <summary>
    /// Scales a stat to a share of itself in whole numbers, rounded
    /// half up. A positive stat never drops below 1; zero stays zero.
    /// </summary>
    /// <param name="value">The stat; not negative.</param>
    /// <param name="percent">The share to keep, in percent; not negative.</param>
    /// <returns>The scaled stat.</returns>
    public static int Scale(int value, int percent)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfNegative(percent);

        var scaled = (value * percent + 50) / 100;

        return value == 0 ? 0 : Math.Max(1, scaled);
    }
}
