using Battles.Domain;
using Catalog.Contracts;

namespace Battles.Grains;

/// <summary>
/// One effect on a monster slot as the battle stores it.
/// </summary>
[GenerateSerializer]
[Alias("battles.CombatantEffect")]
internal sealed class CombatantEffect
{
    [Id(0)]
    public required AbilityEffect Kind { get; init; }

    [Id(1)]
    public required int Value { get; init; }

    [Id(2)]
    public required int RoundsLeft { get; init; }

    /// <summary>
    /// Stores an effect of a fighter.
    /// </summary>
    public static CombatantEffect From(ActiveEffect effect) =>
        new() { Kind = effect.Kind, Value = effect.Value, RoundsLeft = effect.RoundsLeft };

    /// <summary>
    /// The effect as the combat rules see it.
    /// </summary>
    public ActiveEffect ToActiveEffect() => new(Kind, Value, RoundsLeft);
}
