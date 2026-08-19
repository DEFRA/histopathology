using Dapper;
// Modular monolith — each module registers its own internals via extension method
using Histo.Administration;
using Histo.AuditLog;
using Histo.Histology;
using Histo.Infrastructure;
using Histo.QualityControl;
using Histo.Submissions;
using Histo.Web.Services;
using Serilog;

using Histo.Administration.Interfaces;
using Histo.Administration.Repositories;
using Histo.Administration.Services;
using Histo.AuditLog.Interfaces;
using Histo.AuditLog.Repositories;
using Histo.AuditLog.Services;
using Histo.Histology.Interfaces;
using Histo.Histology.Repositories;
using Histo.Histology.Services;
using Histo.QualityControl.Interfaces;
using Histo.QualityControl.Repositories;
using Histo.QualityControl.Services;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Repositories;
using Histo.Submissions.Services;
using Histo.Reporting.Reports;
using Histo.Reporting.Services;

// Bootstrap Serilog before the host is built so startup errors are captured.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

// Register Dapper type handler for DateTime? â€” handles legacy stored procedures that
// return date columns as CONVERT(VARCHAR, col, 103) strings (dd/MM/yyyy format).
// Must be called before any Dapper query executes.
SqlMapper.AddTypeHandler(new NullableDateTimeTypeHandler());
// Map the audit log SP column "DateTime" â†’ AuditLogEntry.ChangedAt
AuditLogDapperSetup.RegisterTypeMaps();

try
{
    Log.Information("Starting Histo.Web");

    var builder = WebApplication.CreateBuilder(args);

    // â”€â”€ Serilog â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .WriteTo.Console());

    // â”€â”€ Strongly-typed options â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    builder.Services.Configure<AppOptions>(
        builder.Configuration.GetSection(AppOptions.SectionName));

    // â”€â”€ DB connection factory â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Connection string comes from ConnectionStrings:HistologyDb in appsettings.json
    // (Key Vault reference in non-dev environments â€” never a plaintext password here).
    var connectionString = builder.Configuration.GetConnectionString("HistologyDb")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:HistologyDb is not configured. " +
            "Set it in appsettings.json (dev) or via App Service configuration (non-dev).");

    builder.Services.AddSingleton<IDbConnectionFactory>(
        new SqlConnectionFactory(connectionString));

    // â”€â”€ Logger adapter (wraps Microsoft.Extensions.Logging for domain services) â”€â”€
    // IAppLogger is a non-generic interface; register via ILoggerFactory so every
    // service injection site gets a logger whose category is the declaring service type.
    // Using a factory delegate ensures the ILoggerFactory is resolved from the
    // built container at call-time, not before services are fully initialised.
    builder.Services.AddTransient<IAppLogger>(sp =>
    {
        var factory = sp.GetRequiredService<ILoggerFactory>();
        return new AppLogger<IAppLogger>(factory.CreateLogger<IAppLogger>());
    });

    // â”€â”€ Health checks â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    builder.Services.AddHealthChecks();
    // TODO Phase 1+: add SQL health check:
    //   .AddSqlServer(connectionString, name: "histology-db", tags: ["db"]);

    // â”€â”€ Razor Pages â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    builder.Services.AddRazorPages();

    // â”€â”€ Session â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(o =>
    {
        o.IdleTimeout     = TimeSpan.FromMinutes(30);
        o.Cookie.HttpOnly = true;
        o.Cookie.IsEssential = true;
    });
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ISessionService, SessionService>();

    // â”€â”€ Administration module â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    builder.Services.AddAdministrationModule();

    // â”€â”€ AuditLog module â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    builder.Services.AddAuditLogModule();

    // â”€â”€ Histology module â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    builder.Services.AddHistologyModule();

    // â”€â”€ QualityControl module â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    builder.Services.AddQualityControlModule();

    // â”€â”€ Submissions module â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    builder.Services.AddSubmissionsModule();
    builder.Services.AddScoped<IBatchRepository, BatchRepository>();
    builder.Services.AddScoped<BatchService>();
    builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
    builder.Services.AddScoped<SubmissionService>();

    // â”€â”€ Reporting module â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    builder.Services.AddTransient<HistologyReportDataSetBuilder>();
    builder.Services.AddTransient<HistologyReportRenderer>();
    builder.Services.AddTransient<QCNoteDataSetBuilder>();
    builder.Services.AddTransient<QCNoteRenderer>();
    builder.Services.AddTransient<SubmissionNotesDataSetBuilder>();
    builder.Services.AddTransient<SubmissionNotesRenderer>();

    // â”€â”€ Application Insights telemetry â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    var aiConnString = builder.Configuration["AppSettings:ApplicationInsightsConnectionString"];
    if (!string.IsNullOrWhiteSpace(aiConnString))
        builder.Services.AddApplicationInsightsTelemetry(o =>
            o.ConnectionString = aiConnString);

    var app = builder.Build();

    // â”€â”€ Middleware pipeline â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    if (!app.Environment.IsDevelopment())
        app.UseExceptionHandler("/Error");

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseSession();

    // Phase 2: app.UseAuthentication(); app.UseAuthorization();
    
    app.UseAuthorization();

    app.MapHealthChecks("/health");
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
