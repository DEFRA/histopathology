namespace Histo.Web.Auth;

/// <summary>Single place that records authorization denials. Logs only the opaque user ID, never claim values.</summary>
public static class AuthorizationAudit
{
    public static void LogDenied(HttpContext? context, string reason, int? resourceId = null)
    {
        var logger = context?.RequestServices?.GetService<ILoggerFactory>()?.CreateLogger("Histo.Web.Authorization");
        if (context is null || logger is null) return;

        var userId = context.User.FindFirst(AppClaimTypes.UserDbId)?.Value ?? "anonymous";
        logger.LogWarning(
            "Authorization denied: {Reason} (path={Path}, method={Method}, userId={UserId}, resourceId={ResourceId}).",
            reason, context.Request.Path.Value, context.Request.Method, userId, resourceId);
    }
}
