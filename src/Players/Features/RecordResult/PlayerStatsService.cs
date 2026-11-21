using Players.Contracts;
using Players.Infrastructure;

namespace Players.Features.RecordResult;

/// <summary>
/// Serves <see cref="IPlayerStatsService"/>: counts each human side
/// of a decided battle in the player stats table. Each side is its own
/// write; like the hot→cold bridge that calls it, this is
/// at-most-once, not transactional (ADR-0003).
/// </summary>
/// <param name="stats">Writes the player stats table.</param>
internal sealed class PlayerStatsService(PlayerStatsWriteRepository stats) : IPlayerStatsService
{
    /// <inheritdoc />
    public async Task RecordResultAsync(BattleResultDto result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        Validate(result);

        if (result.WinnerId is { } winnerId)
        {
            await stats.IncrementAsync(winnerId, wins: 1, losses: 0, cancellationToken);
        }

        foreach (var loserId in result.LoserIds)
        {
            await stats.IncrementAsync(loserId, wins: 0, losses: 1, cancellationToken);
        }
    }

    private static void Validate(BattleResultDto result)
    {
        ArgumentNullException.ThrowIfNull(result.LoserIds, nameof(result));

        string[] playerIds = result.WinnerId is { } winnerId
            ? [winnerId, .. result.LoserIds]
            : [.. result.LoserIds];

        if (playerIds.Length == 0)
        {
            throw new ArgumentException("A decided battle has at least one player side.", nameof(result));
        }

        if (playerIds.Any(id => string.IsNullOrWhiteSpace(id) || id.Length > PlayerIds.MaxLength))
        {
            throw new ArgumentException(
                $"Player ids must be non-blank and at most {PlayerIds.MaxLength} characters.",
                nameof(result));
        }

        if (playerIds.Distinct(StringComparer.Ordinal).Count() != playerIds.Length)
        {
            throw new ArgumentException("A player appears on more than one side.", nameof(result));
        }
    }
}
