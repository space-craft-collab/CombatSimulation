using Battles.Features.GetBattle;
using Battles.Grains.Abstractions;

namespace Battles.Features.SubmitAction;

/// <summary>
/// Hands a participant's action to the battle grain.
/// </summary>
/// <param name="grains">Reaches the battle grains.</param>
/// <param name="battles">Reads the battle from either store.</param>
internal sealed class SubmitActionService(IGrainFactory grains, GetBattleService battles)
{
    /// <summary>
    /// Submits the action and returns the battle's state afterwards —
    /// a later round once it completed the round, or the cold store's
    /// record once it ended the battle. A battle that is in the cold
    /// store already is over, which the grain answers as a conflict.
    /// </summary>
    /// <returns>The battle, or <see langword="null"/> if it was never started.</returns>
    /// <exception cref="ArgumentException">The action is not legal for this participant.</exception>
    /// <exception cref="InvalidOperationException">The battle is over or the participant already acted.</exception>
    public async Task<BattleResponse?> SubmitAsync(Guid battleId, SubmitActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await battles.FindAsync(battleId) is null)
        {
            return null;
        }

        await grains.GetGrain<ILiveBattleGrain>(battleId).SubmitActionAsync(new BattleAction(
            request.ParticipantId, request.ActorSlot, request.TargetSlot, request.AbilityId));

        return await battles.FindAsync(battleId);
    }
}
