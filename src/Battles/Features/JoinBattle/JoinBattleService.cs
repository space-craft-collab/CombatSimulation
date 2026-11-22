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
/// <param name="bot">Fields the bot's team.</param>
internal sealed class JoinBattleService(IGrainFactory grains, GetBattleService battles, BotOpponent bot)
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
    /// Lets the bot take the open seat with a team of its own, as
    /// large as the host's, and returns the battle's first state.
    /// </summary>
    /// <returns>The battle, or <see langword="null"/> if it was never started.</returns>
    /// <exception cref="InvalidOperationException">The battle no longer waits for players.</exception>
    public async Task<BattleResponse?> JoinBotAsync(Guid battleId)
    {
        if (await battles.FindAsync(battleId) is not { } current)
        {
            return null;
        }

        // A battle that waits for players has its host's monsters
        // only. One that does not is refused by the grain below,
        // whatever team the bot brings.
        var teamSize = Math.Max(1, current.Combatants.Count(combatant => !combatant.IsBot));
        await grains.GetGrain<ILiveBattleGrain>(battleId).JoinAsync(await bot.FieldAsync(teamSize));

        return await battles.FindAsync(battleId);
    }
}
