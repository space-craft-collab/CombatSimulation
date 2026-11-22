namespace Battles.Domain;

/// <summary>
/// One thing that happened while a round resolved: an action that
/// landed and what it did to its target.
/// </summary>
/// <param name="ActorSlot">The slot of the monster that acted.</param>
/// <param name="TargetSlot">The slot of the monster that was hit.</param>
/// <param name="AbilityId">The ability that was used.</param>
/// <param name="Damage">The hit points the target actually lost.</param>
/// <param name="TargetHitPoints">The target's hit points afterwards.</param>
internal sealed record RoundEvent(
    int ActorSlot,
    int TargetSlot,
    string AbilityId,
    int Damage,
    int TargetHitPoints);
