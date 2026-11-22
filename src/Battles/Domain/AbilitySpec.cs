using Catalog.Contracts;

namespace Battles.Domain;

/// <summary>
/// An ability's numbers as the combat rules use them, copied from
/// the Catalog when a monster enters a battle.
/// </summary>
/// <param name="Id">The ability id.</param>
/// <param name="Element">The element the ability strikes with.</param>
/// <param name="Power">The damage in percent of the user's attack; 0 for an ability that deals none.</param>
/// <param name="Cooldown">The rounds the ability cannot be used in after the round it was used in.</param>
/// <param name="Effect">What the ability does besides its damage.</param>
/// <param name="EffectValue">The effect's strength: hit points per round for a burn, percent for the others.</param>
/// <param name="EffectRounds">The rounds the effect lasts, the round it is applied in included.</param>
internal sealed record AbilitySpec(
    string Id,
    Element Element,
    int Power,
    int Cooldown,
    AbilityEffect Effect = AbilityEffect.None,
    int EffectValue = 0,
    int EffectRounds = 0)
{
    /// <summary>
    /// The id of the basic attack every monster knows.
    /// </summary>
    public const string StrikeId = "strike";

    /// <summary>
    /// The basic attack: neutral, full attack, always ready.
    /// </summary>
    public static AbilitySpec Strike { get; } = new(StrikeId, Element.Neutral, 100, 0);

    /// <summary>
    /// Whether a monster uses the ability on itself instead of on an
    /// opponent.
    /// </summary>
    public bool TargetsSelf => Effect is AbilityEffect.Guard or AbilityEffect.Heal;

    /// <summary>
    /// Copies an ability from the Catalog.
    /// </summary>
    /// <param name="ability">The Catalog's ability.</param>
    /// <returns>The ability as the rules use it.</returns>
    public static AbilitySpec From(AbilityDto ability)
    {
        ArgumentNullException.ThrowIfNull(ability);

        return new AbilitySpec(
            ability.Id,
            ability.Element,
            ability.Power,
            ability.Cooldown,
            ability.Effect,
            ability.EffectValue,
            ability.EffectRounds);
    }
}
