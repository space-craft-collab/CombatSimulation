using System.Text.Json.Serialization;
using Battles.Grains.Abstractions;

namespace Battles.Features.GetBattle;

/// <summary>
/// One effect on a monster in a battle, as the HTTP API shows it.
/// </summary>
/// <param name="Kind">What the effect does.</param>
/// <param name="Value">Its strength: hit points per round for a burn, percent for a guard or a slow.</param>
/// <param name="RoundsLeft">The rounds it still lasts, the current one included.</param>
internal sealed record EffectResponse(
    [property: JsonConverter(typeof(JsonStringEnumConverter<StatusEffect>))] StatusEffect Kind,
    int Value,
    int RoundsLeft);
