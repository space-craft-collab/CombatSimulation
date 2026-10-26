namespace Battles.Grains;

/// <summary>
/// Pacing of the round loop (ADR-0007), bound from the
/// <c>Battles:Timing</c> configuration section.
/// </summary>
internal sealed class BattleTimingOptions
{
    /// <summary>
    /// The configuration section the options bind from.
    /// </summary>
    public const string SectionName = "Battles:Timing";

    /// <summary>
    /// How long human sides get to act before the round resolves
    /// without them.
    /// </summary>
    public TimeSpan TurnDeadline { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// How long an open battle waits for an opponent before it is
    /// abandoned.
    /// </summary>
    public TimeSpan JoinDeadline { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The pause between two rounds of a battle with only bots left.
    /// </summary>
    public TimeSpan BotRoundInterval { get; set; } = TimeSpan.FromMilliseconds(500);
}
