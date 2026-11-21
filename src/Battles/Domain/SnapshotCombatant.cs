namespace Battles.Domain;

/// <summary>
/// One monster slot of a <see cref="BattleSnapshot"/> as it stood
/// when the battle ended.
/// </summary>
/// <param name="Slot">The slot, numbered across the whole battle.</param>
/// <param name="ParticipantId">The side that fielded the monster.</param>
/// <param name="IsBot">Whether that side is a bot.</param>
/// <param name="SpeciesId">The Catalog species.</param>
/// <param name="HitPoints">Hit points left at the end.</param>
/// <param name="MaxHitPoints">Hit points at full health.</param>
internal sealed record SnapshotCombatant(
    int Slot,
    string ParticipantId,
    bool IsBot,
    string SpeciesId,
    int HitPoints,
    int MaxHitPoints);
