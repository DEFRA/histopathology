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
using Histo.Web.Services;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.MvcCore.Configuration;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
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
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .WriteTo.Console());

    // -- Strongly-typed options -----------------------------------------------
    builder.Services.Configure<AppOptions>(
        builder.Configuration.GetSection(AppOptions.SectionName));

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
        options.KnownNetworks.Clear();
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
        return new AppLogger<IAppLogger>(factory.CreateLogger<IAppLogger>());
    });

    // -- Health checks --------------------------------------------------------
    builder.Services.AddHealthChecks();
    // TODO Phase 1+: add SQL health check:
    //   .AddSqlServer(connectionString, name: "histology-db", tags: ["db"]);

    // -- Razor Pages + MVC controllers (controllers needed for SAML2 endpoints) --
    builder.Services.AddRazorPages()
                    .AddSessionStateTempDataProvider();
    builder.Services.AddControllers(); // AuthController (SAML2 protocol endpoints)
    builder.Services.AddHttpClient();  // required for async IdP metadata loading post-build

    // -- Entra ID SAML2 authentication (Phase 1 — replaces ADR-006 bridge) -----
    // Library: ITfoxtec.Identity.Saml2.MvcCore v4.20.1
    // See: ENTRA-REGISTRATION.md for app registration checklist.
    // See: docs/EntraID-Implementation-plan.md Phase B for migration steps.
    var saml2Section = builder.Configuration.GetSection("Saml2");

    var saml2Config = new Saml2Configuration
    {
        Issuer             = saml2Section["SPEntityId"] ?? string.Empty,
        SignatureAlgorithm = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256",
        CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.ChainTrust,
        RevocationMode            = X509RevocationMode.NoCheck,
    };

    // SP signing certificate (optional in dev; required in all deployed environments).
    var spCertThumbprint = saml2Section["SPCertificateThumbprint"];
    if (!string.IsNullOrEmpty(spCertThumbprint))
    {
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);
        var cert = store.Certificates
            .Find(X509FindType.FindByThumbprint, spCertThumbprint, false)
            .OfType<X509Certificate2>()
            .FirstOrDefault();
        if (cert is not null)
            saml2Config.SigningCertificate = cert;
        else
            Log.Warning("SP signing certificate with thumbprint {Thumbprint} was not found in the certificate store.", spCertThumbprint);
    }
    else
    {
        Log.Warning("Saml2:SPCertificateThumbprint is not configured — SP will not sign AuthnRequests. Required for all non-development environments.");
    }

    builder.Services.AddSingleton(saml2Config); // saml2Config ref captured for post-build metadata load below

    builder.Services.AddSaml2(loginPath: "/Saml2/login", slidingExpiration: true, accessDeniedPath: "/AccessDenied");

    // Claims transformation — fallback DB lookup; normal path bakes claims in AuthController at ACS time.
    builder.Services.AddScoped<IClaimsTransformation, HistopathologyClaimsTransformation>();

    // -- Session --------------------------------------------------------------
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(o =>
    {
        o.IdleTimeout = TimeSpan.FromMinutes(30);
        o.Cookie.HttpOnly = true;
        o.Cookie.IsEssential = true;
    });
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
    builder.Services.AddTransient<HistologyReportRenderer>();
    builder.Services.AddTransient<QCNoteDataSetBuilder>();
    builder.Services.AddTransient<QCNoteRenderer>();
    builder.Services.AddTransient<SubmissionNotesDataSetBuilder>();
    builder.Services.AddTransient<SubmissionNotesRenderer>();

    // -- Application Insights telemetry --------------------------------------
    var aiConnString = builder.Configuration["AppSettings:ApplicationInsightsConnectionString"];
    if (!string.IsNullOrWhiteSpace(aiConnString))
        builder.Services.AddApplicationInsightsTelemetry(o =>
            o.ConnectionString = aiConnString);

    var app = builder.Build();

    // -- Load IdP federation metadata (async — requires IHttpClientFactory from built container) --
    // saml2Config is the same singleton object already registered; updating it here is safe
    // because no requests are served until app.Run() is called.
    // IdPMetadataUrl format: https://login.microsoftonline.com/{tenant-id}/federationmetadata/2007-06/federationmetadata.xml?appid={app-id}
    var idpMetadataUrl = app.Configuration.GetSection("Saml2")["IdPMetadataUrl"];
    if (!string.IsNullOrEmpty(idpMetadataUrl) && Uri.TryCreate(idpMetadataUrl, UriKind.Absolute, out var metadataUri))
    {
        try
        {
            var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
            var entityDescriptor  = new EntityDescriptor();
            await entityDescriptor.ReadIdPSsoDescriptorFromUrlAsync(httpClientFactory, metadataUri);

            if (entityDescriptor.IdPSsoDescriptor != null)
            {
                saml2Config.SingleSignOnDestination = entityDescriptor.IdPSsoDescriptor.SingleSignOnServices
                    .FirstOrDefault(s => s.Binding.OriginalString.Contains("HTTP-Redirect"))?.Location
                    ?? entityDescriptor.IdPSsoDescriptor.SingleSignOnServices.FirstOrDefault()?.Location;

                saml2Config.SingleLogoutDestination = entityDescriptor.IdPSsoDescriptor.SingleLogoutServices
                    .FirstOrDefault()?.Location;

                saml2Config.SignatureValidationCertificates.AddRange(
                    entityDescriptor.IdPSsoDescriptor.SigningCertificates);

                Log.Information("Loaded SAML2 IdP metadata. SSO: {SsoUrl}. Signing certs: {CertCount}.",
                    saml2Config.SingleSignOnDestination,
                    saml2Config.SignatureValidationCertificates.Count);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not load IdP metadata from {MetadataUrl}. " +
                "Configure Saml2:IdPMetadataUrl with the Entra ID federation metadata URL.", idpMetadataUrl);
        }
    }
    else
    {
        Log.Warning("Saml2:IdPMetadataUrl is not a valid URL. " +
                    "Set it to: https://login.microsoftonline.com/{{tenant-id}}/federationmetadata/2007-06/federationmetadata.xml?appid={{app-id}}");
    }

    // -- Middleware pipeline --------------------------------------------------
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        // GDS "There is a problem with the service" for all unhandled 5xx
        app.UseExceptionHandler("/Errors/ServiceProblem");
        app.UseStatusCodePagesWithReExecute("/Errors/ServiceProblem");
    }

    app.UseSerilogRequestLogging();
    app.UseForwardedHeaders();  // must run before UseHttpsRedirection/UseAuthentication
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseSession();
    app.UseAuthentication();
    app.UseSaml2();         // ITfoxtec SAML2 middleware — must follow UseAuthentication
    app.UseAuthorization();

    app.MapHealthChecks("/health");
    app.MapControllers(); // AuthController — SAML2 /Saml2/* endpoints
    app.MapRazorPages();

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
