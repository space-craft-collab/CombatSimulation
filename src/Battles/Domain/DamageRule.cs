namespace Battles.Domain;

/// <summary>
/// How much damage one hit deals.
/// </summary>
internal static class DamageRule
{
    /// <summary>
    /// Attack minus defense, but never less than 1 so every hit counts.
    /// </summary>
    public static int Calculate(int attack, int defense) => Math.Max(1, attack - defense);
}
