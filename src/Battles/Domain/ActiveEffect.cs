using Catalog.Contracts;

namespace Battles.Domain;

/// <summary>
/// An effect that lies on a monster for some rounds: a burn, a
/// guard or a slow.
/// </summary>
/// <param name="Kind">What the effect does.</param>
/// <param name="Value">Its strength: hit points per round for a burn, percent for a guard or a slow.</param>
/// <param name="RoundsLeft">The rounds it still lasts, the current one included.</param>
internal sealed record ActiveEffect(AbilityEffect Kind, int Value, int RoundsLeft);
