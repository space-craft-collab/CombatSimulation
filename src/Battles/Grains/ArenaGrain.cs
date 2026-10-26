using System.Collections.Immutable;
using Battles.Grains.Abstractions;
using Orleans.Runtime;

namespace Battles.Grains;

/// <summary>
/// Implementation of <see cref="IArenaGrain"/>: the battle browser of
/// one arena (ADR-0007). It only creates, opens and lists battles; it never
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

        await RememberAsync(battleId);

        return battleId;
    }

    /// <inheritdoc />
    public async Task<Guid> OpenBattleAsync(ParticipantSetup host)
    {
        var battleId = Guid.NewGuid();
        await GrainFactory.GetGrain<ILiveBattleGrain>(battleId)
            .OpenAsync(this.GetPrimaryKeyString(), host);

        await RememberAsync(battleId);

        return battleId;
    }

    /// <inheritdoc />
    public async Task<ImmutableArray<BattleSummary>> GetBattlesAsync()
    {
        var states = await Task.WhenAll(state.State.Battles
            .Select(battleId => GrainFactory.GetGrain<ILiveBattleGrain>(battleId).GetStateAsync()));

        var completed = states.Where(battle => battle.Status == BattleStatus.Completed).ToList();
        if (completed.Count > 0)
        {
            completed.ForEach(battle => state.State.Battles.Remove(battle.BattleId));
            await state.WriteStateAsync();
        }

        return [.. states
            .Where(battle => battle.Status != BattleStatus.Completed)
            .Select(battle => new BattleSummary(
                battle.BattleId,
                battle.Status,
                [.. battle.Combatants.Select(combatant => combatant.ParticipantId).Distinct()]))];
    }

    private async Task RememberAsync(Guid battleId)
    {
        state.State.Battles.Add(battleId);
        await state.WriteStateAsync();
    }
}
