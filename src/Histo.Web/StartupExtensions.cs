using Azure.Monitor.OpenTelemetry.AspNetCore;
using Histo.Web.Auth;
using Histo.Web.Security;
using Histo.Web.Telemetry;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using System.Security.Cryptography.X509Certificates;

namespace Histo.Web;

/// <summary>Startup wiring extracted from <c>Program.cs</c> so the top-level file stays small.</summary>
internal static class StartupExtensions
{
    internal const string AuthCookieName = "Histo.Auth";
    internal const string SessionCookieName = "Histo.Session";
    private const string SessionMarkerKey = "SessionStarted";

    private static readonly string[] SessionExpirySkippedPaths =
        ["/health", "/Saml2", "/Errors", "/AccessDenied", "/SessionError", "/signin"];

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

    internal static Saml2Configuration CreateSaml2Configuration(IConfigurationSection saml2Section)
    {
        var saml2Config = new Saml2Configuration
        {
            Issuer             = saml2Section["SPEntityId"] ?? string.Empty,
            SignatureAlgorithm = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256",
            CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.ChainTrust,
            RevocationMode            = X509RevocationMode.NoCheck,
        };

        // Entra ID sets the assertion's <AudienceRestriction> to the SP Entity ID — without
        // registering it here, audience validation always fails (IDX10214) regardless of
        // what the IdP sends, since AllowedAudienceUris would otherwise be empty.
        if (!string.IsNullOrEmpty(saml2Config.Issuer))
            saml2Config.AllowedAudienceUris.Add(saml2Config.Issuer);

        ConfigureSigningCertificate(saml2Config, saml2Section["SPCertificateThumbprint"]);
        return saml2Config;
    }

    // SP signing certificate (optional in dev; required in all deployed environments).
    private static void ConfigureSigningCertificate(Saml2Configuration saml2Config, string? thumbprint)
    {
        if (string.IsNullOrEmpty(thumbprint))
        {
            Log.Warning("Saml2:SPCertificateThumbprint is not configured — SP will not sign AuthnRequests. Required for all non-development environments.");
            return;
        }

        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);
        var cert = store.Certificates
            .Find(X509FindType.FindByThumbprint, thumbprint, false)
            .OfType<X509Certificate2>()
            .FirstOrDefault();
        if (cert is not null)
            saml2Config.SigningCertificate = cert;
        else
            Log.Warning("SP signing certificate with thumbprint {Thumbprint} was not found in the certificate store.", thumbprint);
    }

    // The default cookie challenge/forbid handlers build an ABSOLUTE redirect URI from
    // Request.Scheme/Request.Host. Behind a reverse proxy that doesn't forward a trusted
    // X-Forwarded-Host (or forwards headers UseForwardedHeaders isn't picking up), that
    // leaks the App Service origin hostname instead of the public custom domain. Forcing
    // a relative redirect removes the dependency on Request.Host entirely — the browser
    // resolves it against whatever host is currently in the address bar.
    internal static void ConfigureSamlCookie(CookieAuthenticationOptions options, TimeSpan idleTimeout)
    {
        options.Cookie.Name = AuthCookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = idleTimeout;
        options.SlidingExpiration = true;

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

    // IdPMetadataUrl format: https://login.microsoftonline.com/{tenant-id}/federationmetadata/2007-06/federationmetadata.xml?appid={app-id}
    internal static async Task LoadIdpMetadataAsync(WebApplication app, Saml2Configuration saml2Config)
    {
        var idpMetadataUrl = app.Configuration.GetSection("Saml2")["IdPMetadataUrl"];
        if (string.IsNullOrEmpty(idpMetadataUrl) || !Uri.TryCreate(idpMetadataUrl, UriKind.Absolute, out var metadataUri))
        {
            Log.Warning("Saml2:IdPMetadataUrl is not a valid URL. " +
                        "Set it to: https://login.microsoftonline.com/{{tenant-id}}/federationmetadata/2007-06/federationmetadata.xml?appid={{app-id}}");
            return;
        }

        try
        {
            var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
            var entityDescriptor  = new EntityDescriptor();
            await entityDescriptor.ReadIdPSsoDescriptorFromUrlAsync(httpClientFactory, metadataUri);

            if (entityDescriptor.IdPSsoDescriptor != null)
                ApplyIdpDescriptor(saml2Config, entityDescriptor.IdPSsoDescriptor);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not load IdP metadata from {MetadataUrl}. " +
                "Configure Saml2:IdPMetadataUrl with the Entra ID federation metadata URL.", idpMetadataUrl);
        }
    }

    private static void ApplyIdpDescriptor(Saml2Configuration saml2Config, IdPSsoDescriptor descriptor)
    {
        saml2Config.SingleSignOnDestination = descriptor.SingleSignOnServices
            .FirstOrDefault(s => s.Binding.OriginalString.Contains("HTTP-Redirect"))?.Location
            ?? descriptor.SingleSignOnServices.FirstOrDefault()?.Location;

        saml2Config.SingleLogoutDestination = descriptor.SingleLogoutServices
            .FirstOrDefault()?.Location;

        saml2Config.SignatureValidationCertificates.AddRange(descriptor.SigningCertificates);

        Log.Information("Loaded SAML2 IdP metadata. SSO: {SsoUrl}. Signing certs: {CertCount}.",
            saml2Config.SingleSignOnDestination,
            saml2Config.SignatureValidationCertificates.Count);
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
    // routes to /Saml2/login with a return to the home page (locally the dev bypass skips this).
    internal static void MapSignIn(this WebApplication app)
    {
        app.MapGet("/signin", (HttpContext context) =>
        {
            context.Response.Cookies.Delete(AuthCookieName);
            context.Response.Cookies.Delete(SessionCookieName);

            if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("DevAuth:BypassEntraId"))
                return Results.Redirect("/Index");

            return Results.Challenge(new AuthenticationProperties { RedirectUri = "/Index" }, ["saml2"]);
        }).AllowAnonymous();
    }
}
