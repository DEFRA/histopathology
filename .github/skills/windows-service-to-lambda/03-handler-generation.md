# Playbook 03: Handler Generation

**Purpose**: Translate Windows Service business logic from Lane 1 (Low) and Lane 2 (Medium)
services into Lambda handler source files. All Windows-specific patterns are replaced with
their Lambda-safe equivalents. Business logic, stored procedure calls, and data-access
patterns are preserved verbatim.

**Input**: `migration-output/migration-plan.json`, original service source files  
**Output**: `migration-output/handlers/<ServiceName>/` per migrated service

> **Lane 3 (High-complexity) services are skipped entirely.** Proceed to Skill 06 for those.

---

## Step 0 — Detect Local .NET SDK Version

Before reading the migration plan, detect the highest .NET SDK installed on the local machine
and derive the target framework moniker (TFM) and Lambda runtime identifier used throughout
this skill.

1. Run:
   ```bash
   dotnet --list-sdks
   ```
2. Parse the output. Each line has the format `<Major>.<Minor>.<Patch> [<path>]`.
3. Select the entry with the **highest major version** (e.g. `10.0.100` → major `10`).
4. Derive values:

   | Detected SDK major | `<HighestSdkTfm>` | `<detectedRuntime>` |
   |--------------------|-------------------|---------------------|
   | 10                 | `net10.0`         | `dotnet10`          |
   | N (future)         | `netN.0`          | `dotnetN`           |

5. Store `<HighestSdkTfm>` and `<detectedRuntime>` as variables. Substitute them in all
   generated `.csproj`, `aws-lambda-tools-defaults.json`, and README.md files below.
6. If no .NET SDK is installed (command not found), default to `net10.0` / `dotnet10` and emit
   a `⚠ WARNING: dotnet SDK not detected — defaulting to dotnet10. Verify before deploying.`
   comment in all generated files.

---

## Step 1 — Read the Migration Plan

1. Load `migration-output/migration-plan.json`.
2. Extract `servicePlans` where `lane` is `1` or `2`.
3. For each service, read:
   - `runtime` (`dotnet10`)
   - `handler` string (for dotnet runtimes only)
   - `timeoutSeconds`
   - `memorySizeMb`
   - `secretReferences` list
4. Locate the original service source files using the project paths from `service-inventory.json`.

---

## Step 2 — Apply Pattern Mapping

Before writing any handler code, map every Windows pattern found in the original service to
its Lambda equivalent. Use the full mapping table in
[references/pattern-mapping.md](./references/pattern-mapping.md).

Key replacements applied in this skill:

### Configuration replacement

```csharp
// ORIGINAL (.NET Framework 4.x)
var connStr = ConfigurationManager.ConnectionStrings["MainDbConnection"].ConnectionString;
var batchSize = int.Parse(ConfigurationManager.AppSettings["BatchSize"]);

// MIGRATED (Lambda handler)
// MIGRATED FROM: ConfigurationManager.ConnectionStrings["MainDbConnection"]
var connStr = Environment.GetEnvironmentVariable("MAIN_DB_CONNECTION");
// MIGRATED FROM: ConfigurationManager.AppSettings["BatchSize"]
var batchSize = int.Parse(Environment.GetEnvironmentVariable("BATCH_SIZE") ?? "100");
```

### EventLog replacement

```csharp
// ORIGINAL
EventLog.WriteEntry("BatchProcessor", "Starting batch run", EventLogEntryType.Information);

// MIGRATED
// MIGRATED FROM: EventLog.WriteEntry("BatchProcessor", ...)
context.Logger.LogInformation("Starting batch run");
```

### Timer entry point replacement

```csharp
// ORIGINAL — OnTimer callback in Windows Service
private void OnTimerElapsed(object sender, ElapsedEventArgs e)
{
    ProcessBatch();
}

// MIGRATED — Lambda handler entry point (dotnet10)
// MIGRATED FROM: OnTimerElapsed callback (System.Timers.Timer, 300000ms interval)
// Trigger: EventBridge Scheduler rate(5 minutes)
public async Task FunctionHandler(ScheduledEvent evnt, ILambdaContext context)
{
    context.Logger.LogInformation($"Lambda invoked at {evnt.Time:O}");
    await ProcessBatchAsync(context);
}
```

---

## Step 3 — Generate Handler Files (dotnet10 runtime)

For each Lane 1 / Lane 2 service with `runtime: "dotnet10"`:

### 3.1 — Project File

Create `migration-output/handlers/<ServiceName>/<ServiceName>.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- MIGRATED FROM: <TargetFrameworkVersion>v4.X</TargetFrameworkVersion> in <ServiceName>.csproj -->
    <!-- <HighestSdkTfm> is detected in Step 0 (e.g. net10.0 when the highest installed SDK is 10.x) -->
    <TargetFramework><HighestSdkTfm></TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>
    <AssemblyName><ServiceName></AssemblyName>
    <RootNamespace><ServiceName></RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Amazon.Lambda.Core" Version="2.2.0" />
    <PackageReference Include="Amazon.Lambda.Serialization.SystemTextJson" Version="2.4.1" />
    <PackageReference Include="Amazon.Lambda.CloudWatchEvents.ScheduledEvents" Version="2.1.0" />
    <!-- Include AWSSDK packages for any AWS services used by the handler -->
    <PackageReference Include="AWSSDK.SecretsManager" Version="3.7.*" />
    <!-- Preserve original data-access package -->
    <PackageReference Include="System.Data.SqlClient" Version="4.8.6" />
    <!-- Add further packages from original packages.config here -->
  </ItemGroup>
</Project>
```

### 3.2 — aws-lambda-tools-defaults.json

```json
{
  "profile": "",
  "region": "<awsRegion from migration-plan.json>",
  "configuration": "Release",
  "function-runtime": "<detectedRuntime>",
  "function-memory-size": <memorySizeMb>,
  "function-timeout": <timeoutSeconds>,
  "function-handler": "<handler from migration-plan.json>"
}
```

### 3.3 — Function.cs

Structure:

```csharp
using Amazon.Lambda.Core;
using Amazon.Lambda.CloudWatchEvents.ScheduledEvents;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Data.SqlClient;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace <ServiceName>;

/// <summary>
/// Lambda handler migrated from Windows Service: <OriginalServiceClass>
/// Original trigger: <OriginalTriggerDescription>
/// Migrated trigger: EventBridge Scheduler <scheduleExpression>
/// Migration date: <YYYY-MM-DD>
/// </summary>
public class Function
{
    // MIGRATED FROM: Static fields / fields initialised in OnStart()
    private readonly IAmazonSecretsManager _secretsManager;

    // MIGRATED FROM: Constructor / ServiceBase constructor
    public Function()
    {
        _secretsManager = new AmazonSecretsManagerClient();
    }

    // MIGRATED FROM: OnTimerElapsed / OnStart background thread
    // Trigger: EventBridge Scheduler <scheduleExpression>
    public async Task FunctionHandler(ScheduledEvent evnt, ILambdaContext context)
    {
        context.Logger.LogInformation($"[<ServiceName>] Invoked at {evnt.Time:O}");

        // MIGRATED FROM: ConfigurationManager.ConnectionStrings["MainDbConnection"]
        var connStr = await GetSecretAsync("<secretPath>", context);

        await RunServiceLogicAsync(connStr, context);
    }

    // ─── MIGRATED BUSINESS LOGIC ─────────────────────────────────────────────
    // All methods below are direct translations of the original service logic.
    // Stored procedure names, parameter names, and SQL text are unchanged.
    // Windows-specific APIs have been replaced; see MIGRATED FROM comments.

    private async Task RunServiceLogicAsync(string connStr, ILambdaContext context)
    {
        // MIGRATED FROM: <OriginalMethodName> in <OriginalFile>.cs
        // [paste translated business logic here]
        await Task.CompletedTask;
    }

    // ─── INFRASTRUCTURE HELPERS ──────────────────────────────────────────────

    private async Task<string> GetSecretAsync(string secretId, ILambdaContext context)
    {
        try
        {
            var request = new GetSecretValueRequest { SecretId = secretId };
            var response = await _secretsManager.GetSecretValueAsync(request);
            return response.SecretString;
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Failed to retrieve secret '{secretId}': {ex.Message}");
            throw;
        }
    }
}
```

> **Critical rules for business logic translation:**
> - Copy all `private` / `internal` methods that are called from `OnStart` or the timer callback.
> - Preserve stored procedure names, parameter names, and SQL command text **exactly** as in the original.
> - Replace `SqlConnection(connectionString)` literal with `SqlConnection(connStr)` (variable).
> - Replace `Thread.Sleep(ms)` with `await Task.Delay(ms)` where delay is still required (e.g. throttle avoidance).
> - Replace `EventLog.WriteEntry(...)` with `context.Logger.LogInformation(...)`.
> - Remove all `ServiceBase`, `OnStart`, `OnStop`, `ProjectInstaller` code entirely.
> - Do **not** introduce any behavioural change. If a pattern cannot be safely translated, flag it and leave a `// TODO: MANUAL REVIEW REQUIRED — <reason>` comment.

### 3.4 — README.md

Create `migration-output/handlers/<ServiceName>/README.md`:

```markdown
# <ServiceName> — AWS Lambda Handler

Migrated from Windows Service: `<OriginalServiceClass>`  
Migration date: <YYYY-MM-DD>  
Source framework: .NET Framework <SourceFramework>  
Target runtime: <HighestSdkTfm> (`<detectedRuntime>`)

## Overview

This Lambda function replaces the `<OriginalServiceClass>` Windows Service.
It is triggered by an **EventBridge Scheduler** rule on schedule `<scheduleExpression>`
(originally a `System.Timers.Timer` at `<originalIntervalMs>` ms).

## Environment Variables

| Variable | Description | Source |
|----------|-------------|--------|
| `DB_CONNECTION_SECRET_ID` | Secrets Manager secret ID whose value is the SQL Server connection string | Secrets Manager: `<secretReference>` |
| `DEFAULT_BATCH_SIZE` | Max records per stored-procedure call (default: 500) | Lambda environment variable |

> **Never set `DB_CONNECTION_SECRET_ID` to a plaintext connection string.**
> Set it to the ARN or name of the Secrets Manager secret that holds the connection string.

## Build

```bash
dotnet build <ServiceName>.csproj --configuration Release
```

## Publish (for Lambda deployment)

```bash
dotnet lambda package --configuration Release --output-package bin/Release/<HighestSdkTfm>/<ServiceName>.zip
```

## Local Testing

```bash
dotnet test ../../tests/<ServiceName>.Tests/<ServiceName>.Tests.csproj --source "https://api.nuget.org/v3/index.json"
```

## Timeout Risk

⚠ This function contains an unbounded batch-drain loop (`while (true)`).
The Lambda hard limit is 900 seconds. If the deletion backlog cannot drain
within 15 minutes, the invocation will be terminated mid-run.
**Review the `TODO: MANUAL REVIEW REQUIRED — TIMEOUT RISK` comment in
`<ServiceName>.cs` and implement SQS pagination or Step Functions chunking.**

## Pattern Mapping Applied

| Original (Windows Service) | Lambda Equivalent |
|----------------------------|-------------------|
| `System.Timers.Timer` (30 min) | EventBridge Scheduler `rate(30 minutes)` |
| `EventLog.WriteEntry(...)` | `ILambdaLogger.LogInformation/LogError` → CloudWatch Logs |
| `ConfigurationManager.ConnectionStrings["ProficiencyTestingDb"]` | `Environment.GetEnvironmentVariable("DB_CONNECTION_SECRET_ID")` → Secrets Manager |
| `ConfigurationManager.AppSettings["DefaultBatchSize"]` | `Environment.GetEnvironmentVariable("DEFAULT_BATCH_SIZE")` |
| `System.ServiceProcess.ServiceBase` (SCM host) | Removed — replaced by Lambda entry point |
| Dry-run log file (relative path) | `/tmp/<filename>` (`Path.GetTempPath()`) |
```

---

## Step 4 — Validate Generated Handlers

After generating all handler files, perform a structural check:

**dotnet handlers**:
```bash
dotnet build migration-output/handlers/<ServiceName>/<ServiceName>.csproj --configuration Release
```
Resolve any build errors before proceeding. Cap at 3 fix attempts; surface remaining errors
as blocking issues in the migration report.

**Checks for all handlers**:
- No `backslash` file paths in generated source
- No hardcoded connection string values (only `Environment.GetEnvironmentVariable` / `_get_secret`)
- No `ServiceBase`, `ProjectInstaller`, or `System.ServiceProcess` imports remain
- Every `// TODO: MANUAL REVIEW REQUIRED` comment is catalogued for the migration report
- `MIGRATED FROM:` comments present on every translated line

**Decision gate (Lane 2 only)**: Present generated handlers to the developer for review.
Confirm approval before IaC generation proceeds for Lane 2 services.
