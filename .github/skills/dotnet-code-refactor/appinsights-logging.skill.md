---
name: appinsights-logging
description: Inject Application Insights telemetry for observability which includes log context not personal information. Targets C# .NET 10 / ASP.NET Core.
---
 
# Skill: appinsights-logging
 
## Purpose
Replace all legacy and inconsistent logging mechanisms in migrated C# `.NET 10` / ASP.NET Core source code with Application Insights structured telemetry. Installs the required NuGet packages, wires the `TelemetryClient` through a shared `TelemetryHelper.cs` (DI-registered scoped service), and applies consistent log levels, contextual properties, and operation tracking across the entire codebase.
 
## Trigger Conditions
- Called after `global-exception-handling` has wired the `UseExceptionHandler` middleware and domain exceptions (`BSESystemExceptions.cs`) are in place.
- The exception types introduced by `global-exception-handling` are referenced in `TrackException` calls placed by this skill.
 
---
 
## Step 1 — Install NuGet Packages
 
Add the following packages to every web project's SDK-style `.csproj` as `<PackageReference>` items:
<ItemGroup>
  <PackageReference Include="Microsoft.ApplicationInsights" Version="2.22.0" />
  <PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.22.0" />
</ItemGroup>
```

> `Microsoft.ApplicationInsights.Web` and `Microsoft.ApplicationInsights.WindowsServer` are classic `System.Web`-based packages and are **not compatible with .NET 10**. Use `Microsoft.ApplicationInsights.AspNetCore` instead.

Run `dotnet restore` after adding the package references.
 
## Step 2 — Configure Application Insights in appsettings.json and Program.cs

`ApplicationInsights.config` is a classic SDK pattern and is **not used in .NET 5+**. Configuration is done via `appsettings.json` and service registration in `Program.cs`.

**appsettings.json** — add the Application Insights connection string placeholder (value supplied via Key Vault at deploy time):

```json
{
  "ApplicationInsights": {
    "ConnectionString": "__APP_INSIGHTS_CONNECTION_STRING__"
  }
}
```

**Program.cs** — register Application Insights telemetry and the `TelemetryHelper` in the DI container:

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TelemetryHelper>();
```

> Never hardcode the connection string — use a Key Vault reference via Azure App Service application settings (`APPLICATIONINSIGHTS_CONNECTION_STRING`). Do not use the legacy `InstrumentationKey` — it is deprecated; use `ConnectionString` only.
 
---
 
## Step 3 — Create Shared TelemetryHelper

Create `BSESystem\Logging\TelemetryHelper.cs`. The helper is DI-registered (scoped) — it receives `TelemetryClient` and `IHttpContextAccessor` via constructor injection. Never create `TelemetryClient` instances directly in application code.







```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace BSESystem.Logging;

/// <summary>
/// Application-wide telemetry wrapper. Registered as a scoped service in Program.cs.
/// All logging in the application must go through this class.
/// </summary>
public class TelemetryHelper
{
    private readonly TelemetryClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TelemetryHelper(TelemetryClient client, IHttpContextAccessor httpContextAccessor)
    {
        _client = client;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>Log an exception with structured context properties.</summary>
    public void TrackException(Exception ex, string operation,
        Dictionary<string, string>? additionalProperties = null)
    {
        var props = new Dictionary<string, string>
        {
            ["operation"] = operation,
            ["appVersion"] = GetType().Assembly.GetName().Version?.ToString() ?? "unknown",
            ["userId"] = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "anonymous",
            ["sessionId"] = _httpContextAccessor.HttpContext?.Session?.Id ?? string.Empty,
            ["pageUrl"] = _httpContextAccessor.HttpContext?.Request?.Path.Value ?? string.Empty
        };
        if (additionalProperties is not null)
            foreach (var kvp in additionalProperties)
                props[kvp.Key] = kvp.Value;
        _client.TrackException(ex, props);
    }

    /// <summary>Log a diagnostic trace message.</summary>
    public void TrackTrace(string message, SeverityLevel level, string? operation = null)
    {
        var props = new Dictionary<string, string>();
        if (operation is not null) props["operation"] = operation;
        _client.TrackTrace(message, level, props);
    }

    /// <summary>Log a named business event.</summary>
    public void TrackEvent(string eventName,
        Dictionary<string, string>? properties = null)
        => _client.TrackEvent(eventName, properties);

    /// <summary>Flush all pending telemetry.</summary>
    public void Flush() => _client.Flush();
}
```
| Legacy Pattern | New Call | Level | Notes |
|---|---|---|---|
| `EventLog.WriteEntry(..., Error)` / `EventLogEntryType.Error` | `TelemetryHelper.TrackException(ex, ...)` | Error | Wrap in suppressing try/catch (EventLog unavailable on App Service) |
| `EventLog.WriteEntry(..., Warning)` / `EventLogEntryType.Warning` | `TelemetryHelper.TrackTrace(msg, SeverityLevel.Warning, ...)` | Warning | |
| `EventLog.WriteEntry(..., Information)` | `TelemetryHelper.TrackTrace(msg, SeverityLevel.Information, ...)` | Information | |
| `Debug.Print(...)` / `Debug.WriteLine(...)` | `TelemetryHelper.TrackTrace(msg, SeverityLevel.Verbose, ...)` | Verbose | Remove from production; guard with `#If DEBUG` or remove entirely |
| `Console.WriteLine(...)` (diagnostic) | `TelemetryHelper.TrackTrace(msg, SeverityLevel.Information, ...)` | Information | |
| `Response.Write(...)` (debug output) | Remove entirely + `TelemetryHelper.TrackTrace(msg, SeverityLevel.Warning, ...)` | Warning | Never write debug output to HTTP response in production |
| Business event tracking (user action, workflow step) | `TelemetryHelper.TrackEvent(eventName, properties)` | — | |
 
### Level Discipline Rules
- **Verbose**: developer diagnostics only — never fire in production by default.
- **Information**: normal start/end of significant operations (workflow initiated, record saved).
- **Warning**: recoverable unexpected state (retry triggered, fallback value used, deprecated path taken).
- **Error**: operation failed and was not recovered; an exception was thrown.
- **Critical**: application startup failure, security breach detected, data corruption — rare; treated as an incident trigger.
 
---
 
## Step 5 — Add Contextual Properties
 
Every `TrackException` and significant `TrackTrace` call must include at minimum:
 
| Property | Source | Notes |
|---|---|---|
| `operation` | Enclosing method name | `NameOf(MethodName)` |
| `userId` | `_httpContextAccessor.HttpContext?.User?.Identity?.Name` | Non-PII identifier; do not log full UPN |
| `sessionId` | `_httpContextAccessor.HttpContext?.Session?.Id` | For session-correlated error diagnosis |
| `pageUrl` | `_httpContextAccessor.HttpContext?.Request?.Path.Value` | Path only — no query string if it contains tokens |

> `HttpContext.Current` is not available in ASP.NET Core. Access `HttpContext` via the injected `IHttpContextAccessor` in `TelemetryHelper` — these properties are already captured automatically inside `TrackException` (see Step 3).
 
---
 
## Step 6 — Wire Flush on Application Shutdown

`Global.asax.vb` `Application_End` is not processed in .NET 10. Wire the telemetry flush to `IHostApplicationLifetime.ApplicationStopped` in `Program.cs`:

```csharp
// Program.cs
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopped.Register(() =>
{
    var telemetry = app.Services.GetRequiredService<TelemetryClient>();
    telemetry.Flush();
    Task.Delay(TimeSpan.FromSeconds(5)).Wait(); // Allow time for flush to complete
});
```
 
---
 
## Step 7 — Replace EventLog.WriteEntry in Catch Blocks

Where `EventLog.WriteEntry` appears inside a `Catch`/`catch` block, replace it with a `TelemetryHelper` call.





// After
catch (Exception ex)
{
    _telemetryHelper.TrackException(ex, nameof(ProcessRecord),
        new Dictionary<string, string> { ["recordId"] = recordId.ToString() });
    throw;
}
```

> The suppressing try/catch around `EventLog.WriteEntry` is only needed during phased rollout. Once all `EventLog` calls are replaced, no suppressing wrapper is required.
 
---
 
## Outputs
 
| Output | Description |
|---|---|
| `BSESystem\Logging\TelemetryHelper.cs` | Shared telemetry wrapper class (DI-registered scoped service) |
| `appsettings.json` | Updated with `ApplicationInsights.ConnectionString` placeholder |
| `docs\code-refactor\logging-enhancement-report.json` | Structured log: legacy calls replaced, NuGet packages added, files modified |
| All modified `.cs` files | Updated in place with `_telemetryHelper` instance calls |
- **Never** use `TrackException` for expected business exceptions (e.g., `ValidationException` raised by user input) — use `TrackEvent` or `TrackTrace(Warning)` for expected paths.
- Do **not** add `#Disable Warning` or `#pragma warning disable` directives to suppress compiler warnings about replaced `EventLog` calls — fix them.
- Do **not** use `Response.Write` for any diagnostic output — ever.
 
 --