namespace Battles.Features.WatchBattle;

/// <summary>
/// One action that landed during a round, in resolution order.
/// </summary>
/// <param name="ActorSlot">The slot of the monster that acted.</param>
/// <param name="TargetSlot">The slot of the monster that was hit.</param>
/// <param name="AbilityId">The ability that was used.</param>
/// <param name="Damage">The hit points the target actually lost.</param>
/// <param name="TargetHitPoints">The target's hit points afterwards.</param>
internal sealed record StrikeDto(
    int ActorSlot,
    int TargetSlot,
    string AbilityId,
    int Damage,
    int TargetHitPoints);
