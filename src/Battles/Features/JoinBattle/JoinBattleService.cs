using Battles.Features.GetBattle;
using Battles.Features.StartBattle;
using Battles.Grains.Abstractions;

namespace Battles.Features.JoinBattle;

/// <summary>
/// Fills the open seat of a battle that waits for players: with a
/// second human, or with the bot ("play vs bot instead").
/// </summary>
/// <param name="grains">Reaches the battle grains.</param>
internal sealed class JoinBattleService(IGrainFactory grains)
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

        var battle = grains.GetGrain<ILiveBattleGrain>(battleId);
        if ((await battle.GetStateAsync()).Status == BattleStatus.Created)
        {
            return null;
        }

        await battle.JoinAsync(new ParticipantSetup(request.PlayerId, false, [.. request.MonsterSpeciesIds]));

        return BattleResponse.From(await battle.GetStateAsync());
    }

    /// <summary>
    /// Lets the bot take the open seat, mirroring the host's team, and
    /// returns the battle's first state.
    /// </summary>
    /// <returns>The battle, or <see langword="null"/> if it was never started.</returns>
    /// <exception cref="InvalidOperationException">The battle no longer waits for players.</exception>
    public async Task<BattleResponse?> JoinBotAsync(Guid battleId)
    {
        var battle = grains.GetGrain<ILiveBattleGrain>(battleId);
        var state = await battle.GetStateAsync();
        if (state.Status == BattleStatus.Created)
        {
            return null;
        }

        var hostId = state.Combatants.FirstOrDefault()?.ParticipantId;
        var host = new ParticipantSetup(
            hostId ?? "",
            false,
            [.. state.Combatants
                .Where(combatant => combatant.ParticipantId == hostId)
                .Select(combatant => combatant.SpeciesId)]);
        await battle.JoinAsync(StartBattleService.MirrorBot(host));

        return BattleResponse.From(await battle.GetStateAsync());
    }
}
