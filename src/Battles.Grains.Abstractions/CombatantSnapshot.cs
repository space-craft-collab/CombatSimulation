namespace Battles.Grains.Abstractions;

/// <summary>
/// Point-in-time view of one monster slot in a battle — what a
/// client needs to pick an actor and a target.
/// </summary>
/// <param name="Slot">The slot, numbered across the whole battle.</param>
/// <param name="ParticipantId">The side fielding the monster.</param>
/// <param name="IsBot">Whether that side is a bot.</param>
/// <param name="SpeciesId">The Catalog species.</param>
/// <param name="HitPoints">Remaining hit points.</param>
/// <param name="MaxHitPoints">Hit points at full health.</param>
[GenerateSerializer]
public sealed record CombatantSnapshot(
    [property: Id(0)] int Slot,
    [property: Id(1)] string ParticipantId,
    [property: Id(2)] bool IsBot,
    [property: Id(3)] string SpeciesId,
    [property: Id(4)] int HitPoints,
    [property: Id(5)] int MaxHitPoints);
