namespace Histo.Web.Security;

public static class SecurityHeadersExtensions
{
    // The views use inline scripts and styles, so those two sources stay 'unsafe-inline'.
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; font-src 'self' data:; object-src 'none'; base-uri 'self'; frame-ancestors 'none'";

    /// <summary>Adds baseline hardening headers to every response, including redirects, errors and static files.</summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, bool sendHsts)
    {
        return app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers.ContentSecurityPolicy = ContentSecurityPolicy;
                if (sendHsts) headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
                return Task.CompletedTask;
            });
            return next();
        });
    }
}
