namespace Battles.Features.StartBattle;

/// <summary>
/// Body of <c>POST /arenas/{arenaId}/battles</c>: a visitor starts a
/// battle against the bot (ADR-0007, "play vs bot").
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
    /// Checks the request's shape; whether the species exist is the
    /// grain's call.
    /// </summary>
    /// <returns>Errors per field; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(PlayerId))
        {
            errors[nameof(PlayerId)] = ["A player id is required."];
        }
        else if (PlayerId == StartBattleService.BotId)
        {
            errors[nameof(PlayerId)] = [$"'{StartBattleService.BotId}' is reserved for the bot."];
        }

        if (MonsterSpeciesIds.Count is 0 or > MaxTeamSize)
        {
            errors[nameof(MonsterSpeciesIds)] = [$"Field between 1 and {MaxTeamSize} monsters."];
        }

        return errors;
    }
}
