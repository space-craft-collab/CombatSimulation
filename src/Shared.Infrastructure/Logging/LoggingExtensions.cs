using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Shared.Infrastructure.Logging;

/// <summary>
/// Registers NLog as the logging provider (ADR-0006).
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Replaces the default logging providers with NLog, configured
    /// from <c>nlog.config</c> in the host's content root.
    /// </summary>
    /// <param name="logging">The logging builder to configure.</param>
    /// <returns>The same builder for chaining.</returns>
    public static ILoggingBuilder AddArenaLogging(this ILoggingBuilder logging)
    {
        ArgumentNullException.ThrowIfNull(logging);

        logging.ClearProviders();
        logging.AddNLog();

        return logging;
    }
}
