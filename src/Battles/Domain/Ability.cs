namespace Battles.Domain;

/// <summary>
/// Ability ids. Abilities are not modelled yet; every monster has
/// only the basic attack.
/// </summary>
internal static class Ability
{
    /// <summary>
    /// The basic attack every monster knows.
    /// </summary>
    public const string Strike = "strike";

    /// <summary>
    /// Whether the id names an ability the round loop can resolve.
    /// </summary>
    public static bool IsKnown(string? abilityId) => abilityId == Strike;
}
