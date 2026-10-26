namespace Battles.Grains.Abstractions;

/// <summary>
/// One running battle (ADR-0007), keyed by its battle id. Moves
/// through <see cref="BattleStatus.Created"/> →
/// <see cref="BattleStatus.WaitingForPlayers"/> (only when opened for
/// a human opponent) → <see cref="BattleStatus.InProgress"/> →
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
    /// Opens the battle with its first side and waits for an opponent.
    /// An open battle nobody joins before the join deadline is
    /// abandoned.
    /// </summary>
    /// <param name="arenaId">The arena the battle runs on.</param>
    /// <param name="host">The human who opens the battle.</param>
    /// <returns>A task that completes once the battle waits for players.</returns>
    Task OpenAsync(string arenaId, ParticipantSetup host);

    /// <summary>
    /// Adds the opponent to an open battle and starts round 1. A bot
    /// joins through the same call ("play vs bot instead").
    /// </summary>
    /// <param name="participant">The joining side.</param>
    /// <returns>A task that completes once the battle is in progress.</returns>
    Task JoinAsync(ParticipantSetup participant);

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
