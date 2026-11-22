namespace Battles.Features.GetBattle;

/// <summary>
/// One monster slot as the HTTP API shows it.
/// </summary>
/// <param name="Slot">The slot, numbered across the whole battle.</param>
/// <param name="ParticipantId">The side fielding the monster.</param>
/// <param name="IsBot">Whether that side is a bot.</param>
/// <param name="SpeciesId">The Catalog species.</param>
/// <param name="HitPoints">Remaining hit points.</param>
/// <param name="MaxHitPoints">Hit points at full health.</param>
/// <param name="Abilities">The monster's abilities in their order, each with its cooldown; empty once the
/// battle is over.</param>
internal sealed record CombatantResponse(
    int Slot,
    string ParticipantId,
    bool IsBot,
    string SpeciesId,
    int HitPoints,
    int MaxHitPoints,
    IReadOnlyList<AbilityCooldownResponse> Abilities);
