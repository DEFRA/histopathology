using Dapper;
// Modular monolith — each module registers its own internals via extension method
using Histo.Administration;
using Histo.AuditLog;
using Histo.Histology;
using Histo.Infrastructure;
using Histo.QualityControl;
using Histo.Reporting.Reports;
using Histo.Reporting.Services;
using Histo.Submissions;
using Histo.Web.Auth;
using Histo.Web.Security;
using Histo.Web.Services;
using Histo.Web.Telemetry;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Serilog;

// Bootstrap Serilog before the host is built so startup errors are captured.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

// Register Dapper type handler for DateTime? — handles legacy stored procedures that
// return date columns as CONVERT(VARCHAR, col, 103) strings (dd/MM/yyyy format).
// Must be called before any Dapper query executes.
SqlMapper.AddTypeHandler(new NullableDateTimeTypeHandler());
SqlMapper.AddTypeHandler(new DateTimeTypeHandler());
// Map the audit log SP column "DateTime" → AuditLogEntry.ChangedAt
AuditLogDapperSetup.RegisterTypeMaps();

try
{
    Log.Information("Starting Histo.Web");

    var builder = WebApplication.CreateBuilder(args);

    // -- Serilog -------------------------------------------------------------
    builder.Host.UseSerilog((ctx, services, cfg) =>
    {
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .WriteTo.Console();
    }, writeToProviders: IsAzureMonitorConfigured(builder.Configuration));

    // -- Strongly-typed options -----------------------------------------------
    builder.Services.Configure<AppOptions>(
        builder.Configuration.GetSection(AppOptions.SectionName));

    // One idle timeout drives both the auth cookie and the server-side session.
    const string AuthCookieName = "Histo.Auth";
    const string SessionCookieName = "Histo.Session";
    const string SessionMarkerKey = "SessionStarted";
    var idleTimeout = TimeSpan.FromMinutes(Math.Max(1, builder.Configuration.GetValue("Session:IdleTimeoutMinutes", 20)));

    // Optional shared key ring so auth/session cookies survive restarts and scale-out.
    var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
    if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
    {
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
            .SetApplicationName("Histo.Web");
    }

    // -- Forwarded headers (reverse proxy / edge in front of App Service) -----
    // dev-cde.azure.defra.cloud terminates TLS and forwards to the App Service's
    // default hostname. Without this, Request.Scheme/Request.Host reflect the
    // origin (devcdewebaw1401.azurewebsites.net) rather than the public custom
    // domain, causing absolute redirects (e.g. the OIDC redirect_uri) to leak
    // the origin hostname. Azure's edge proxy IPs are not fixed, so the default
    // KnownNetworks/KnownProxies allow-list is cleared to trust the forwarded
    // headers regardless of hop address.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // -- DB connection factory ------------------------------------------------
    // Connection string comes from ConnectionStrings:HistologyDb in appsettings.json
    // (Key Vault reference in non-dev environments — never a plaintext password here).
    var connectionString = builder.Configuration.GetConnectionString("HistologyDb")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:HistologyDb is not configured. " +
            "Set it in appsettings.json (dev) or via App Service configuration (non-dev).");

    builder.Services.AddSingleton<IDbConnectionFactory>(
        new SqlConnectionFactory(connectionString));

    // -- Logger adapter (wraps Microsoft.Extensions.Logging for domain services) --
    // IAppLogger is a non-generic interface; register via ILoggerFactory so every
    // service injection site gets a logger whose category is the declaring service type.
    // Using a factory delegate ensures the ILoggerFactory is resolved from the
    // built container at call-time, not before services are fully initialised.
    builder.Services.AddTransient<IAppLogger>(sp =>
    {
        var factory = sp.GetRequiredService<ILoggerFactory>();
        return new AppLogger(factory.CreateLogger<IAppLogger>());
    });
    builder.Services.AddScoped<TelemetryHelper>();

    // -- Health checks --------------------------------------------------------
    builder.Services.AddHealthChecks();
    // TODO Phase 1+: add SQL health check:
    //   .AddSqlServer(connectionString, name: "histology-db", tags: ["db"]);

    // -- Razor Pages + MVC controllers (controllers needed for the /auth endpoints) --
    builder.Services.AddRazorPages(options =>
                    {
                        options.Conventions.AddFolderApplicationModelConvention(
                            "/Admin", model => model.Filters.Add(new Histo.Web.Auth.MaintenanceOnlyPageFilter()));
                        options.Conventions.AddFolderApplicationModelConvention(
                            "/AuditLog", model => model.Filters.Add(new Histo.Web.Auth.MaintenanceOnlyPageFilter()));
                    })
                    .AddSessionStateTempDataProvider();
    builder.Services.AddControllers(); // AuthController (/auth/login, /auth/signout)

    // -- Entra ID OpenID Connect authentication -------------------------------
    // Authorization code flow + PKCE via Microsoft.Identity.Web. See docs/ENTRA-REGISTRATION.md.
    var azureAdSection = builder.Configuration.GetSection("AzureAd");
    AzureAdConfigurationValidator.Validate(builder.Configuration);
    var publicOrigin = azureAdSection["PublicOrigin"];
    var forceLogin = azureAdSection.GetValue("ForceLogin", true);

    builder.Services
        .AddAuthentication(o =>
        {
            o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            // Challenge via the cookie scheme so an expired cookie reaches OnRedirectToLogin (and the
            // /SessionError page) instead of going straight to Entra ID.
            o.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddMicrosoftIdentityWebApp(azureAdSection);

    builder.Services.AddScoped<OidcSignInHandler>();

    builder.Services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.Scope.Add("email");

        var originalRedirectToIdp = options.Events.OnRedirectToIdentityProvider;
        options.Events.OnRedirectToIdentityProvider = async context =>
        {
            await originalRedirectToIdp(context);
            OidcRedirects.ToIdentityProvider(context.ProtocolMessage, options.CallbackPath, publicOrigin, forceLogin);
        };

        var originalRedirectForSignOut = options.Events.OnRedirectToIdentityProviderForSignOut;
        options.Events.OnRedirectToIdentityProviderForSignOut = async context =>
        {
            await originalRedirectForSignOut(context);
            OidcRedirects.ToIdentityProviderForSignOut(context.ProtocolMessage, options.SignedOutCallbackPath, publicOrigin);
        };

        var originalTokenValidated = options.Events.OnTokenValidated;
        options.Events.OnTokenValidated = async context =>
        {
            await originalTokenValidated(context);
            await context.HttpContext.RequestServices.GetRequiredService<OidcSignInHandler>()
                .OnTokenValidatedAsync(context.Principal);
        };

        options.Events.OnRemoteFailure = context =>
        {
            // Exception only — IdentityModel redacts PII; the response itself is never logged.
            context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>()
                .LogWarning(context.Failure, "OIDC sign-in failed at the callback.");
            context.Response.Redirect("/AccessDenied");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });

    // The default cookie challenge/forbid handlers build an ABSOLUTE redirect URI from
    // Request.Scheme/Request.Host. Behind a reverse proxy that doesn't forward a trusted
    // X-Forwarded-Host (or forwards headers UseForwardedHeaders isn't picking up), that
    // leaks the App Service origin hostname instead of the public custom domain. Forcing
    // a relative redirect removes the dependency on Request.Host entirely — the browser
    // resolves it against whatever host is currently in the address bar.
    builder.Services.PostConfigure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
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
            var request = context.Request;

            // A leftover auth cookie means the user was signed in and timed out, not a first visit.
            // /signin is exempt: it deletes the stale cookie but this request still carries it.
            if (request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase)
                && request.Cookies.ContainsKey(AuthCookieName)
                && !request.Path.StartsWithSegments("/signin"))
            {
                context.Response.Redirect("/SessionError");
                return Task.CompletedTask;
            }

            if (Uri.TryCreate(context.RedirectUri, UriKind.Absolute, out var absoluteUri))
                context.RedirectUri = absoluteUri.PathAndQuery;

            // Only GET requests can be replayed after sign-in; anything else returns to the home page.
            if (!HttpMethods.IsGet(request.Method))
                context.RedirectUri = $"{options.LoginPath}?{options.ReturnUrlParameter}={Uri.EscapeDataString("/Index")}";

            return originalRedirectToLogin(context);
        };

        var originalRedirectToAccessDenied = options.Events.OnRedirectToAccessDenied;
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (Uri.TryCreate(context.RedirectUri, UriKind.Absolute, out var absoluteUri))
                context.RedirectUri = absoluteUri.PathAndQuery;
            return originalRedirectToAccessDenied(context);
        };
    });

    // Claims transformation — per-request tblUser refresh; OidcSignInHandler bakes the claims in at sign-in.
    builder.Services.AddScoped<IClaimsTransformation, HistopathologyClaimsTransformation>();

    // -- Session --------------------------------------------------------------
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(o =>
    {
        o.IdleTimeout = idleTimeout;
        o.Cookie.Name = SessionCookieName;
        o.Cookie.HttpOnly = true;
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        o.Cookie.IsEssential = true;
    });
    builder.Services.AddAntiforgery(o => o.Cookie.SecurePolicy = CookieSecurePolicy.Always);
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ISessionService, SessionService>();

    // -- Administration module -------------------------------------------------
    builder.Services.AddAdministrationModule();

    // -- AuditLog module -------------------------------------------------------
    builder.Services.AddAuditLogModule();

    // -- Histology module ------------------------------------------------------
    builder.Services.AddHistologyModule();

    // -- QualityControl module -------------------------------------------------
    builder.Services.AddQualityControlModule();

    // -- Submissions module ----------------------------------------------------
    builder.Services.AddSubmissionsModule();

    // -- Reporting module ------------------------------------------------------
    builder.Services.AddTransient<HistologyReportDataSetBuilder>();
    builder.Services.AddTransient<QCNoteDataSetBuilder>();
    builder.Services.AddTransient<SubmissionNotesDataSetBuilder>();
    builder.Services.AddTransient<SubmissionNotesRenderer>();

    // -- Azure Monitor (OpenTelemetry) telemetry ------------------------------
    // Matches the standard App Service setting name (APPLICATIONINSIGHTS_CONNECTION_STRING,
    // top-level, the same key Azure's own extension uses). Skipped when unset (e.g. local dev).
    // OpenTelemetry does not emit the Request-Context app-id response header.
    if (IsAzureMonitorConfigured(builder.Configuration))
    {
        // Serilog forwards to the OpenTelemetry provider (writeToProviders), so the default
        // providers are cleared here to avoid duplicate console output.
        builder.Logging.ClearProviders();
        builder.Services.AddOpenTelemetry().UseAzureMonitor(o =>
            o.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
    }

    var app = builder.Build();

    // -- Middleware pipeline --------------------------------------------------
    app.UseSecurityHeaders(sendHsts: !app.Environment.IsDevelopment());

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        // GDS "There is a problem with the service" for all unhandled 5xx. Explicitly tracks
        // the exception (with URL/user context) before redirecting, so a genuinely unhandled
        // failure is fully visible in Application Insights rather than just an opaque TraceId.
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(context =>
            {
                var error = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
                if (error is not null)
                {
                    var telemetry = context.RequestServices.GetRequiredService<TelemetryHelper>();
                    telemetry.TrackException(error, "GlobalExceptionHandler", new Dictionary<string, string>
                    {
                        ["url"] = context.Request.Path.Value ?? string.Empty,
                    });
                }
                context.Response.Redirect("/Errors/ServiceProblem");
                return Task.CompletedTask;
            });
        });
        app.UseStatusCodePagesWithReExecute("/Errors/ServiceProblem");
    }

    app.UseSerilogRequestLogging();

    // Pins date/number formatting to en-GB (dd/MM/yyyy) regardless of the hosting OS/container's
    // default culture — every ToShortDateString()/plain ToString() call across the app (grids,
    // summaries) otherwise renders MM/dd/yyyy on any host whose default culture isn't en-GB, even
    // though it renders correctly dd/MM/yyyy on a UK-locale dev machine. This is a UK government
    // service, so en-GB is the only supported culture — not driven by the browser's Accept-Language.
    var enGb = new[] { new System.Globalization.CultureInfo("en-GB") };
    app.UseRequestLocalization(new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture("en-GB"),
        SupportedCultures = enGb,
        SupportedUICultures = enGb,
    });

    app.UseForwardedHeaders();  // must run before UseHttpsRedirection/UseAuthentication
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseSession();
    app.UseAuthentication();

    // The auth cookie can outlive the server-side session (restart, idle expiry). A request that
    // arrives signed in with a session cookie but no marker means the session expired -> timeout page.
    // Static files are served earlier by UseStaticFiles and non-HTML requests are ignored.
    string[] sessionExpirySkippedPaths = ["/health", "/auth", "/signin-oidc", "/signout-oidc", "/signout-callback-oidc", "/Errors", "/AccessDenied", "/SessionError", "/signin"];
    app.Use(async (context, next) =>
    {
        if (context.User.Identity?.IsAuthenticated == true
            && context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase)
            && !sessionExpirySkippedPaths.Any(p => context.Request.Path.StartsWithSegments(p)))
        {
            await context.Session.LoadAsync();
            if (context.Session.GetString(SessionMarkerKey) is null)
            {
                context.Session.SetString(SessionMarkerKey, "1");
                if (context.Request.Cookies.ContainsKey(SessionCookieName))
                {
                    context.Response.Redirect("/SessionError");
                    return;
                }
            }
        }

        await next();
    });

    app.UseAuthorization();

    app.MapHealthChecks("/health");
    app.MapControllers(); // AuthController — /auth/* endpoints
    app.MapRazorPages();

    // Re-sign-in after a timeout: drop the stale cookies, then challenge the cookie scheme, which
    // routes to /auth/login with a return to the home page (locally the dev bypass skips this).
    app.MapGet("/signin", (HttpContext context) =>
    {
        context.Response.Cookies.Delete(AuthCookieName);
        context.Response.Cookies.Delete(SessionCookieName);

        if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("DevAuth:BypassEntraId"))
            return Results.Redirect("/Index");

        return Results.Challenge(new AuthenticationProperties { RedirectUri = "/Index" }, [CookieAuthenticationDefaults.AuthenticationScheme]);
    }).AllowAnonymous();

    // Renamed to /Submissions/SampleSummary — "Block" is meaningless for Wet Tissue submissions.
    app.MapGet("/Submissions/BatchBlockSummary", (HttpRequest request) =>
        Results.Redirect($"/Submissions/SampleSummary{request.QueryString}", permanent: true));

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Histo.Web terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static bool IsAzureMonitorConfigured(IConfiguration configuration)
{
    var connectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    return !string.IsNullOrWhiteSpace(connectionString) && !connectionString.StartsWith("__", StringComparison.Ordinal);
}
