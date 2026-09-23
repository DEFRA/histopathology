namespace Histo.Infrastructure;

/// <summary>
/// Application-level logging abstraction.
///
/// Replaces direct calls to <c>DataAccessLib/clsInfoLog.vb::InfoLog.LogToEventViewer</c>
/// and <c>HistopathologyLib/clsLog.vb::clsLog.LogException</c>.
///
/// The implementation (<see cref="AppLogger{T}"/>) delegates to
/// <c>Microsoft.Extensions.Logging.ILogger&lt;T&gt;</c>, which is backed by Serilog
/// at the composition root (<c>Histo.Web/Program.cs</c>).
///
/// Callers should prefer injecting <see cref="IAppLogger"/> rather than the generic
/// <c>ILogger&lt;T&gt;</c> directly, so that the logging contract is explicit and testable.
/// </summary>
public interface IAppLogger
{
    void LogInfo(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(string message, Exception? ex = null, params object[] args);
}
