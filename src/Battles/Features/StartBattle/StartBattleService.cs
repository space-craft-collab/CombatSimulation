using Battles.Features.GetBattle;
using Battles.Grains.Abstractions;
using Catalog.Contracts;

namespace Battles.Features.StartBattle;

/// <summary>
/// Starts a visitor's battle on an arena: against the rule-based bot,
/// or opened for a human opponent to join.
/// </summary>
/// <param name="grains">Reaches the arena and battle grains.</param>
/// <param name="catalog">Knows which arenas exist.</param>
/// <param name="bot">Fields the bot's team.</param>
internal sealed class StartBattleService(IGrainFactory grains, ICatalogQueryService catalog, BotOpponent bot)
{
    /// <summary>
    /// Creates the battle on the arena and returns its first state.
    /// Against the bot it starts at once; the bot fields a team of
    /// its own, as large as the visitor's, on the requested
    /// difficulty. Against a human it waits for players until someone
    /// joins, and the difficulty is ignored.
    /// </summary>
    /// <returns>The battle's first state, or <see langword="null"/> if
    /// the arena does not exist in the Catalog.</returns>
    /// <exception cref="ArgumentException">A species does not exist.</exception>
    public async Task<BattleResponse?> StartAsync(string arenaId, StartBattleRequest request)
    {
        ArgumentNullException.ThrowIfNull(arenaId);
        ArgumentNullException.ThrowIfNull(request);

        if (await catalog.FindArenaAsync(arenaId) is null)
        {
            return null;
        }

        var arena = grains.GetGrain<IArenaGrain>(arenaId);
        var visitor = new ParticipantSetup(request.PlayerId, false, [.. request.MonsterSpeciesIds]);
        var battleId = request.Opponent switch
        {
            Opponent.Human => await arena.OpenBattleAsync(visitor),
            _ => await arena.CreateBattleAsync([visitor, await bot.FieldAsync(visitor.MonsterSpeciesIds.Length, request.Difficulty)]),
        };

        return BattleResponse.From(await grains.GetGrain<ILiveBattleGrain>(battleId).GetStateAsync());
    }
}
