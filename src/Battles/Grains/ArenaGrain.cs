using System.Collections.Immutable;
using Battles.Grains.Abstractions;
using Orleans.Runtime;

namespace Battles.Grains;

/// <summary>
/// Implementation of <see cref="IArenaGrain"/>: the battle browser of
/// one arena (ADR-0007). It only creates and lists battles; it never
/// sits on the round path, so a busy battle does not block the arena.
/// </summary>
/// <param name="state">The arena's persistent state.</param>
internal sealed class ArenaGrain(
    [PersistentState("arena")] IPersistentState<ArenaState> state)
    : Grain, IArenaGrain
{
    /// <inheritdoc />
    public async Task<Guid> CreateBattleAsync(ImmutableArray<ParticipantSetup> participants)
    {
        var battleId = Guid.NewGuid();
        await GrainFactory.GetGrain<ILiveBattleGrain>(battleId)
            .StartAsync(new BattleSetup(this.GetPrimaryKeyString(), participants));

        state.State.Battles[battleId] = participants.Length;
        await state.WriteStateAsync();

        return battleId;
    }

    /// <inheritdoc />
    public async Task<ImmutableArray<BattleSummary>> GetBattlesAsync()
    {
        var states = await Task.WhenAll(state.State.Battles.Keys
            .Select(battleId => GrainFactory.GetGrain<ILiveBattleGrain>(battleId).GetStateAsync()));

        var completed = states.Where(battle => battle.Status == BattleStatus.Completed).ToList();
        if (completed.Count > 0)
        {
            completed.ForEach(battle => state.State.Battles.Remove(battle.BattleId));
            await state.WriteStateAsync();
        }

        return [.. states
            .Where(battle => battle.Status != BattleStatus.Completed)
            .Select(battle => new BattleSummary(battle.BattleId, battle.Status, state.State.Battles[battle.BattleId]))];
    }
}
