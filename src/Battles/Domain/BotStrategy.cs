namespace Battles.Domain;

/// <summary>
/// The rule-based bot from ADR-0007: its strongest monster attacks
/// the weakest opponent with the hardest-hitting ability it has
/// ready. Phase 8 swaps this rule for a Claude call.
/// </summary>
internal static class BotStrategy
{
    /// <summary>
    /// Picks the bot's move. Ties go to the lowest slot and to the
    /// earlier ability, so the choice is deterministic.
    /// </summary>
    public static BotMove Decide(string botId, IReadOnlyCollection<Fighter> fighters)
    {
        ArgumentNullException.ThrowIfNull(botId);
        ArgumentNullException.ThrowIfNull(fighters);

        var standing = fighters.Where(fighter => fighter.HitPoints > 0).ToList();

        var actor = standing
            .Where(fighter => fighter.ParticipantId == botId)
            .OrderByDescending(fighter => fighter.Attack)
            .ThenBy(fighter => fighter.Slot)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"Bot '{botId}' has no standing monster.");

        var target = standing
            .Where(fighter => fighter.ParticipantId != botId)
            .OrderBy(fighter => fighter.HitPoints)
            .ThenBy(fighter => fighter.Slot)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"Bot '{botId}' has no standing opponent.");

        // MaxBy keeps the first of several equal abilities; the basic
        // attack is always ready, so there is always one.
        var ability = actor.Abilities
            .Where(ability => ability.IsReady && !ability.Spec.TargetsSelf)
            .MaxBy(ability => DamageRule.Calculate(
                actor.Attack,
                target.Defense,
                ability.Spec.Power,
                ElementChart.Of(ability.Spec.Element, target.Element)))
            ?? throw new InvalidOperationException($"The monster in slot {actor.Slot} has no attack ready.");

        return new BotMove(actor.Slot, target.Slot, ability.Spec.Id);
    }
}
