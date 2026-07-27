using Serilog;
using Histo.Infrastructure;
using Microsoft.Extensions.Options;

// Bootstrap Serilog before the host is built so startup errors are captured.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Histo.Web");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ─────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .WriteTo.Console());

    // ── Strongly-typed options ───────────────────────────────────────────────
    builder.Services.Configure<AppOptions>(
        builder.Configuration.GetSection(AppOptions.SectionName));

    // ── DB connection factory ────────────────────────────────────────────────
    // Connection string comes from ConnectionStrings:HistologyDb in appsettings.json
    // (Key Vault reference in non-dev environments — never a plaintext password here).
    var connectionString = builder.Configuration.GetConnectionString("HistologyDb")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:HistologyDb is not configured. " +
            "Set it in appsettings.json (dev) or via App Service configuration (non-dev).");

    builder.Services.AddSingleton<IDbConnectionFactory>(
        new SqlConnectionFactory(connectionString));

    // ── Health checks ────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks();
    // TODO Phase 1+: add SQL health check:
    //   .AddSqlServer(connectionString, name: "histology-db", tags: ["db"]);

    // ── Razor Pages ──────────────────────────────────────────────────────────
    builder.Services.AddRazorPages();

    // ── Application Insights telemetry ──────────────────────────────────────
    var aiConnString = builder.Configuration["AppSettings:ApplicationInsightsConnectionString"];
    if (!string.IsNullOrWhiteSpace(aiConnString))
        builder.Services.AddApplicationInsightsTelemetry(o =>
            o.ConnectionString = aiConnString);

    var app = builder.Build();

    // ── Middleware pipeline ──────────────────────────────────────────────────
    if (!app.Environment.IsDevelopment())
        app.UseExceptionHandler("/Error");

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    // Phase 2: app.UseAuthentication(); app.UseAuthorization();

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
