using Battles.Grains.Abstractions;
using Orleans.Runtime;

namespace Battles.Grains;

/// <summary>
/// Implementation of <see cref="IMonsterInstanceGrain"/>: keeps one
/// monster's hit points for the length of a battle.
/// </summary>
/// <param name="state">The monster's persistent state.</param>
// Pinned to the name Orleans would derive anyway: the grain type is part
// of every stored state and reminder key, so a class rename must not
// change it.
[GrainType("monsterinstance")]
internal sealed class MonsterInstanceGrain(
    [PersistentState("monster")] IPersistentState<MonsterInstanceState> state)
    : Grain, IMonsterInstanceGrain
{
    /// <inheritdoc />
    public async Task InitializeAsync(MonsterStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stats.MaxHitPoints);

        if (state.State.Stats is not null)
        {
            throw new InvalidOperationException($"Monster '{this.GetPrimaryKeyString()}' is already initialized.");
        }

        state.State.Stats = stats;
        state.State.HitPoints = stats.MaxHitPoints;
        await state.WriteStateAsync();
    }

    /// <inheritdoc />
    public async Task<MonsterSnapshot> SetHitPointsAsync(int hitPoints)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(hitPoints);
        var stats = RequireStats();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hitPoints, stats.MaxHitPoints);

        state.State.HitPoints = hitPoints;
        await state.WriteStateAsync();

        return new MonsterSnapshot(stats.SpeciesId, state.State.HitPoints, stats.MaxHitPoints);
    }

    /// <inheritdoc />
    public Task<MonsterSnapshot> GetSnapshotAsync()
    {
        var stats = RequireStats();

        return Task.FromResult(new MonsterSnapshot(stats.SpeciesId, state.State.HitPoints, stats.MaxHitPoints));
    }

    /// <inheritdoc />
    public async Task ClearAsync()
    {
        await state.ClearStateAsync();
        DeactivateOnIdle();
    }

    private MonsterStats RequireStats() =>
        state.State.Stats
            ?? throw new InvalidOperationException($"Monster '{this.GetPrimaryKeyString()}' is not initialized.");
}
