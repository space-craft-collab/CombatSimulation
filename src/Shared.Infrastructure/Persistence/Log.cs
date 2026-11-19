using Microsoft.Extensions.Logging;

namespace Shared.Infrastructure.Persistence;

/// <summary>
/// Source-generated log messages of the persistence wiring.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Applying pending migrations of {Context}")]
    public static partial void Migrating(ILogger logger, string context);
}
