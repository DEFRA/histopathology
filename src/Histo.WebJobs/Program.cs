using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = new HostBuilder();

builder
    .ConfigureAppConfiguration((context, config) =>
    {
        // Single appsettings.json, same as Histo.Web: real per-environment values come from
        // Azure DevOps-injected App Service Application Settings (env vars), not per-env json files.
        config
            .SetBasePath(context.HostingEnvironment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>(optional: true);
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.ClearProviders();
        logging.AddConfiguration(context.Configuration.GetSection("Logging")); // binds Logging:LogLevel from appsettings.json
        logging.AddConsole();
        logging.AddDebug();
    })
    .ConfigureServices((context, services) =>
    {
        // Same APPLICATIONINSIGHTS_CONNECTION_STRING key/value as Histo.Web — shared App Service setting.
        var aiConnectionString = context.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (!string.IsNullOrWhiteSpace(aiConnectionString) && !aiConnectionString.StartsWith("__", StringComparison.Ordinal))
        {
            services.AddApplicationInsightsTelemetryWorkerService(options =>
            {
                options.ConnectionString = aiConnectionString;
            });
        }
    })
    .ConfigureWebJobs(webJobsBuilder =>
    {
        webJobsBuilder.AddTimers();
    });

var host = builder.Build();

await host.RunAsync();
