namespace Battles.Domain;

/// <summary>
/// One monster in a battle as a decision rule sees it: owner, attack
/// and current hit points. Zero hit points means defeated.
/// </summary>
internal sealed record Fighter(int Slot, string ParticipantId, int Attack, int HitPoints);
