using Microsoft.Extensions.Logging;

namespace Histo.Infrastructure;

/// <summary>
/// <see cref="IAppLogger"/> implementation backed by
/// <c>Microsoft.Extensions.Logging.ILogger&lt;T&gt;</c>.
///
/// Serilog is the concrete provider wired at the composition root in
/// <c>Histo.Web/Program.cs</c> via <c>UseSerilog()</c>. When Azure Monitor is configured, an
/// <c>ILogger</c> error that carries an exception is exported as an exception record, so callers
/// that catch and swallow an exception still get visibility through <see cref="LogError"/>.
/// </summary>
public sealed class AppLogger : IAppLogger
{
    private readonly ILogger _logger;

    public AppLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void LogInfo(string message, params object[] args) =>
        _logger.LogInformation(message, args);

    public void LogWarning(string message, params object[] args) =>
        _logger.LogWarning(message, args);

    public void LogError(string message, Exception? ex = null, params object[] args) =>
        _logger.LogError(ex, message, args);
}
