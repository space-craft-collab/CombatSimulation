namespace Battles.Grains.Abstractions;

/// <summary>
/// One monster in play, keyed by <c>"{BattleId}:{Slot}"</c>
/// (ADR-0007): the record of its hit points. The combat rules decide
/// them, the battle tells the monster after every round. Lives only
/// as long as its battle.
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
    /// Tells the monster where it stands after a round. The value is
    /// absolute, not a change, so telling it twice does no harm: a
    /// battle that failed in the middle of a round simply says it
    /// again (ADR-0012).
    /// </summary>
    /// <param name="hitPoints">The monster's hit points, from 0 (defeated) to its full health.</param>
    /// <returns>The monster's state afterwards.</returns>
    Task<MonsterSnapshot> SetHitPointsAsync(int hitPoints);

    /// <summary>
    /// Returns the monster's current state.
    /// </summary>
    /// <returns>The current state.</returns>
    Task<MonsterSnapshot> GetSnapshotAsync();

    /// <summary>
    /// Deletes the monster's stored state once its battle is in the
    /// cold store (ADR-0003), and lets the grain deactivate. The
    /// monster reads as never initialized afterwards.
    /// </summary>
    /// <returns>A task that completes once the state is deleted.</returns>
    Task ClearAsync();
}
