using System.Collections.Immutable;

namespace Battles.Grains.Abstractions;

/// <summary>
/// The battle browser of one arena (ADR-0007): one grain per
/// arena, keyed by the Catalog arena id. It creates battles on
/// its arena and lists the ones that are open or running.
/// </summary>
public interface IArenaGrain : IGrainWithStringKey
{
    /// <summary>
    /// Creates and starts a new battle on this arena.
    /// </summary>
    /// <param name="participants">The sides taking part.</param>
    /// <returns>The id of the new battle.</returns>
    Task<Guid> CreateBattleAsync(ImmutableArray<ParticipantSetup> participants);

    /// <summary>
    /// Lists the battles on this arena that have not completed.
    /// </summary>
    /// <returns>One summary per open or running battle.</returns>
    Task<ImmutableArray<BattleSummary>> GetBattlesAsync();
}
