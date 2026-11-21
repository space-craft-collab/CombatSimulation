namespace Players.Contracts;

/// <summary>
/// Rules for player ids. There are no accounts before Phase 6, so
/// clients choose their ids; the stats store bounds their length.
/// </summary>
public static class PlayerIds
{
    /// <summary>
    /// The longest player id the stats store accepts.
    /// </summary>
    public const int MaxLength = 64;
}
