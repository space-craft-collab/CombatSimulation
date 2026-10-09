namespace Battles.Grains.Abstractions;

/// <summary>
/// One participant's move for the current round.
/// </summary>
/// <param name="ParticipantId">The participant submitting the action.</param>
/// <param name="ActorSlot">The slot of the acting monster.</param>
/// <param name="TargetSlot">The slot of the targeted monster.</param>
/// <param name="AbilityId">The Catalog ability being used.</param>
[GenerateSerializer]
public sealed record BattleAction(
    [property: Id(0)] string ParticipantId,
    [property: Id(1)] int ActorSlot,
    [property: Id(2)] int TargetSlot,
    [property: Id(3)] string AbilityId);
