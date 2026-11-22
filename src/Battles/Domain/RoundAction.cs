namespace Battles.Domain;

/// <summary>
/// One side's move for a round: which of its monsters uses which
/// ability on which monster.
/// </summary>
/// <param name="ParticipantId">The side that acts.</param>
/// <param name="ActorSlot">The slot of the acting monster.</param>
/// <param name="TargetSlot">The slot of the targeted monster.</param>
/// <param name="AbilityId">The ability being used.</param>
internal sealed record RoundAction(string ParticipantId, int ActorSlot, int TargetSlot, string AbilityId);
