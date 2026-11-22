namespace Battles.Grains.Abstractions;

/// <summary>
/// One effect on a monster in a battle.
/// </summary>
/// <param name="Kind">What the effect does.</param>
/// <param name="Value">Its strength: hit points per round for a burn, percent for a guard or a slow.</param>
/// <param name="RoundsLeft">The rounds it still lasts, the current one included.</param>
[GenerateSerializer]
[Alias("battles.ActiveEffectSnapshot")]
public sealed record ActiveEffectSnapshot(
    [property: Id(0)] StatusEffect Kind,
    [property: Id(1)] int Value,
    [property: Id(2)] int RoundsLeft);
