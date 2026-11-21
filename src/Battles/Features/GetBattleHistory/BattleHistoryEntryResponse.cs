namespace Battles.Features.GetBattleHistory;

/// <summary>
/// One completed battle in a player's history.
/// </summary>
/// <param name="BattleId">The battle's id; <c>GET /battles/{id}</c> has the details.</param>
/// <param name="ArenaId">The arena it ran on.</param>
/// <param name="CompletedAt">When it ended.</param>
/// <param name="Round">The round it ended in.</param>
/// <param name="WinnerId">The winning side, bot or human; <see langword="null"/> when abandoned.</param>
/// <param name="ParticipantIds">Every side, in slot order.</param>
internal sealed record BattleHistoryEntryResponse(
    Guid BattleId,
    string ArenaId,
    DateTimeOffset CompletedAt,
    int Round,
    string? WinnerId,
    IReadOnlyList<string> ParticipantIds);
