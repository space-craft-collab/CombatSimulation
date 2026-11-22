using System.Net;
using System.Net.Http.Json;
using Battles.Features.GetBattle;
using Battles.Grains.Abstractions;
using Xunit;
using Xunit.Sdk;

namespace OrleansMonsterArena.Tests;

/// <summary>
/// Plays battles over HTTP for the tests, as a client would, without
/// caring how strong the monsters are or how many rounds a battle
/// takes (see <see cref="BattleRounds"/>). Every battle runs on
/// <see cref="TestArenas.Volcano"/>.
/// </summary>
/// <param name="client">A client of the host under test.</param>
internal sealed class HttpBattleDriver(HttpClient client)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>
    /// <c>/battles/{battleId}/actions</c>.
    /// </summary>
    /// <param name="battleId">The battle.</param>
    /// <returns>The relative URI.</returns>
    public static Uri Actions(Guid battleId) => new($"/battles/{battleId}/actions", UriKind.Relative);

    /// <summary>
    /// <c>/battles/{battleId}</c>.
    /// </summary>
    /// <param name="battleId">The battle.</param>
    /// <returns>The relative URI.</returns>
    public static Uri Battle(Guid battleId) => new($"/battles/{battleId}", UriKind.Relative);

    /// <summary>
    /// Starts a battle against the bot; it starts at once.
    /// </summary>
    /// <param name="playerId">The visitor.</param>
    /// <param name="team">The species the visitor fields, in slot order.</param>
    /// <returns>The battle's first state.</returns>
    public Task<BattleResponse> StartAgainstBotAsync(string playerId, params string[] team) =>
        PostStartAsync(new { playerId, monsterSpeciesIds = team });

    /// <summary>
    /// Opens a battle that waits for a second human.
    /// </summary>
    /// <param name="playerId">The host.</param>
    /// <param name="team">The species the host fields, in slot order.</param>
    /// <returns>The battle, waiting for players.</returns>
    public Task<BattleResponse> OpenAsync(string playerId, params string[] team) =>
        PostStartAsync(new { playerId, monsterSpeciesIds = team, opponent = "Human" });

    /// <summary>
    /// Joins a second human to an open battle, which starts round 1.
    /// </summary>
    /// <param name="battleId">The open battle.</param>
    /// <param name="playerId">The joining human.</param>
    /// <param name="team">The species they field, in slot order.</param>
    /// <returns>The battle's first state.</returns>
    public async Task<BattleResponse> JoinAsync(Guid battleId, string playerId, params string[] team)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri($"/battles/{battleId}/participants", UriKind.Relative),
            new { playerId, monsterSpeciesIds = team },
            Cancellation);
        response.EnsureSuccessStatusCode();

        return await ReadBattleAsync(response);
    }

    /// <summary>
    /// Reads the battle from <c>GET /battles/{battleId}</c>.
    /// </summary>
    /// <param name="battleId">The battle.</param>
    /// <returns>The battle as it stands.</returns>
    public async Task<BattleResponse> GetAsync(Guid battleId) =>
        await client.GetFromJsonAsync<BattleResponse>(Battle(battleId), Cancellation)
        ?? throw FailException.ForFailure($"GET /battles/{battleId} returned no battle.");

    /// <summary>
    /// Lets each of the given human sides strike once, in the order
    /// given (<see cref="BattleRounds.ChooseStrike"/>). The round
    /// resolves once every human side still standing has acted.
    /// </summary>
    /// <param name="battleId">The battle.</param>
    /// <param name="participantIds">The sides that act.</param>
    /// <returns>The battle as the last action's response shows it.</returns>
    public async Task<BattleResponse> StrikeAsync(Guid battleId, params string[] participantIds) =>
        await StrikeAsync(await GetAsync(battleId), participantIds);

    /// <summary>
    /// Plays rounds, every human side striking once per round, until
    /// the condition holds; see <see cref="BattleRounds.PlayUntilAsync"/>.
    /// </summary>
    /// <param name="battleId">The battle.</param>
    /// <param name="condition">When to stop.</param>
    /// <returns>The first state that meets the condition.</returns>
    public Task<BattleResponse> PlayUntilAsync(Guid battleId, Func<BattleResponse, bool> condition) =>
        BattleRounds.PlayUntilAsync(() => GetAsync(battleId), StrikeAsync, condition);

    /// <summary>
    /// Plays rounds until the battle has completed, every human side
    /// striking once per round.
    /// </summary>
    /// <param name="battleId">The battle.</param>
    /// <returns>The completed battle.</returns>
    public Task<BattleResponse> PlayToTheEndAsync(Guid battleId) =>
        PlayUntilAsync(battleId, battle => battle.Status == BattleStatus.Completed);

    private async Task<BattleResponse> StrikeAsync(BattleResponse battle, IReadOnlyList<string> participantIds)
    {
        var latest = battle;
        foreach (var participantId in participantIds)
        {
            var (actorSlot, targetSlot) = BattleRounds.ChooseStrike(battle, participantId);
            using var response = await client.PostAsJsonAsync(
                Actions(battle.BattleId),
                new { participantId, actorSlot, targetSlot, abilityId = BattleRounds.Strike },
                Cancellation);
            response.EnsureSuccessStatusCode();
            latest = await ReadBattleAsync(response);
        }

        return latest;
    }

    private async Task<BattleResponse> PostStartAsync(object body)
    {
        using var response = await client.PostAsJsonAsync(TestArenas.VolcanoBattles, body, Cancellation);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw FailException.ForFailure(
                $"POST {TestArenas.VolcanoBattles} answered {(int)response.StatusCode}: "
                + await response.Content.ReadAsStringAsync(Cancellation));
        }

        return await ReadBattleAsync(response);
    }

    private static async Task<BattleResponse> ReadBattleAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<BattleResponse>(Cancellation)
        ?? throw FailException.ForFailure("The endpoint returned no battle.");
}
