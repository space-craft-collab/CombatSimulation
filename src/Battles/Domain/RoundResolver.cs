namespace Battles.Domain;

/// <summary>
/// The combat rules of one round, free of grains and storage: the
/// fighters and the round's actions go in, the events and the
/// fighters afterwards come out. The battle grain plays its rounds
/// through it, and so does the balance simulator.
/// </summary>
internal static class RoundResolver
{
    /// <summary>
    /// Plays the actions fastest monster first; ties go to the lower
    /// slot. An action whose actor or target fell earlier in the same
    /// round is dropped.
    /// </summary>
    /// <param name="fighters">Every fighter of the battle, defeated ones included.</param>
    /// <param name="actions">The round's actions, at most one per side.</param>
    /// <returns>The events and the fighters after the round.</returns>
    /// <exception cref="ArgumentException">An action names a slot that is not in the battle.</exception>
    public static RoundResult Resolve(IReadOnlyCollection<Fighter> fighters, IReadOnlyCollection<RoundAction> actions)
    {
        ArgumentNullException.ThrowIfNull(fighters);
        ArgumentNullException.ThrowIfNull(actions);

        var bySlot = fighters.ToDictionary(fighter => fighter.Slot);
        foreach (var action in actions)
        {
            if (!bySlot.ContainsKey(action.ActorSlot) || !bySlot.ContainsKey(action.TargetSlot))
            {
                throw new ArgumentException(
                    $"Action {action.ActorSlot} -> {action.TargetSlot} names a slot that is not in the battle.",
                    nameof(actions));
            }
        }

        // The order is fixed by the stats the round began with.
        var ordered = actions
            .OrderByDescending(action => bySlot[action.ActorSlot].Speed)
            .ThenBy(action => action.ActorSlot)
            .ToList();

        var events = new List<RoundEvent>();
        foreach (var action in ordered)
        {
            var actor = bySlot[action.ActorSlot];
            var target = bySlot[action.TargetSlot];
            if (actor.IsDefeated || target.IsDefeated)
            {
                continue;
            }

            var damage = Math.Min(target.HitPoints, DamageRule.Calculate(actor.Attack, target.Defense));
            var hit = target with { HitPoints = target.HitPoints - damage };
            bySlot[hit.Slot] = hit;
            events.Add(new RoundEvent(actor.Slot, hit.Slot, action.AbilityId, damage, hit.HitPoints));
        }

        return new RoundResult([.. bySlot.Values.OrderBy(fighter => fighter.Slot)], events);
    }
}
