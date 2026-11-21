using Microsoft.Extensions.Logging;

namespace Battles.Grains;

/// <summary>
/// Source-generated log messages of the battle grains.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Battle {BattleId} completed, but its hand-over to the cold store failed; the hot state is kept")]
    public static partial void HandOverFailed(ILogger logger, Exception exception, Guid battleId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Battle {BattleId} is in the cold store, but clearing its hot state failed; its rows stay behind")]
    public static partial void ClearFailed(ILogger logger, Exception exception, Guid battleId);
}
