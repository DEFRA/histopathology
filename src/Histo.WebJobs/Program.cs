using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Histo.WebJobs;

// Plain console app scheduled by Kudu's built-in WebJob scheduler via settings.job
// (Triggered WebJob) rather than the Azure WebJobs SDK's [TimerTrigger] host — this
// process runs once per invocation and exits, so no AzureWebJobsStorage account is required.
var builder = Host.CreateApplicationBuilder(args);

// Single appsettings.json, same as Histo.Web: real per-environment values come from
// Azure DevOps-injected App Service Application Settings (env vars), not per-env json files.
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddUserSecrets<HistologyResetJob>(optional: true);

builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Same APPLICATIONINSIGHTS_CONNECTION_STRING key/value as Histo.Web — shared App Service setting.
var aiConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(aiConnectionString) && !aiConnectionString.StartsWith("__", StringComparison.Ordinal))
{
    builder.Services.AddApplicationInsightsTelemetryWorkerService(options =>
    {
        options.ConnectionString = aiConnectionString;
    });
}

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("HistologyResetJob");

// The actual trigger schedule is defined in settings.job (read by Kudu's WebJob scheduler), which
// is auto-generated at build/publish time from this same "WebJob:Schedule" value — see
// GenerateSettingsJob.ps1. Logged here purely for traceability of the configured value at runtime.
var configuredSchedule = host.Services.GetRequiredService<IConfiguration>()["WebJob:Schedule"];
logger.LogInformation("Configured WebJob schedule: {Schedule}", configuredSchedule ?? "(not set)");

var job = new HistologyResetJob(host.Services.GetRequiredService<IConfiguration>(), logger);

var succeeded = await job.RunAsync();

// Flush Application Insights telemetry before the process exits (no background host keeps it alive).
var telemetryClient = host.Services.GetService<Microsoft.ApplicationInsights.TelemetryClient>();
if (telemetryClient is not null)
{
    telemetryClient.Flush();
    await Task.Delay(TimeSpan.FromSeconds(2));
}

// Non-zero exit code marks the Triggered WebJob run as "Failed" in the Azure Portal/Kudu.
return succeeded ? 0 : 1;
