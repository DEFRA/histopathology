using System.Security.Cryptography;

namespace Histo.Web.Security;

public static class SecurityHeadersExtensions
{
    public const string CspNonceKey = "csp-nonce";

    // Inline scripts must carry the per-request nonce; inline style attributes are the only inline source left.
    private const string ContentSecurityPolicyTemplate =
        "default-src 'self'; script-src 'self' 'nonce-{0}'; style-src 'self'; style-src-attr 'unsafe-inline'; " +
        "img-src 'self' data:; font-src 'self' data:; object-src 'none'; base-uri 'self'; frame-ancestors 'none'";

    /// <summary>Returns the nonce issued for this request's Content-Security-Policy, or null if none was issued.</summary>
    public static string? GetCspNonce(this HttpContext context) =>
        context.Items.TryGetValue(CspNonceKey, out var nonce) ? nonce as string : null;

    /// <summary>Error pages are read-only; a re-executed POST would otherwise be rejected by antiforgery validation.</summary>
    public static Task ForceGetWhenReExecuted(HttpContext context, RequestDelegate next)
    {
        if (context.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodeReExecuteFeature>() is not null)
            context.Request.Method = HttpMethods.Get;
        return next(context);
    }

    /// <summary>Adds baseline hardening headers to every response, including redirects, errors and static files.</summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, bool sendHsts)
    {
        return app.Use((context, next) =>
        {
            var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
            context.Items[CspNonceKey] = nonce;

            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers.ContentSecurityPolicy = string.Format(System.Globalization.CultureInfo.InvariantCulture, ContentSecurityPolicyTemplate, nonce);
                if (sendHsts) headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
                return Task.CompletedTask;
            });
            return next();
        });
    }
}
