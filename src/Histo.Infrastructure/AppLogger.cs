using System.Text.RegularExpressions;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;

namespace Histo.Infrastructure;

/// <summary>
/// <see cref="IAppLogger"/> implementation backed by
/// <c>Microsoft.Extensions.Logging.ILogger&lt;T&gt;</c>.
///
/// Serilog is the concrete provider wired at the composition root in
/// <c>Histo.Web/Program.cs</c> via <c>UseSerilog()</c>.
///
/// <see cref="LogError"/> also calls <see cref="TelemetryClient.TrackException"/> directly.
/// The Serilog to Application Insights sink is configured with <c>TelemetryConverter.Traces</c>,
/// so exceptions logged only through <c>ILogger</c> are not reliably queryable from the
/// exceptions table (they can surface as traces with severityLevel=Error instead). Callers that
/// catch and swallow an exception (returning false/null rather than rethrowing) rely entirely on
/// this method for exception visibility, so it must not depend on the sink converter alone.
/// </summary>
public sealed class AppLogger<T> : IAppLogger
{
    private readonly ILogger<T> _logger;
    private readonly TelemetryClient _telemetryClient;

    public AppLogger(ILogger<T> logger, TelemetryClient telemetryClient)
    {
        _logger = logger;
        _telemetryClient = telemetryClient;
    }

    public void LogInfo(string message, params object[] args) =>
        _logger.LogInformation(message, args);

    public void LogWarning(string message, params object[] args) =>
        _logger.LogWarning(message, args);

    public void LogError(string message, Exception? ex = null, params object[] args)
    {
        _logger.LogError(ex, message, args);

        if (ex is not null)
            _telemetryClient.TrackException(ex, new Dictionary<string, string> { ["message"] = FormatMessage(message, args) });
    }

    // {NamedPlaceholder} tokens are matched positionally to args, same as the ILogger structured template above.
    private static readonly Regex PlaceholderPattern = new(@"\{[^{}]+\}", RegexOptions.Compiled);

    private static string FormatMessage(string template, object[] args)
    {
        if (args.Length == 0) return template;
        var i = 0;
        return PlaceholderPattern.Replace(template, _ => i < args.Length ? Convert.ToString(args[i++]) ?? string.Empty : string.Empty);
    }
}
