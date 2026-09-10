using Microsoft.ApplicationInsights;

namespace Histo.Web.Telemetry;

/// <summary>
/// Supplementary Application Insights helper for named business events and exceptions
/// that need explicit contextual properties beyond a plain message. Day-to-day diagnostic
/// logging still goes through <see cref="Histo.Infrastructure.IAppLogger"/> (which now
/// reaches Application Insights automatically via the Serilog sink wired in Program.cs) —
/// use this only for <see cref="TrackEvent"/>-style business telemetry (e.g. "SubmissionCreated")
/// or an exception that needs extra structured properties.
/// </summary>
public sealed class TelemetryHelper
{
    private readonly TelemetryClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TelemetryHelper(TelemetryClient client, IHttpContextAccessor httpContextAccessor)
    {
        _client = client;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>Log an exception with structured context properties.</summary>
    public void TrackException(Exception ex, string operation, IDictionary<string, string>? additionalProperties = null)
    {
        var props = BaseProperties(operation);
        if (additionalProperties is not null)
            foreach (var kvp in additionalProperties)
                props[kvp.Key] = kvp.Value;
        _client.TrackException(ex, props);
    }

    /// <summary>Log a named business event (e.g. "SubmissionCreated", "SamplesDispatched").</summary>
    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null)
        => _client.TrackEvent(eventName, properties is null ? null : new Dictionary<string, string>(properties));

    /// <summary>Flush all pending telemetry — call on application shutdown.</summary>
    public void Flush() => _client.Flush();

    private Dictionary<string, string> BaseProperties(string operation)
    {
        var context = _httpContextAccessor.HttpContext;
        return new Dictionary<string, string>
        {
            ["operation"] = operation,
            // Non-PII identifier only — never the full claims/UPN.
            ["userId"] = context?.User?.Identity?.Name ?? "anonymous",
            ["pageUrl"] = context?.Request?.Path.Value ?? string.Empty,
        };
    }
}
