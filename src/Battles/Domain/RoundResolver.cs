using Catalog.Contracts;

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
    /// Plays a round in three steps. First the actions, fastest
    /// monster first (a slow counts, ties go to the lower slot); an
    /// action whose actor or target fell earlier in the round is
    /// dropped and its cooldown is not spent. Then every burn takes
    /// its hit points, in slot order. Last, effects and cooldowns run
    /// down by one round. Once only one side is left standing the
    /// battle is over and no further burn is dealt.
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
        var events = new List<RoundEvent>();

        // The order is fixed by the speeds the round began with.
        var ordered = actions
            .OrderByDescending(action => bySlot[action.ActorSlot].EffectiveSpeed)
            .ThenBy(action => action.ActorSlot);

        foreach (var action in ordered)
        {
            if (bySlot[action.ActorSlot].IsDefeated || bySlot[action.TargetSlot].IsDefeated)
            {
                continue;
            }

            events.Add(Act(bySlot, action));
        }

        foreach (var slot in bySlot.Keys.Order().ToList())
        {
            if (IsDecided(bySlot.Values))
            {
                break;
            }

            if (Burn(bySlot, slot) is { } burned)
            {
                events.Add(burned);
            }
        }

        return new RoundResult([.. bySlot.Values.OrderBy(fighter => fighter.Slot).Select(EndRound)], events);
    }

    /// <summary>
    /// Whether the battle is over: at most one side has a monster
    /// standing.
    /// </summary>
    /// <param name="fighters">Every fighter of the battle.</param>
    /// <returns><see langword="true"/> if no two sides are left to fight.</returns>
    public static bool IsDecided(IEnumerable<Fighter> fighters)
    {
        ArgumentNullException.ThrowIfNull(fighters);

        return fighters
            .Where(fighter => !fighter.IsDefeated)
            .Select(fighter => fighter.ParticipantId)
            .Distinct()
            .Take(2)
            .Count() < 2;
    }

    private static RoundEvent Act(Dictionary<int, Fighter> bySlot, RoundAction action)
    {
        var actor = bySlot[action.ActorSlot];
        var ability = actor.Find(action.AbilityId)!.Spec;
        bySlot[actor.Slot] = actor = Spend(actor, ability);

        var roundEvent = ability.Effect switch
        {
            AbilityEffect.Heal => Heal(actor, ability),
            AbilityEffect.Guard => Guard(actor, ability),
            _ => Hit(actor, bySlot[action.TargetSlot], ability),
        };
        bySlot[roundEvent.Target.Slot] = roundEvent.Target;

        return roundEvent.Event;
    }

    private static (Fighter Target, RoundEvent Event) Hit(Fighter actor, Fighter target, AbilitySpec ability)
    {
        var effectiveness = ElementChart.Of(ability.Element, target.Element);
        var damage = Math.Min(
            target.HitPoints,
            DamageRule.Calculate(actor.Attack, target.Defense, ability.Power, effectiveness, target.GuardPercent));
        var hit = target with { HitPoints = target.HitPoints - damage };

        // A burn or a slow rides on the hit, if the target survives it.
        var takesHold = ability.Effect is AbilityEffect.Burn or AbilityEffect.Slow && !hit.IsDefeated;
        if (takesHold)
        {
            hit = hit.With(new ActiveEffect(ability.Effect, ability.EffectValue, ability.EffectRounds));
        }

        return (hit, new RoundEvent
        {
            Kind = RoundEventKind.Ability,
            ActorSlot = actor.Slot,
            TargetSlot = hit.Slot,
            AbilityId = ability.Id,
            Damage = damage,
            TargetHitPoints = hit.HitPoints,
            Effectiveness = effectiveness,
            Effect = takesHold ? ability.Effect : AbilityEffect.None,
        });
    }

    private static (Fighter Target, RoundEvent Event) Heal(Fighter actor, AbilitySpec ability)
    {
        // A share of full health, halves up, but never above it.
        var share = ((2 * actor.MaxHitPoints * ability.EffectValue) + 100) / 200;
        var healing = Math.Clamp(share, 0, actor.MaxHitPoints - actor.HitPoints);
        var healed = actor with { HitPoints = actor.HitPoints + healing };

        return (healed, new RoundEvent
        {
            Kind = RoundEventKind.Ability,
            ActorSlot = actor.Slot,
            TargetSlot = actor.Slot,
            AbilityId = ability.Id,
            Healing = healing,
            TargetHitPoints = healed.HitPoints,
            Effect = AbilityEffect.Heal,
        });
    }

    private static (Fighter Target, RoundEvent Event) Guard(Fighter actor, AbilitySpec ability) =>
        (actor.With(new ActiveEffect(AbilityEffect.Guard, ability.EffectValue, ability.EffectRounds)), new RoundEvent
        {
            Kind = RoundEventKind.Ability,
            ActorSlot = actor.Slot,
            TargetSlot = actor.Slot,
            AbilityId = ability.Id,
            TargetHitPoints = actor.HitPoints,
            Effect = AbilityEffect.Guard,
        });

    // A guard does not help against a burn; it only blunts hits.
    private static RoundEvent? Burn(Dictionary<int, Fighter> bySlot, int slot)
    {
        var fighter = bySlot[slot];
        if (fighter.IsDefeated || fighter.Effects.FirstOrDefault(effect => effect.Kind == AbilityEffect.Burn) is not { } burn)
        {
            return null;
        }

        var damage = Math.Min(fighter.HitPoints, burn.Value);
        bySlot[slot] = fighter with { HitPoints = fighter.HitPoints - damage };

        return new RoundEvent
        {
            Kind = RoundEventKind.Burn,
            ActorSlot = slot,
            TargetSlot = slot,
            Damage = damage,
            TargetHitPoints = fighter.HitPoints - damage,
            Effect = AbilityEffect.Burn,
        };
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

    // Nothing to run down leaves the fighter as it is, so an idle
    // round returns the very fighters it was given.
    private static Fighter EndRound(Fighter fighter)
    {
        // A fallen monster carries no effects any more.
        if (fighter.Effects.Count > 0)
        {
            IReadOnlyList<ActiveEffect> remaining = fighter.IsDefeated
                ? []
                :
                [
                    .. fighter.Effects
                        .Where(effect => effect.RoundsLeft > 1)
                        .Select(effect => effect with { RoundsLeft = effect.RoundsLeft - 1 }),
                ];
            fighter = fighter with { Effects = remaining };
        }

        return fighter.Abilities.All(ability => ability.IsReady)
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
}
