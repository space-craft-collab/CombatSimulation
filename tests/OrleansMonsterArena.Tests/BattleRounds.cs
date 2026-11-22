using Battles.Features.GetBattle;
using Battles.Grains.Abstractions;
using Xunit.Sdk;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// The round loop the battle drivers share. It picks moves that are
/// legal under any rules — the basic attack, from the first standing
/// monster of a side at the first standing opponent — so the tests
/// depend neither on balance numbers nor on how many rounds a battle
/// takes.
/// </summary>
internal static class BattleRounds
{
    /// <summary>
    /// The basic attack every monster knows and may always use.
    /// </summary>
    public const string Strike = "strike";

    /// <summary>
    /// How many rounds a driver plays before it gives up on a
    /// condition; far more than any battle needs, so only a stuck
    /// battle reaches it.
    /// </summary>
    public const int RoundCap = 200;

    /// <summary>
    /// The monsters of one side that are still standing, in slot order.
    /// </summary>
    /// <param name="battle">The battle to look at.</param>
    /// <param name="participantId">The side.</param>
    /// <returns>The side's standing monsters.</returns>
    public static IReadOnlyList<CombatantResponse> StandingOf(BattleResponse battle, string participantId)
    {
        ArgumentNullException.ThrowIfNull(battle);

        return [.. battle.Combatants.Where(combatant =>
            combatant.ParticipantId == participantId && combatant.HitPoints > 0)];
    }

    /// <summary>
    /// The sides taking part, in slot order.
    /// </summary>
    /// <param name="battle">The battle to look at.</param>
    /// <returns>Every participant id once.</returns>
    public static IReadOnlyList<string> ParticipantIds(BattleResponse battle)
    {
        ArgumentNullException.ThrowIfNull(battle);

        return [.. battle.Combatants.Select(combatant => combatant.ParticipantId).Distinct()];
    }

    /// <summary>
    /// The human sides that still have a monster standing, in slot
    /// order: the ones a round waits for.
    /// </summary>
    /// <param name="battle">The battle to look at.</param>
    /// <returns>The human sides still in the fight.</returns>
    public static IReadOnlyList<string> HumanSidesStanding(BattleResponse battle)
    {
        ArgumentNullException.ThrowIfNull(battle);

        return [.. battle.Combatants
            .Where(combatant => !combatant.IsBot && combatant.HitPoints > 0)
            .Select(combatant => combatant.ParticipantId)
            .Distinct()];
    }

    /// <summary>
    /// The strike a side makes: its first standing monster at the
    /// first standing opponent.
    /// </summary>
    /// <param name="battle">The battle as it stands.</param>
    /// <param name="participantId">The side that acts.</param>
    /// <returns>The acting slot and the targeted slot.</returns>
    public static (int ActorSlot, int TargetSlot) ChooseStrike(BattleResponse battle, string participantId)
    {
        ArgumentNullException.ThrowIfNull(battle);

        if (battle.Status != BattleStatus.InProgress)
        {
            throw FailException.ForFailure(
                $"'{participantId}' cannot strike: battle {battle.BattleId} is {battle.Status}.");
        }

        var actor = battle.Combatants.FirstOrDefault(combatant =>
                combatant.ParticipantId == participantId && combatant.HitPoints > 0)
            ?? throw FailException.ForFailure(
                $"'{participantId}' has no standing monster in battle {battle.BattleId}.");
        var target = battle.Combatants.FirstOrDefault(combatant =>
                combatant.ParticipantId != participantId && combatant.HitPoints > 0)
            ?? throw FailException.ForFailure(
                $"'{participantId}' has no standing opponent in battle {battle.BattleId}.");

        return (actor.Slot, target.Slot);
    }

    /// <summary>
    /// Plays rounds until the condition holds: every round, each human
    /// side still standing strikes once, so no round waits for a
    /// deadline. Fails the test if the battle ends first, if only bots
    /// are left to play it, or if <see cref="RoundCap"/> rounds pass.
    /// </summary>
    /// <param name="read">Reads the battle as it stands.</param>
    /// <param name="strike">Lets the given sides strike once each, given the battle as it stands.</param>
    /// <param name="condition">When to stop.</param>
    /// <returns>The first state that meets the condition.</returns>
    public static async Task<BattleResponse> PlayUntilAsync(
        Func<Task<BattleResponse>> read,
        Func<BattleResponse, IReadOnlyList<string>, Task> strike,
        Func<BattleResponse, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(strike);
        ArgumentNullException.ThrowIfNull(condition);

        for (var played = 0; ; played++)
        {
            var battle = await read();
            if (condition(battle))
            {
                return battle;
            }

            if (battle.Status != BattleStatus.InProgress)
            {
                throw FailException.ForFailure(
                    $"Battle {battle.BattleId} ended ({battle.Status}, winner '{battle.WinnerId}') "
                    + $"in round {battle.Round} before the condition held.");
            }

            if (played == RoundCap)
            {
                throw FailException.ForFailure(
                    $"Battle {battle.BattleId} did not reach the condition within {RoundCap} rounds.");
            }

            var humans = HumanSidesStanding(battle);
            if (humans.Count == 0)
            {
                throw FailException.ForFailure(
                    $"Battle {battle.BattleId} has only bots left; wait for it instead of playing it.");
            }

            await strike(battle, humans);
        }
    }
}
