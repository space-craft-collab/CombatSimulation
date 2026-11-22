namespace Battles.Domain;

/// <summary>
/// The combat rules of one round (ADR-0012), free of grains and
/// storage: the fighters and the round's actions go in, the events
/// and the fighters afterwards come out. The battle grain plays its
/// rounds through it, and so does the balance simulator.
/// </summary>
internal static class RoundResolver
{
    /// <summary>
    /// Plays the actions fastest monster first; ties go to the lower
    /// slot. An action whose actor or target fell earlier in the same
    /// round is dropped and its cooldown is not spent. At the end of
    /// the round every cooldown runs down by one.
    /// </summary>
    /// <param name="fighters">Every fighter of the battle, defeated ones included.</param>
    /// <param name="actions">The round's actions, at most one per side.</param>
    /// <returns>The events and the fighters after the round.</returns>
    /// <exception cref="ArgumentException">An action is not a legal move (<see cref="MoveRule"/>).</exception>
    public static RoundResult Resolve(IReadOnlyCollection<Fighter> fighters, IReadOnlyCollection<RoundAction> actions)
    {
        ArgumentNullException.ThrowIfNull(fighters);
        ArgumentNullException.ThrowIfNull(actions);

        foreach (var action in actions)
        {
            if (MoveRule.Reject(fighters, action) is { } reason)
            {
                throw new ArgumentException(reason, nameof(actions));
            }
        }

        var bySlot = fighters.ToDictionary(fighter => fighter.Slot);

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

            var ability = actor.Find(action.AbilityId)!.Spec;
            var effectiveness = ElementChart.Of(ability.Element, target.Element);
            var damage = Math.Min(
                target.HitPoints,
                DamageRule.Calculate(actor.Attack, target.Defense, ability.Power, effectiveness));

            bySlot[actor.Slot] = Spend(actor, ability);
            var hit = target with { HitPoints = target.HitPoints - damage };
            bySlot[hit.Slot] = hit;
            events.Add(new RoundEvent(actor.Slot, hit.Slot, ability.Id, damage, hit.HitPoints, effectiveness));
        }

        return new RoundResult([.. bySlot.Values.OrderBy(fighter => fighter.Slot).Select(EndRound)], events);
    }

    // One more than the cooldown, because the end of this round
    // already takes one off.
    private static Fighter Spend(Fighter actor, AbilitySpec used) =>
        used.Cooldown == 0
            ? actor
            : actor with
            {
                Abilities =
                [
                    .. actor.Abilities.Select(ability =>
                        ability.Spec.Id == used.Id ? ability with { CooldownLeft = used.Cooldown + 1 } : ability),
                ],
            };

    private static Fighter EndRound(Fighter fighter) =>
        fighter.Abilities.All(ability => ability.IsReady)
            ? fighter
            : fighter with
            {
                Abilities =
                [
                    .. fighter.Abilities.Select(ability =>
                        ability.IsReady ? ability : ability with { CooldownLeft = ability.CooldownLeft - 1 }),
                ],
            };
}
