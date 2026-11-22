using Catalog.Contracts;

namespace Battles.Domain;

/// <summary>
/// The rule-based bot from ADR-0007. It rates every legal move by
/// how much of an opponent's threat the move removes, or how much of
/// its own it saves, and makes the best one: it finishes a monster
/// it can fell, uses the element advantage, heals when low and
/// guards when that spares more than a hit would cost the opponent.
/// It looks at the current round only. Phase 8 swaps this rule for a
/// Claude call.
/// </summary>
internal static class BotStrategy
{
    // Felling a monster ends everything it would still have done, so
    // it counts for more than the hit points it had left.
    private const double FinishingBlow = 2.0;

    // Damage a burn deals later is worth less than damage dealt now:
    // the target may fall first, or the battle may end.
    private const double DamageOverTime = 0.75;

    // What it is worth to make a faster opponent act after one of the
    // bot's own monsters, as a share of that opponent's threat.
    private const double Outrun = 0.15;

    /// <summary>
    /// Picks the bot's move. Among equal moves the first one in the
    /// order of <see cref="MoveRule.LegalMoves"/> wins (lowest actor
    /// slot, earliest ability, lowest target slot), so the choice is
    /// deterministic.
    /// </summary>
    /// <param name="botId">The side the bot plays.</param>
    /// <param name="fighters">Every fighter of the battle.</param>
    /// <returns>The move.</returns>
    /// <exception cref="InvalidOperationException">The bot has no standing monster or no standing opponent.</exception>
    public static BotMove Decide(string botId, IReadOnlyCollection<Fighter> fighters)
    {
        ArgumentNullException.ThrowIfNull(botId);
        ArgumentNullException.ThrowIfNull(fighters);

        var bySlot = fighters.ToDictionary(fighter => fighter.Slot);
        var standing = fighters.Where(fighter => !fighter.IsDefeated).ToList();
        var own = standing.Where(fighter => fighter.ParticipantId == botId).ToList();
        var opponents = standing.Where(fighter => fighter.ParticipantId != botId).ToList();

        var best = MoveRule.LegalMoves(botId, fighters)
            .MaxBy(move => Rate(bySlot[move.ActorSlot], bySlot[move.TargetSlot], move.AbilityId, own, opponents))
            ?? throw new InvalidOperationException($"Bot '{botId}' has no standing monster or no standing opponent.");

        return new BotMove(best.ActorSlot, best.TargetSlot, best.AbilityId);
    }

    private static double Rate(
        Fighter actor,
        Fighter target,
        string abilityId,
        List<Fighter> own,
        List<Fighter> opponents)
    {
        var ability = actor.Find(abilityId)!.Spec;

        return ability.Effect switch
        {
            AbilityEffect.Heal => RateHeal(actor, ability),
            AbilityEffect.Guard => RateGuard(actor, ability, own, opponents),
            _ => RateAttack(actor, target, ability, own),
        };
    }

    // The share of the target that the hit removes, weighted by how
    // dangerous the target is.
    private static double RateAttack(Fighter actor, Fighter target, AbilitySpec ability, List<Fighter> own)
    {
        var damage = Math.Min(target.HitPoints, HitOf(actor, target, ability));
        if (damage == target.HitPoints)
        {
            return FinishingBlow * target.Attack;
        }

        var share = (double)damage / target.HitPoints;
        if (ability.Effect == AbilityEffect.Burn && !target.Has(AbilityEffect.Burn))
        {
            var burn = Math.Min(ability.EffectValue * ability.EffectRounds, target.HitPoints - damage);
            share += DamageOverTime * burn / target.HitPoints;
        }

        var rating = share * target.Attack;
        if (ability.Effect == AbilityEffect.Slow && !target.Has(AbilityEffect.Slow))
        {
            var slowed = target.Speed * (100 - ability.EffectValue) / 100;
            if (own.Any(fighter => fighter.EffectiveSpeed < target.EffectiveSpeed && fighter.EffectiveSpeed >= slowed))
            {
                rating += Outrun * target.Attack;
            }
        }

        return rating;
    }

    // What the heal gives back, as a share of what the monster then
    // has: the lower it is, the more a heal counts.
    private static double RateHeal(Fighter actor, AbilitySpec ability)
    {
        var share = ((2 * actor.MaxHitPoints * ability.EffectValue) + 100) / 200;
        var healing = Math.Min(share, actor.MaxHitPoints - actor.HitPoints);

        return (double)healing / (actor.HitPoints + healing) * actor.Attack;
    }

    // What the guard spares: its share of the hardest hit an opponent
    // has ready, for the rounds after this one, if the opponent goes
    // for this monster at all, which gets less likely with every
    // other monster the bot still has.
    private static double RateGuard(Fighter actor, AbilitySpec ability, List<Fighter> own, List<Fighter> opponents)
    {
        if (actor.Has(AbilityEffect.Guard))
        {
            return 0;
        }

        var hardestHit = opponents.Max(opponent => opponent.Abilities
            .Where(ready => ready.IsReady && !ready.Spec.TargetsSelf)
            .Max(ready => HitOf(opponent, actor, ready.Spec)));
        var spared = hardestHit * ability.EffectValue / 100.0 * (ability.EffectRounds - 1) / own.Count;

        return Math.Min(1, spared / actor.HitPoints) * actor.Attack;
    }

    private static int HitOf(Fighter actor, Fighter target, AbilitySpec ability) =>
        DamageRule.Calculate(
            actor.Attack,
            target.Defense,
            ability.Power,
            ElementChart.Of(ability.Element, target.Element),
            target.GuardPercent);
}
