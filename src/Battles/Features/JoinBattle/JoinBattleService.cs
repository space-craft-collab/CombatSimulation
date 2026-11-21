using Battles.Features.GetBattle;
using Battles.Features.StartBattle;
using Battles.Grains.Abstractions;

namespace Battles.Features.JoinBattle;

/// <summary>
/// Fills the open seat of a battle that waits for players: with a
/// second human, or with the bot ("play vs bot instead").
/// A battle that is in the cold store already is over, which the
/// grain answers as a conflict.
/// </summary>
/// <param name="grains">Reaches the battle grains.</param>
/// <param name="battles">Reads the battle from either store.</param>
internal sealed class JoinBattleService(IGrainFactory grains, GetBattleService battles)
{
    /// <summary>
    /// Adds a human opponent and returns the battle's first state.
    /// </summary>
    /// <returns>The battle, or <see langword="null"/> if it was never started.</returns>
    /// <exception cref="ArgumentException">A species does not exist, or the player already takes part.</exception>
    /// <exception cref="InvalidOperationException">The battle no longer waits for players.</exception>
    public async Task<BattleResponse?> JoinAsync(Guid battleId, JoinBattleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await battles.FindAsync(battleId) is null)
        {
            return null;
        }

        await grains.GetGrain<ILiveBattleGrain>(battleId)
            .JoinAsync(new ParticipantSetup(request.PlayerId, false, [.. request.MonsterSpeciesIds]));

        return await battles.FindAsync(battleId);
    }

    /// <summary>
    /// Lets the bot take the open seat, mirroring the host's team, and
    /// returns the battle's first state.
    /// </summary>
    /// <returns>The battle, or <see langword="null"/> if it was never started.</returns>
    /// <exception cref="InvalidOperationException">The battle no longer waits for players.</exception>
    public async Task<BattleResponse?> JoinBotAsync(Guid battleId)
    {
        if (await battles.FindAsync(battleId) is not { } current)
        {
            return null;
        }

        var hostId = current.Combatants.Count > 0 ? current.Combatants[0].ParticipantId : null;
        var host = new ParticipantSetup(
            hostId ?? "",
            false,
            [.. current.Combatants
                .Where(combatant => combatant.ParticipantId == hostId)
                .Select(combatant => combatant.SpeciesId)]);
        await grains.GetGrain<ILiveBattleGrain>(battleId).JoinAsync(StartBattleService.MirrorBot(host));

        return await battles.FindAsync(battleId);
    }
}
