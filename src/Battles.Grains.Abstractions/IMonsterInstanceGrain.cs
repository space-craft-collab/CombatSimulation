namespace Battles.Grains.Abstractions;

/// <summary>
/// One monster in play, keyed by <c>"{BattleId}:{Slot}"</c>
/// (ADR-0007). Lives only as long as its battle.
/// </summary>
public interface IMonsterInstanceGrain : IGrainWithStringKey
{
    /// <summary>
    /// Brings the monster into the battle at full health.
    /// </summary>
    /// <param name="stats">The stats it enters with.</param>
    /// <returns>A task that completes once the monster is ready.</returns>
    Task InitializeAsync(MonsterStats stats);

    /// <summary>
    /// Reduces the monster's hit points.
    /// </summary>
    /// <param name="amount">The damage to apply; must not be negative.</param>
    /// <returns>The monster's state after the damage.</returns>
    Task<MonsterSnapshot> ApplyDamageAsync(int amount);

    /// <summary>
    /// Returns the monster's current state.
    /// </summary>
    /// <returns>The current state.</returns>
    Task<MonsterSnapshot> GetSnapshotAsync();
}
