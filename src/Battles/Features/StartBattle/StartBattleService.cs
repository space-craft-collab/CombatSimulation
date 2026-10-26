using Battles.Features.GetBattle;
using Battles.Grains.Abstractions;

namespace Battles.Features.StartBattle;

/// <summary>
/// Starts a visitor's battle on an arena: against the rule-based bot,
/// or opened for a human opponent to join.
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
    /// Against the bot it starts at once, and the bot mirrors the
    /// visitor's team, so every pick is a fair fight. Against a human
    /// it waits for players until someone joins.
    /// </summary>
    /// <exception cref="ArgumentException">A species does not exist.</exception>
    public async Task<BattleResponse> StartAsync(string arenaId, StartBattleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var arena = grains.GetGrain<IArenaGrain>(arenaId);
        var visitor = new ParticipantSetup(request.PlayerId, false, [.. request.MonsterSpeciesIds]);
        var battleId = request.Opponent switch
        {
            Opponent.Human => await arena.OpenBattleAsync(visitor),
            _ => await arena.CreateBattleAsync([visitor, MirrorBot(visitor)]),
        };

        return BattleResponse.From(await grains.GetGrain<ILiveBattleGrain>(battleId).GetStateAsync());
    }

    /// <summary>
    /// The bot opponent fielding the same team as the given side.
    /// </summary>
    public static ParticipantSetup MirrorBot(ParticipantSetup side)
    {
        ArgumentNullException.ThrowIfNull(side);

        return new ParticipantSetup(BotId, true, side.MonsterSpeciesIds);
    }
}
