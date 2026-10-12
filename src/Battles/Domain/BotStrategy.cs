namespace Battles.Domain;

/// <summary>
/// The rule-based bot from ADR-0007: its strongest monster attacks
/// the weakest opponent. Phase 8 swaps this rule for a Claude call.
/// </summary>
internal static class BotStrategy
{
    /// <summary>
    /// Picks the bot's move. Ties go to the lowest slot, so the
    /// choice is deterministic.
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

        return new BotMove(actor.Slot, target.Slot);
    }
}
