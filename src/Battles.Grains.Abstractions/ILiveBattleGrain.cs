namespace Battles.Grains.Abstractions;

/// <summary>
/// One running battle (ADR-0007), keyed by its battle id. Moves
/// through <see cref="BattleStatus.Created"/> →
/// <see cref="BattleStatus.InProgress"/> →
/// <see cref="BattleStatus.Completed"/>, resolving one round at a time.
/// Monster slots are numbered across the whole battle in participant
/// order: with two monsters each, side one holds slots 0–1, side two 2–3.
/// </summary>
public interface ILiveBattleGrain : IGrainWithGuidKey
{
    /// <summary>
    /// Sets the battle up and starts round 1.
    /// </summary>
    /// <param name="setup">The arena and the participants.</param>
    /// <returns>A task that completes once the battle is in progress.</returns>
    Task StartAsync(BattleSetup setup);

    /// <summary>
    /// Records a participant's action for the current round.
    /// </summary>
    /// <param name="action">The action to record.</param>
    /// <returns>A task that completes once the action is accepted.</returns>
    Task SubmitActionAsync(BattleAction action);

    /// <summary>
    /// Returns the battle's current state.
    /// </summary>
    /// <returns>The current state.</returns>
    Task<BattleState> GetStateAsync();
}
