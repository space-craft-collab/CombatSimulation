using Battles.Grains.Abstractions;

namespace Battles.Features.ListBattles;

/// <summary>
/// Reads an arena's battle list: the open battles a visitor can join
/// and the ones already running.
/// </summary>
/// <param name="grains">Reaches the arena grains.</param>
internal sealed class ListBattlesService(IGrainFactory grains)
{
    /// <summary>
    /// Lists the arena's battles that have not completed.
    /// </summary>
    /// <param name="arenaId">The arena.</param>
    /// <returns>One entry per open or running battle.</returns>
    public async Task<IReadOnlyList<BattleSummaryResponse>> ListAsync(string arenaId) =>
        [.. (await grains.GetGrain<IArenaGrain>(arenaId).GetBattlesAsync())
            .Select(battle => new BattleSummaryResponse(battle.BattleId, battle.Status, battle.ParticipantIds))];
}
