namespace Histo.Web.Auth;

/// <summary>
/// Single place that records authorization denials. Logs only the opaque user ID, never claim values.
/// Each denial is also an Application Insights custom event (<see cref="EventName"/>), so it can be queried and alerted on.
/// </summary>
public static class AuthorizationAudit
{
    public const string EventName = "AuthorizationDenied";
    private const string CustomEventNameKey = "microsoft.custom_event.name";

    public static void LogDenied(HttpContext? context, string reason, int? resourceId = null)
    {
        var logger = context?.RequestServices?.GetService<ILoggerFactory>()?.CreateLogger("Histo.Web.Authorization");
        if (context is null || logger is null) return;

        var userId = context.User.FindFirst(AppClaimTypes.UserDbId)?.Value ?? "anonymous";
        var path = context.Request.Path.Value;
        var method = context.Request.Method;
        var message = $"Authorization denied: {reason} (path={path}, method={method}, userId={userId}, resourceId={resourceId}).";

        var state = new List<KeyValuePair<string, object?>>
        {
            new(CustomEventNameKey, EventName),
            new("Reason", reason),
            new("Path", path),
            new("Method", method),
            new("UserId", userId),
            new("ResourceId", resourceId),
        };
        logger.Log(LogLevel.Warning, default, state, null, (_, _) => message);
    }
}
