using Battles.Grains.Abstractions;
using Battles.Infrastructure;

namespace Battles.Features.GetBattle;

/// <summary>
/// Reads one battle across both stores (ADR-0003): the grain while
/// the battle is live, the cold store once it completed and its hot
/// state was cleared. A cleared grain reads as <c>Created</c>, the
/// same as an id that never existed, so the cold store decides.
/// </summary>
/// <param name="grains">Reaches the battle grains.</param>
/// <param name="snapshots">Reads completed battles.</param>
internal sealed class GetBattleService(IGrainFactory grains, BattleSnapshotReadRepository snapshots)
{
    /// <summary>
    /// Returns the battle, or <see langword="null"/> if neither store
    /// knows it.
    /// </summary>
    public async Task<BattleResponse?> FindAsync(Guid battleId, CancellationToken cancellationToken = default)
    {
        var state = await grains.GetGrain<ILiveBattleGrain>(battleId).GetStateAsync();
        if (state.Status != BattleStatus.Created)
        {
            return BattleResponse.From(state);
        }

        return await snapshots.FindAsync(battleId, cancellationToken) is { } snapshot
            ? BattleResponse.From(snapshot)
            : null;
    }
}
