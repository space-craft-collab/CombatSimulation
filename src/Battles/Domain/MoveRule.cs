namespace Battles.Domain;

/// <summary>
/// Which moves a side may make in a round.
/// </summary>
internal static class MoveRule
{
    /// <summary>
    /// Checks a move against the battle as it stands: the actor is a
    /// standing monster of the side, it knows the ability and the
    /// ability is ready, and the target fits the ability — the actor
    /// itself for a guard or a heal, a standing opponent otherwise.
    /// </summary>
    /// <param name="fighters">Every fighter of the battle.</param>
    /// <param name="action">The move to check.</param>
    /// <returns>Why the move is not allowed, or <see langword="null"/> if it is.</returns>
    public static string? Reject(IReadOnlyCollection<Fighter> fighters, RoundAction action)
    {
        ArgumentNullException.ThrowIfNull(fighters);
        ArgumentNullException.ThrowIfNull(action);

        var actor = FindStanding(fighters, action.ActorSlot);
        if (actor?.ParticipantId != action.ParticipantId)
        {
            return $"Slot {action.ActorSlot} is not a standing monster of '{action.ParticipantId}'.";
        }

        if (actor.Find(action.AbilityId) is not { } ability)
        {
            return $"The monster in slot {action.ActorSlot} does not know the ability '{action.AbilityId}'.";
        }

        if (!ability.IsReady)
        {
            return $"'{action.AbilityId}' is ready again in {ability.CooldownLeft} round(s).";
        }

        if (ability.Spec.TargetsSelf)
        {
            return action.TargetSlot == action.ActorSlot
                ? null
                : $"'{action.AbilityId}' is used by a monster on itself; the target must be slot {action.ActorSlot}.";
        }

        var target = FindStanding(fighters, action.TargetSlot);

        return target is null || target.ParticipantId == action.ParticipantId
            ? $"Slot {action.TargetSlot} is not a standing opponent."
            : null;
    }

    /// <summary>
    /// Every move a side may make, in a fixed order: by actor slot,
    /// then by the actor's abilities, then by target slot.
    /// </summary>
    /// <param name="participantId">The side to move.</param>
    /// <param name="fighters">Every fighter of the battle.</param>
    /// <returns>The legal moves; empty if the side has no standing monster or opponent.</returns>
    public static List<RoundAction> LegalMoves(string participantId, IReadOnlyCollection<Fighter> fighters)
    {
        ArgumentNullException.ThrowIfNull(participantId);
        ArgumentNullException.ThrowIfNull(fighters);

        var standing = fighters.Where(fighter => !fighter.IsDefeated).OrderBy(fighter => fighter.Slot).ToList();
        var opponents = standing.Where(fighter => fighter.ParticipantId != participantId).ToList();
        var moves = new List<RoundAction>();

        foreach (var actor in standing.Where(fighter => fighter.ParticipantId == participantId))
        {
            foreach (var ability in actor.Abilities.Where(ability => ability.IsReady))
            {
                if (ability.Spec.TargetsSelf)
                {
                    moves.Add(new RoundAction(participantId, actor.Slot, actor.Slot, ability.Spec.Id));
                    continue;
                }

                moves.AddRange(opponents.Select(
                    target => new RoundAction(participantId, actor.Slot, target.Slot, ability.Spec.Id)));
            }
        }

        return opponents.Count == 0 ? [] : moves;
    }

    private static Fighter? FindStanding(IReadOnlyCollection<Fighter> fighters, int slot) =>
        fighters.FirstOrDefault(fighter => fighter.Slot == slot && !fighter.IsDefeated);
}
