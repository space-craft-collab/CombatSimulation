using Battles.Features.GetBattle;
using Battles.Grains.Abstractions;

namespace Battles.Features.SubmitAction;

/// <summary>
/// Hands a participant's action to the battle grain.
/// </summary>
/// <param name="grains">Reaches the battle grains.</param>
internal sealed class SubmitActionService(IGrainFactory grains)
{
    /// <summary>
    /// Submits the action and returns the battle's state afterwards —
    /// a later round once it completed the round.
    /// </summary>
    /// <returns>The battle, or <see langword="null"/> if it was never started.</returns>
    /// <exception cref="ArgumentException">The action is not legal for this participant.</exception>
    /// <exception cref="InvalidOperationException">The battle is over or the participant already acted.</exception>
    public async Task<BattleResponse?> SubmitAsync(Guid battleId, SubmitActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var battle = grains.GetGrain<ILiveBattleGrain>(battleId);
        if ((await battle.GetStateAsync()).Status == BattleStatus.Created)
        {
            return null;
        }

        await battle.SubmitActionAsync(new BattleAction(
            request.ParticipantId, request.ActorSlot, request.TargetSlot, request.AbilityId));

        return BattleResponse.From(await battle.GetStateAsync());
    }
}
