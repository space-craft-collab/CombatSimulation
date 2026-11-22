namespace Battles.Grains.Abstractions;

/// <summary>
/// One ability of a monster in a battle and when it is ready.
/// </summary>
/// <param name="AbilityId">The Catalog ability.</param>
/// <param name="RoundsLeft">The rounds until the monster can use it again; 0 when ready.</param>
[GenerateSerializer]
[Alias("battles.AbilityCooldown")]
public sealed record AbilityCooldown(
    [property: Id(0)] string AbilityId,
    [property: Id(1)] int RoundsLeft);
