using Azure.Monitor.OpenTelemetry.AspNetCore;
using Histo.Web.Auth;
using Histo.Web.Security;
using Histo.Web.Telemetry;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;

namespace Histo.Web;

/// <summary>Startup wiring extracted from <c>Program.cs</c> so the top-level file stays small.</summary>
internal static class StartupExtensions
{
    internal const string AuthCookieName = "Histo.Auth";
    internal const string SessionCookieName = "Histo.Session";
    private const string SessionMarkerKey = "SessionStarted";

    private static readonly string[] SessionExpirySkippedPaths =
        ["/health", "/auth", "/signin-oidc", "/signout-oidc", "/signout-callback-oidc", "/Errors", "/AccessDenied", "/SessionError", "/signin"];

    internal static bool IsAzureMonitorConfigured(IConfiguration configuration)
    {
        var connectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        return !string.IsNullOrWhiteSpace(connectionString) && !connectionString.StartsWith("__", StringComparison.Ordinal);
    }

    // Optional shared key ring so auth/session cookies survive restarts and scale-out.
    internal static void AddSharedDataProtection(this WebApplicationBuilder builder)
    {
        var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(dataProtectionKeysPath)) return;

        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
            .SetApplicationName("Histo.Web");
    }

    // The default cookie challenge/forbid handlers build an ABSOLUTE redirect URI from
    // Request.Scheme/Request.Host. Behind a reverse proxy that doesn't forward a trusted
    // X-Forwarded-Host (or forwards headers UseForwardedHeaders isn't picking up), that
    // leaks the App Service origin hostname instead of the public custom domain. Forcing
    // a relative redirect removes the dependency on Request.Host entirely — the browser
    // resolves it against whatever host is currently in the address bar.
    internal static void ConfigureAuthCookie(CookieAuthenticationOptions options, TimeSpan idleTimeout)
    {
        options.Cookie.Name = AuthCookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = idleTimeout;
        options.SlidingExpiration = true;
        options.LoginPath = "/auth/login";
        options.AccessDeniedPath = "/AccessDenied";
        // Keep challenges on the cookie handler so OnRedirectToLogin (and /SessionError) always runs.
        options.ForwardChallenge = null;

        var originalValidatePrincipal = options.Events.OnValidatePrincipal;
        options.Events.OnValidatePrincipal = async context =>
        {
            await originalValidatePrincipal(context);
            await SingleSessionPolicy.ValidateAsync(context);
        };

        var originalRedirectToLogin = options.Events.OnRedirectToLogin;
        options.Events.OnRedirectToLogin = context =>
        {
            if (IsTimedOutHtmlRequest(context.Request))
            {
                context.Response.Redirect("/SessionError");
                return Task.CompletedTask;
            }

            MakeRedirectRelative(context);

            // Only GET requests can be replayed after sign-in; anything else returns to the home page.
            if (!HttpMethods.IsGet(context.Request.Method))
                context.RedirectUri = $"{options.LoginPath}?{options.ReturnUrlParameter}={Uri.EscapeDataString("/Index")}";

            return originalRedirectToLogin(context);
        };

        var originalRedirectToAccessDenied = options.Events.OnRedirectToAccessDenied;
        options.Events.OnRedirectToAccessDenied = context =>
        {
            MakeRedirectRelative(context);
            return originalRedirectToAccessDenied(context);
        };
    }

    // A leftover auth cookie means the user was signed in and timed out, not a first visit.
    // /signin is exempt: it deletes the stale cookie but this request still carries it.
    private static bool IsTimedOutHtmlRequest(HttpRequest request) =>
        AcceptsHtml(request)
        && request.Cookies.ContainsKey(AuthCookieName)
        && !request.Path.StartsWithSegments("/signin");

    private static bool AcceptsHtml(HttpRequest request) =>
        request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);

    private static void MakeRedirectRelative(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (Uri.TryCreate(context.RedirectUri, UriKind.Absolute, out var absoluteUri))
            context.RedirectUri = absoluteUri.PathAndQuery;
    }

    // Matches the standard App Service setting name (APPLICATIONINSIGHTS_CONNECTION_STRING,
    // top-level, the same key Azure's own extension uses). Skipped when unset (e.g. local dev).
    // OpenTelemetry does not emit the Request-Context app-id response header.
    internal static void AddAzureMonitorIfConfigured(this WebApplicationBuilder builder)
    {
        if (!IsAzureMonitorConfigured(builder.Configuration)) return;

        // Serilog forwards to the OpenTelemetry provider (writeToProviders), so the default
        // providers are cleared here to avoid duplicate console output.
        builder.Logging.ClearProviders();
        builder.Services.AddOpenTelemetry().UseAzureMonitor(o =>
            o.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
    }

    internal static void UseServiceProblemHandling(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            return;
        }

        // GDS "There is a problem with the service" for all unhandled 5xx. Explicitly tracks
        // the exception (with URL/user context) before redirecting, so a genuinely unhandled
        // failure is fully visible in Application Insights rather than just an opaque TraceId.
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(context =>
            {
                TrackUnhandledException(context);
                context.Response.Redirect("/Errors/ServiceProblem");
                return Task.CompletedTask;
            });
        });
        app.UseStatusCodePagesWithReExecute("/Errors/ServiceProblem");
        app.Use(SecurityHeadersExtensions.ForceGetWhenReExecuted);
    }

    private static void TrackUnhandledException(HttpContext context)
    {
        var error = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
        if (error is null) return;

        var telemetry = context.RequestServices.GetRequiredService<TelemetryHelper>();
        telemetry.TrackException(error, "GlobalExceptionHandler", new Dictionary<string, string>
        {
            ["url"] = context.Request.Path.Value ?? string.Empty,
        });
    }

    // The auth cookie can outlive the server-side session (restart, idle expiry). A request that
    // arrives signed in with a session cookie but no marker means the session expired -> timeout page.
    // Static files are served earlier by UseStaticFiles and non-HTML requests are ignored.
    internal static void UseSessionExpiryRedirect(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (IsSessionExpiryCandidate(context) && await SessionExpiredAsync(context))
            {
                context.Response.Redirect("/SessionError");
                return;
            }

            await next();
        });
    }

    private static bool IsSessionExpiryCandidate(HttpContext context) =>
        context.User.Identity?.IsAuthenticated == true
        && AcceptsHtml(context.Request)
        && !SessionExpirySkippedPaths.Any(p => context.Request.Path.StartsWithSegments(p));

    private static async Task<bool> SessionExpiredAsync(HttpContext context)
    {
        await context.Session.LoadAsync(context.RequestAborted);
        if (context.Session.GetString(SessionMarkerKey) is not null) return false;

        context.Session.SetString(SessionMarkerKey, "1");
        return context.Request.Cookies.ContainsKey(SessionCookieName);
    }

    // Re-sign-in after a timeout: drop the stale cookies, then challenge the cookie scheme, which
    // routes to /auth/login with a return to the home page (locally the dev bypass skips this).
    internal static void MapSignIn(this WebApplication app)
    {
        app.MapGet("/signin", (HttpContext context) =>
        {
            context.Response.Cookies.Delete(AuthCookieName);
            context.Response.Cookies.Delete(SessionCookieName);

            if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("DevAuth:BypassEntraId"))
                return Results.Redirect("/Index");

            return Results.Challenge(new AuthenticationProperties { RedirectUri = "/Index" }, [CookieAuthenticationDefaults.AuthenticationScheme]);
        }).AllowAnonymous();
    }
}
