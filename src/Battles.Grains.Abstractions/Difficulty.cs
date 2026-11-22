namespace Battles.Grains.Abstractions;

/// <summary>
/// How hard a battle against the bot is (ADR-0013). Only a bot side
/// has one; a human side is never weakened, whatever it says. Stored
/// as its number in grain state, so the values are frozen: never
/// renumber or reuse one, add new members at the end with the next
/// number.
/// </summary>
public enum Difficulty
{
    /// <summary>The bot's monsters fight with their species' stats.</summary>
    Normal = 0,

    /// <summary>
    /// The bot's monsters enter the battle weakened: less hit points
    /// and attack than their species.
    /// </summary>
    Easy = 1,
}
