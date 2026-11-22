namespace Battles.Features.GetBattle;

/// <summary>
/// One ability of a monster in a battle and when it is ready, as
/// the HTTP API shows it. The ability's name and numbers are in
/// <c>GET /species</c>.
/// </summary>
/// <param name="AbilityId">The Catalog ability, as an action names it.</param>
/// <param name="RoundsLeft">The rounds until the monster can use it again; 0 when ready.</param>
internal sealed record AbilityCooldownResponse(string AbilityId, int RoundsLeft);
