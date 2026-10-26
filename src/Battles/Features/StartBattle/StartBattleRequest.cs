using System.Text.Json.Serialization;

namespace Battles.Features.StartBattle;

/// <summary>
/// Body of <c>POST /arenas/{arenaId}/battles</c>: a visitor starts a
/// battle against the bot, or opens one and waits for a human
/// (ADR-0007 game modes).
/// </summary>
internal sealed record StartBattleRequest
{
    /// <summary>
    /// The most monsters one side may field.
    /// </summary>
    public const int MaxTeamSize = 3;

    /// <summary>
    /// The visitor's player id. There is no auth before Phase 6, so
    /// the client names itself.
    /// </summary>
    public required string PlayerId { get; init; }

    /// <summary>
    /// The Catalog species the visitor fields, in slot order.
    /// </summary>
    public required IReadOnlyList<string> MonsterSpeciesIds { get; init; }

    /// <summary>
    /// Who to fight; the bot unless the visitor asks to wait for a human.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<Opponent>))]
    public Opponent Opponent { get; init; } = Opponent.Bot;

    /// <summary>
    /// Checks the request's shape; whether the species exist is the
    /// grain's call.
    /// </summary>
    /// <returns>Errors per field; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate() => ValidateTeam(PlayerId, MonsterSpeciesIds);

    /// <summary>
    /// Checks a human side's player id and team; shared with joining
    /// an open battle.
    /// </summary>
    /// <param name="playerId">The player id.</param>
    /// <param name="monsterSpeciesIds">The fielded species.</param>
    /// <returns>Errors per field; empty when the side is valid.</returns>
    public static Dictionary<string, string[]> ValidateTeam(string playerId, IReadOnlyList<string> monsterSpeciesIds)
    {
        ArgumentNullException.ThrowIfNull(monsterSpeciesIds);

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(playerId))
        {
            errors[nameof(PlayerId)] = ["A player id is required."];
        }
        else if (playerId == StartBattleService.BotId)
        {
            errors[nameof(PlayerId)] = [$"'{StartBattleService.BotId}' is reserved for the bot."];
        }

        if (monsterSpeciesIds.Count is 0 or > MaxTeamSize)
        {
            errors[nameof(MonsterSpeciesIds)] = [$"Field between 1 and {MaxTeamSize} monsters."];
        }

        return errors;
    }
}
