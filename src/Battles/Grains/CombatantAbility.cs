using Battles.Domain;
using Catalog.Contracts;

namespace Battles.Grains;

/// <summary>
/// One ability of a monster slot as the battle stores it: the
/// numbers copied from the Catalog when the battle started, so a
/// running battle never changes under a Catalog edit, and the
/// cooldown.
/// </summary>
[GenerateSerializer]
[Alias("battles.CombatantAbility")]
internal sealed class CombatantAbility
{
    [Id(0)]
    public required string AbilityId { get; init; }

    [Id(1)]
    public required Element Element { get; init; }

    [Id(2)]
    public required int Power { get; init; }

    [Id(3)]
    public required int Cooldown { get; init; }

    [Id(4)]
    public required AbilityEffect Effect { get; init; }

    [Id(5)]
    public required int EffectValue { get; init; }

    [Id(6)]
    public required int EffectRounds { get; init; }

    /// <summary>
    /// The rounds until the monster can use the ability again.
    /// </summary>
    [Id(7)]
    public int CooldownLeft { get; set; }

    /// <summary>
    /// Stores an ability of a fighter.
    /// </summary>
    public static CombatantAbility From(FighterAbility ability) =>
        new()
        {
            AbilityId = ability.Spec.Id,
            Element = ability.Spec.Element,
            Power = ability.Spec.Power,
            Cooldown = ability.Spec.Cooldown,
            Effect = ability.Spec.Effect,
            EffectValue = ability.Spec.EffectValue,
            EffectRounds = ability.Spec.EffectRounds,
            CooldownLeft = ability.CooldownLeft,
        };

    /// <summary>
    /// The ability as the combat rules see it.
    /// </summary>
    public FighterAbility ToFighterAbility() =>
        new(new AbilitySpec(AbilityId, Element, Power, Cooldown, Effect, EffectValue, EffectRounds), CooldownLeft);
}
