using Battles.Features.GetBattle;
using Battles.Grains.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Sdk;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Plays battles through the grains for the tests, without caring
/// how strong the monsters are or how many rounds a battle takes
/// (see <see cref="BattleRounds"/>). It reads a battle through
/// <see cref="GetBattleService"/>, because a completed battle is
/// cleared from its grain and lives in the cold store (ADR-0003).
/// </summary>
/// <param name="services">The host's services.</param>
internal sealed class GrainBattleDriver(IServiceProvider services)
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    private readonly IGrainFactory _grains = services.GetRequiredService<IClusterClient>();

    private readonly GetBattleService _battles = services.GetRequiredService<GetBattleService>();

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>
    /// Reads the battle from whichever store holds it.
    /// </summary>
    /// <param name="battleId">The battle.</param>
    /// <returns>The battle as it stands.</returns>
    public async Task<BattleResponse> ReadAsync(Guid battleId) =>
        await _battles.FindAsync(battleId, Cancellation)
        ?? throw FailException.ForFailure($"Neither store knows battle {battleId}.");

    /// <summary>
    /// Lets each of the given human sides strike once, in the order
    /// given (<see cref="BattleRounds.ChooseStrike"/>). The round
    /// resolves once every human side still standing has acted.
    /// </summary>
    /// <param name="battleId">The battle.</param>
    /// <param name="participantIds">The sides that act.</param>
    /// <returns>The battle afterwards.</returns>
    public async Task<BattleResponse> StrikeAsync(Guid battleId, params string[] participantIds)
    {
        await StrikeAsync(await ReadAsync(battleId), participantIds);

        return await ReadAsync(battleId);
    }

    /// <summary>
    /// Plays rounds, every human side striking once per round, until
    /// the condition holds; see <see cref="BattleRounds.PlayUntilAsync"/>.
    /// </summary>
    /// <param name="battleId">The battle.</param>
    /// <param name="condition">When to stop.</param>
    /// <returns>The first state that meets the condition.</returns>
    public Task<BattleResponse> PlayUntilAsync(Guid battleId, Func<BattleResponse, bool> condition) =>
        BattleRounds.PlayUntilAsync(() => ReadAsync(battleId), StrikeAsync, condition);

    /// <summary>
    /// Plays rounds until the battle has completed, every human side
    /// striking once per round.
    /// </summary>
    /// <param name="battleId">The battle.</param>
    /// <returns>The completed battle, from the cold store unless its hand-over failed.</returns>
    public Task<BattleResponse> PlayToTheEndAsync(Guid battleId) =>
        PlayUntilAsync(battleId, battle => battle.Status == BattleStatus.Completed);

    /// <summary>
    /// Polls the battle until the condition holds, for rounds the
    /// test does not play itself: deadlines and the bot loop run on
    /// reminders and timers.
    /// </summary>
    /// <param name="battleId">The battle.</param>
    /// <param name="condition">When to stop.</param>
    /// <returns>The first state seen that meets the condition.</returns>
    public async Task<BattleResponse> WaitForAsync(Guid battleId, Func<BattleResponse, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        var giveUp = DateTimeOffset.UtcNow + WaitLimit;
        while (true)
        {
            var battle = await ReadAsync(battleId);
            if (condition(battle))
            {
                return battle;
            }

            if (DateTimeOffset.UtcNow > giveUp)
            {
                throw FailException.ForFailure(
                    $"Battle state {battle} did not reach the expected condition within {WaitLimit}.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), Cancellation);
        }
    }

    private async Task StrikeAsync(BattleResponse battle, IReadOnlyList<string> participantIds)
    {
        var grain = _grains.GetGrain<ILiveBattleGrain>(battle.BattleId);
        foreach (var participantId in participantIds)
        {
            var (actorSlot, targetSlot) = BattleRounds.ChooseStrike(battle, participantId);
            await grain.SubmitActionAsync(new BattleAction(participantId, actorSlot, targetSlot, BattleRounds.Strike));
        }
    }
}
