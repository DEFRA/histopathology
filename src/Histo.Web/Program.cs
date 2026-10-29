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
using Histo.Web;
using Histo.Web.Auth;
using Histo.Web.Security;
using Histo.Web.Services;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.MvcCore.Configuration;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Histo.Web.Telemetry;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Serilog;
using System.Security.Cryptography.X509Certificates;

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
    }, writeToProviders: StartupExtensions.IsAzureMonitorConfigured(builder.Configuration));

    // -- Strongly-typed options -----------------------------------------------
    builder.Services.Configure<AppOptions>(
        builder.Configuration.GetSection(AppOptions.SectionName));

    // One idle timeout drives both the auth cookie and the server-side session.
    var idleTimeout = TimeSpan.FromMinutes(Math.Max(1, builder.Configuration.GetValue("Session:IdleTimeoutMinutes", 20)));

    builder.AddSharedDataProtection();

    // -- Forwarded headers (reverse proxy / edge in front of App Service) -----
    // dev-cde.azure.defra.cloud terminates TLS and forwards to the App Service's
    // default hostname. Without this, Request.Scheme/Request.Host reflect the
    // origin (devcdewebaw1401.azurewebsites.net) rather than the public custom
    // domain, causing absolute redirects (e.g. the SAML cookie challenge) to leak
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
    // A SQL Server health check (name "histology-db", tag "db") is planned for a later phase.

    // -- Razor Pages + MVC controllers (controllers needed for SAML2 endpoints) --
    builder.Services.AddRazorPages(options =>
                    {
                        options.Conventions.AddFolderApplicationModelConvention(
                            "/Admin", model => model.Filters.Add(new Histo.Web.Auth.MaintenanceOnlyPageFilter()));
                        options.Conventions.AddFolderApplicationModelConvention(
                            "/AuditLog", model => model.Filters.Add(new Histo.Web.Auth.MaintenanceOnlyPageFilter()));
                        Histo.Web.Auth.GroupRestrictedPages.Apply(options);
                        foreach (var page in new[] { "/AccessDenied", "/AccessibilityStatement", "/SessionError", "/Errors/ServiceProblem", "/Shared/Error" })
                            options.Conventions.AllowAnonymousToPage(page);
                    })
                    .AddSessionStateTempDataProvider();
    builder.Services.AddControllers(); // AuthController (SAML2 protocol endpoints)
    builder.Services.AddHttpClient();  // required for async IdP metadata loading post-build

    // -- Entra ID SAML2 authentication (Phase 1 — replaces ADR-006 bridge) -----
    // Library: ITfoxtec.Identity.Saml2.MvcCore v4.20.1
    // See: ENTRA-REGISTRATION.md for app registration checklist.
    // See: docs/EntraID-Implementation-plan.md Phase B for migration steps.
    var saml2Section = builder.Configuration.GetSection("Saml2");

    var saml2Config = StartupExtensions.CreateSaml2Configuration(saml2Section);

    builder.Services.AddSingleton(saml2Config); // saml2Config ref captured for post-build metadata load below

    builder.Services.AddSaml2(loginPath: "/Saml2/login", slidingExpiration: true, accessDeniedPath: "/AccessDenied");

    // Challenge via the cookie scheme so an expired cookie reaches OnRedirectToLogin (and the
    // /SessionError page) instead of going straight to Entra ID.
    builder.Services.AddAuthentication(o => o.DefaultChallengeScheme = "saml2");

    // Deny by default: any endpoint without an explicit [AllowAnonymous] requires a signed-in user.
    builder.Services.AddAuthorizationBuilder()
        .SetFallbackPolicy(AuthorizationPolicies.FallbackRequireSignedIn())
        .AddHistoPolicies();

    builder.Services.PostConfigure<CookieAuthenticationOptions>("saml2",
        options => StartupExtensions.ConfigureSamlCookie(options, idleTimeout));

    // Claims transformation — fallback DB lookup; normal path bakes claims in AuthController at ACS time.
    builder.Services.AddScoped<IClaimsTransformation, HistopathologyClaimsTransformation>();

    // -- Session --------------------------------------------------------------
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(o =>
    {
        o.IdleTimeout = idleTimeout;
        o.Cookie.Name = StartupExtensions.SessionCookieName;
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

    builder.AddAzureMonitorIfConfigured();

    var app = builder.Build();

    // -- Load IdP federation metadata (async — requires IHttpClientFactory from built container) --
    // saml2Config is the same singleton object already registered; updating it here is safe
    // because no requests are served until app.Run() is called.
    await StartupExtensions.LoadIdpMetadataAsync(app, saml2Config);

    // -- Middleware pipeline --------------------------------------------------
    app.UseSecurityHeaders(sendHsts: !app.Environment.IsDevelopment());
    app.UseServiceProblemHandling();

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
    app.UseSaml2();         // ITfoxtec SAML2 middleware — must follow UseAuthentication

    app.UseSessionExpiryRedirect();

    app.UseAuthorization();

    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapControllers(); // AuthController — SAML2 /Saml2/* endpoints
    app.MapRazorPages();

    app.MapSignIn();

    // Renamed to /Submissions/SampleSummary — "Block" is meaningless for Wet Tissue submissions.
    app.MapGet("/Submissions/BatchBlockSummary", (HttpRequest request) =>
        Results.Redirect($"/Submissions/SampleSummary{request.QueryString}", permanent: true)).AllowAnonymous();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException && LogFatal(ex))
{
    // Unreachable: LogFatal always returns false so the exception propagates after being logged.
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

static bool LogFatal(Exception ex)
{
    Log.Fatal(ex, "Histo.Web terminated unexpectedly");
    return false;
}
