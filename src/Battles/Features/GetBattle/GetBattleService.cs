using Battles.Grains.Abstractions;

namespace Battles.Features.GetBattle;

/// <summary>
/// Reads one battle from its grain.
/// </summary>
/// <param name="grains">Reaches the battle grains.</param>
internal sealed class GetBattleService(IGrainFactory grains)
{
    /// <summary>
    /// Returns the battle, or <see langword="null"/> if it was never started.
    /// </summary>
    public async Task<BattleResponse?> FindAsync(Guid battleId)
    {
        var state = await grains.GetGrain<ILiveBattleGrain>(battleId).GetStateAsync();

        return state.Status == BattleStatus.Created ? null : BattleResponse.From(state);
    }
}
