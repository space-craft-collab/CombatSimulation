namespace Catalog.Contracts;

/// <summary>
/// An ability a species knows.
/// </summary>
/// <param name="Id">The ability id, e.g. <c>"flame-dash"</c>.</param>
/// <param name="Name">The display name.</param>
/// <param name="Element">The element the ability strikes with.</param>
/// <param name="Power">The damage in percent of the user's attack; 0 for an ability that deals none.</param>
/// <param name="Cooldown">The rounds the ability cannot be used in after the round it was used in.</param>
/// <param name="Effect">What the ability does besides its damage.</param>
/// <param name="EffectValue">The effect's strength: hit points per round for a burn, percent for the others.</param>
/// <param name="EffectRounds">The rounds the effect lasts, the round it is applied in included; 0 for a heal.</param>
public sealed record AbilityDto(
    string Id,
    string Name,
    Element Element,
    int Power,
    int Cooldown,
    AbilityEffect Effect,
    int EffectValue,
    int EffectRounds)
{
    /// <summary>
    /// Whether a monster uses the ability on itself instead of on an
    /// opponent: a guard or a heal.
    /// </summary>
    public bool TargetsSelf => Effect is AbilityEffect.Guard or AbilityEffect.Heal;
}
