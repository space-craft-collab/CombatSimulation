using System.Collections.Immutable;
using Battles.Features.GetBattle;
using Battles.Grains.Abstractions;

namespace Battles.Features.StartBattle;

/// <summary>
/// Starts a visitor's battle against the rule-based bot on an arena.
/// </summary>
/// <param name="grains">Reaches the arena and battle grains.</param>
internal sealed class StartBattleService(IGrainFactory grains)
{
    /// <summary>
    /// The participant id of the bot opponent.
    /// </summary>
    public const string BotId = "bot";

    /// <summary>
    /// Creates the battle on the arena and returns its first state.
    /// The bot mirrors the visitor's team, so every pick is a fair
    /// fight.
    /// </summary>
    /// <exception cref="ArgumentException">A species does not exist.</exception>
    public async Task<BattleResponse> StartVsBotAsync(string arenaId, StartBattleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var team = request.MonsterSpeciesIds.ToImmutableArray();
        var battleId = await grains.GetGrain<IArenaGrain>(arenaId).CreateBattleAsync(
        [
            new ParticipantSetup(request.PlayerId, false, team),
            new ParticipantSetup(BotId, true, team),
        ]);

        return BattleResponse.From(await grains.GetGrain<ILiveBattleGrain>(battleId).GetStateAsync());
    }
}
