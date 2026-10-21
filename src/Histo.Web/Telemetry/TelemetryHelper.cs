namespace Histo.Web.Telemetry;

/// <summary>
/// Helper for named business events and exceptions that need explicit contextual properties.
/// Day-to-day diagnostic logging still goes through <see cref="Histo.Infrastructure.IAppLogger"/>.
/// Both paths are written through <see cref="ILogger"/>, which the Azure Monitor OpenTelemetry
/// exporter sends on: errors with an exception become exception records, and a log carrying the
/// <c>microsoft.custom_event.name</c> attribute becomes a custom event.
/// </summary>
public sealed class TelemetryHelper
{
    private const string CustomEventNameKey = "microsoft.custom_event.name";

    private readonly ILogger<TelemetryHelper> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TelemetryHelper(ILogger<TelemetryHelper> logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>Log an exception with structured context properties.</summary>
    public void TrackException(Exception ex, string operation, IDictionary<string, string>? additionalProperties = null)
    {
        var context = _httpContextAccessor.HttpContext;
        var extra = additionalProperties is null
            ? string.Empty
            : string.Join(", ", additionalProperties.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        _logger.LogError(
            ex,
            "Exception in {Operation} for user {UserId} at {PageUrl}. {AdditionalProperties}",
            operation,
            // Non-PII identifier only - never the full claims/UPN.
            context?.User?.Identity?.Name ?? "anonymous",
            context?.Request?.Path.Value ?? string.Empty,
            extra);
    }

    /// <summary>Log a named business event (e.g. "SubmissionCreated", "SamplesDispatched").</summary>
    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null)
    {
        var state = new List<KeyValuePair<string, object?>> { new(CustomEventNameKey, eventName) };
        if (properties is not null)
            state.AddRange(properties.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)));

        _logger.Log(LogLevel.Information, default, state, null, (_, _) => eventName);
    }
}