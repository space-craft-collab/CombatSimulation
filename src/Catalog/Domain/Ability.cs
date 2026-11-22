using Catalog.Contracts;

namespace Catalog.Domain;

/// <summary>
/// An ability and its numbers, as stored in the Catalog. Which
/// species know it is a <see cref="SpeciesAbility"/>.
/// </summary>
/// <param name="Id">The ability id, e.g. <c>"flame-dash"</c>.</param>
/// <param name="Name">The display name.</param>
/// <param name="Element">The element the ability strikes with.</param>
/// <param name="Power">The damage in percent of the user's attack; 0 for an ability that deals none.</param>
/// <param name="Cooldown">The rounds the ability cannot be used in after the round it was used in.</param>
/// <param name="Effect">What the ability does besides its damage.</param>
/// <param name="EffectValue">The effect's strength: hit points per round for a burn, percent for the others.</param>
/// <param name="EffectRounds">The rounds the effect lasts, the round it is applied in included; 0 for a heal.</param>
internal sealed record Ability(
    string Id,
    string Name,
    Element Element,
    int Power,
    int Cooldown,
    AbilityEffect Effect = AbilityEffect.None,
    int EffectValue = 0,
    int EffectRounds = 0);
