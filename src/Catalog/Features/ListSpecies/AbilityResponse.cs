using System.Text.Json.Serialization;
using Catalog.Contracts;

namespace Catalog.Features.ListSpecies;

/// <summary>
/// One ability of a species in <c>GET /species</c>.
/// </summary>
/// <param name="Id">The ability id, as an action names it.</param>
/// <param name="Name">The display name.</param>
/// <param name="Element">The element the ability strikes with.</param>
/// <param name="Power">The damage in percent of the user's attack; 0 for an ability that deals none.</param>
/// <param name="Cooldown">The rounds the ability cannot be used in after the round it was used in.</param>
/// <param name="Effect">What the ability does besides its damage.</param>
/// <param name="EffectValue">The effect's strength: hit points per round for a burn, percent for the others.</param>
/// <param name="EffectRounds">The rounds the effect lasts, the round it is applied in included; 0 for a heal.</param>
/// <param name="TargetsSelf">Whether a monster uses the ability on itself instead of on an opponent.</param>
internal sealed record AbilityResponse(
    string Id,
    string Name,
    [property: JsonConverter(typeof(JsonStringEnumConverter<Element>))] Element Element,
    int Power,
    int Cooldown,
    [property: JsonConverter(typeof(JsonStringEnumConverter<AbilityEffect>))] AbilityEffect Effect,
    int EffectValue,
    int EffectRounds,
    bool TargetsSelf);
