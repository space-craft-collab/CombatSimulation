using System.Text.Json.Serialization;
using Battles.Grains.Abstractions;
using Players.Contracts;

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
    /// How hard the bot is (ADR-0013); <see cref="Difficulty.Normal"/>
    /// unless the visitor asks for <see cref="Difficulty.Easy"/>, which
    /// weakens the bot's monsters. Ignored when <see cref="Opponent"/>
    /// is <see cref="Opponent.Human"/>: a battle between humans has no
    /// difficulty, and a bot that later takes the open seat brings its
    /// own (<c>POST /battles/{battleId}/bot</c>).
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<Difficulty>))]
    public Difficulty Difficulty { get; init; } = Difficulty.Normal;

    /// <summary>
    /// Checks the request's shape; whether the species exist is the
    /// grain's call.
    /// </summary>
    /// <returns>Errors per field; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = ValidateTeam(PlayerId, MonsterSpeciesIds);
        if (ValidateDifficulty(Difficulty) is { } difficultyError)
        {
            errors[nameof(Difficulty)] = [difficultyError];
        }

        return errors;
    }

    /// <summary>
    /// Checks a difficulty; shared with seating the bot in an open
    /// battle. The JSON converter also takes numbers, so an undefined
    /// one is caught here.
    /// </summary>
    /// <param name="difficulty">The requested difficulty.</param>
    /// <returns>The error, or <see langword="null"/> when the difficulty is known.</returns>
    public static string? ValidateDifficulty(Difficulty difficulty) =>
        Enum.IsDefined(difficulty)
            ? null
            : $"Choose one of: {string.Join(", ", Enum.GetNames<Difficulty>())}.";

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
        else if (playerId == BotOpponent.BotId)
        {
            errors[nameof(PlayerId)] = [$"'{BotOpponent.BotId}' is reserved for the bot."];
        }
        else if (playerId.Length > PlayerIds.MaxLength)
        {
            errors[nameof(PlayerId)] = [$"A player id has at most {PlayerIds.MaxLength} characters."];
        }
        else if (PlayerIds.HasOuterWhitespace(playerId))
        {
            errors[nameof(PlayerId)] = ["A player id must not start or end with whitespace."];
        }

        if (monsterSpeciesIds.Count is 0 or > MaxTeamSize)
        {
            errors[nameof(MonsterSpeciesIds)] = [$"Field between 1 and {MaxTeamSize} monsters."];
        }

        return errors;
    }
}
