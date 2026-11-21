namespace Players.Contracts;

/// <summary>
/// Rules for player ids. There are no accounts before Phase 6, so
/// clients choose their ids; the stats store bounds their length.
/// An id is taken exactly as written: <c>Alice</c> and <c>alice</c>
/// are two players.
/// </summary>
public static class PlayerIds
{
    /// <summary>
    /// The longest player id the stats store accepts.
    /// </summary>
    public const int MaxLength = 64;

    /// <summary>
    /// Whether an id starts or ends with whitespace. Such ids are
    /// rejected rather than trimmed: SQL Server ignores trailing
    /// spaces when it compares, so <c>"alice "</c> would share the
    /// record of <c>"alice"</c>.
    /// </summary>
    /// <param name="playerId">The player id to check.</param>
    /// <returns><see langword="true"/> if the id has whitespace at
    /// either end.</returns>
    public static bool HasOuterWhitespace(string playerId)
    {
        ArgumentNullException.ThrowIfNull(playerId);

        return playerId.Length > 0 && (char.IsWhiteSpace(playerId[0]) || char.IsWhiteSpace(playerId[^1]));
    }
}
