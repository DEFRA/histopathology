using Microsoft.Extensions.Logging;

namespace Histo.Infrastructure;

/// <summary>
/// <see cref="IAppLogger"/> implementation backed by
/// <c>Microsoft.Extensions.Logging.ILogger&lt;T&gt;</c>.
///
/// Serilog is the concrete provider wired at the composition root in
/// <c>Histo.Web/Program.cs</c> via <c>UseSerilog()</c>.
/// </summary>
public sealed class AppLogger<T> : IAppLogger
{
    private readonly ILogger<T> _logger;

    public AppLogger(ILogger<T> logger) => _logger = logger;

    public void LogInfo(string message, params object[] args) =>
        _logger.LogInformation(message, args);

    public void LogWarning(string message, params object[] args) =>
        _logger.LogWarning(message, args);

    public void LogError(string message, Exception? ex = null, params object[] args) =>
        _logger.LogError(ex, message, args);
}
