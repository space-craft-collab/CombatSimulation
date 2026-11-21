using Battles.Grains.Abstractions;
using Catalog.Contracts;

namespace Battles.Features.ListBattles;

/// <summary>
/// Reads an arena's battle list: the open battles a visitor can join
/// and the ones already running.
/// </summary>
/// <param name="grains">Reaches the arena grains.</param>
/// <param name="catalog">Knows which arenas exist.</param>
internal sealed class ListBattlesService(IGrainFactory grains, ICatalogQueryService catalog)
{
    /// <summary>
    /// Lists the arena's battles that have not completed.
    /// </summary>
    /// <param name="arenaId">The arena.</param>
    /// <returns>One entry per open or running battle, or
    /// <see langword="null"/> if the arena does not exist in the
    /// Catalog.</returns>
    public async Task<IReadOnlyList<BattleSummaryResponse>?> ListAsync(string arenaId)
    {
        ArgumentNullException.ThrowIfNull(arenaId);

        if (await catalog.FindArenaAsync(arenaId) is null)
        {
            return null;
        }

        return [.. (await grains.GetGrain<IArenaGrain>(arenaId).GetBattlesAsync())
            .Select(battle => new BattleSummaryResponse(battle.BattleId, battle.Status, battle.ParticipantIds))];
    }
}
