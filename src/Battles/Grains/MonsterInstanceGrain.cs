using Battles.Grains.Abstractions;
using Orleans.Runtime;

namespace Battles.Grains;

/// <summary>
/// Implementation of <see cref="IMonsterInstanceGrain"/>: owns one
/// monster's hit points for the length of a battle.
/// </summary>
/// <param name="state">The monster's persistent state.</param>
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
    public async Task<MonsterSnapshot> ApplyDamageAsync(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var stats = RequireStats();

        state.State.HitPoints = Math.Max(0, state.State.HitPoints - amount);
        await state.WriteStateAsync();

        return new MonsterSnapshot(stats.SpeciesId, state.State.HitPoints, stats.MaxHitPoints);
    }

    /// <inheritdoc />
    public Task<MonsterSnapshot> GetSnapshotAsync()
    {
        var stats = RequireStats();

        return Task.FromResult(new MonsterSnapshot(stats.SpeciesId, state.State.HitPoints, stats.MaxHitPoints));
    }

    private MonsterStats RequireStats() =>
        state.State.Stats
            ?? throw new InvalidOperationException($"Monster '{this.GetPrimaryKeyString()}' is not initialized.");
}
