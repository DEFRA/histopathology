# Playbook 01: Service Inventory & Analysis

**Purpose**: Scan the solution for all Windows Service projects, classify each service's
trigger type, extract data-access patterns and Windows API surface, identify timeout risks,
and produce a structured `service-inventory.json` for use by all subsequent skills.

**Input**: `solutionFolder` (repository root), `sourceFramework` moniker  
**Output**: `migration-output/service-inventory.json`

> This skill is **READ-ONLY**. It does not modify any source files.

---

## Step 1 — Locate Windows Service Projects

**Objective**: Identify every project in the solution that contains a Windows Service.

1. Scan the solution folder recursively for `.csproj` and `.vbproj` files.
2. For each project file, check for any of the following signals indicating a Windows Service:
   - A `<Reference>` to `System.ServiceProcess` in a classic `packages.config` project
   - A `using System.ServiceProcess;` / `Imports System.ServiceProcess` statement in any `.cs` / `.vb` file
   - A class that inherits from `ServiceBase` (pattern: `class X : ServiceBase` or `Class X Inherits ServiceBase`)
   - A `ProjectInstaller` class (inherits `Installer`) — marks the project as a deployable service
3. For each identified service project, record:
   - Project file path (relative to `solutionFolder`)
   - Language (C# or VB.NET)
   - Source framework version (from `<TargetFrameworkVersion>` or `<TargetFramework>`)
   - Assembly name (from `<AssemblyName>`)

**Output**: Preliminary list of service projects with file paths and metadata.

**Decision**: Are all service projects identified? If a project lacks `ServiceBase` but has
a `ProjectInstaller`, flag it for manual review and include it in the inventory.

---

## Step 2 — Extract Entry Points and Trigger Patterns

**Objective**: For each service project, locate `ServiceBase` subclasses and extract the
complete trigger and scheduling configuration.

### 2.1 — ServiceBase Subclass Identification

Locate every class that inherits from `ServiceBase`. For each:
- Record the class name and file path.
- Extract all overridden lifecycle methods: `OnStart`, `OnStop`, `OnPause`, `OnContinue`, `OnCustomCommand`.
- Record the full method bodies — these contain the business logic to be migrated.

### 2.2 — Timer Detection

Scan for timer declarations in the service class and any classes it instantiates from `OnStart`:

| Pattern | How to Detect | Trigger Type |
|---------|--------------|-------------|
| `System.Timers.Timer` | Field declaration + `.Interval` assignment | Fixed-interval timer |
| `System.Threading.Timer` | Constructor call with `dueTime` and `period` parameters | Fixed-interval timer |
| `System.Windows.Forms.Timer` | (unusual in services) Component field | Fixed-interval timer |
| Infinite `while(true)` + `Thread.Sleep(ms)` | Loop with `Thread.Sleep` call | Fixed-interval (derived from sleep duration) |
| `ThreadPool.RegisterWaitForSingleObject` | Call with timeout parameter | Fixed-interval timer |

For each timer found, extract:
- The interval in milliseconds (convert to minutes for EventBridge mapping)
- Whether it fires on start (`AutoReset = false` on first tick) or continuously

### 2.3 — Task Scheduler XML Detection

Scan the repository for Task Scheduler XML files (`.xml` files containing `<Task xmlns=`):
- Extract the `<Triggers>` section: `<CalendarTrigger>`, `<TimeTrigger>`, `<BootTrigger>`
- Map to EventBridge cron expressions
- Note the task's `<Actions>` — confirm it launches the service executable

### 2.4 — Alternative Trigger Detection

| Pattern | Detection Method | Lambda Trigger |
|---------|-----------------|---------------|
| `FileSystemWatcher` field | Class field or `OnStart` instantiation | S3 Event Notification |
| `ServiceController` with named pipe | `System.IO.Pipes` namespace usage | N/A — requires architectural redesign |
| `System.Messaging.MessageQueue` | MSMQ references | SQS Queue Trigger |
| `System.Net.Sockets` listener | Socket binding in `OnStart` | API Gateway / ALB (manual gate) |

Flag any service using sockets or named pipes as **High complexity** regardless of other criteria.

---

## Step 3 — Extract Data-Access Patterns

**Objective**: Map every database interaction in the service so that connection string keys
and stored procedure names can be correctly referenced in generated handlers.

### 3.1 — Connection String Discovery

Scan `app.config` / `App.config` in each service project for:
- `<connectionStrings>` entries — record each `name` and `providerName`
- `<appSettings>` entries whose keys contain `Connection`, `ConnStr`, `Database`, `DataSource` — record key names only (never values)

Cross-reference with `ConfigurationManager.ConnectionStrings["key"]` and
`ConfigurationManager.AppSettings["key"]` calls in source code to confirm which keys are used.

### 3.2 — ADO.NET Command Extraction

Scan all `.cs` / `.vb` files in each service project for:

**Stored procedure calls**:
```csharp
// Pattern: SqlCommand with CommandType.StoredProcedure
cmd.CommandType = CommandType.StoredProcedure;
cmd.CommandText = "usp_ProcessBatch";
```

For each, record:
- Stored procedure name (string literal in `CommandText`)
- Parameter names and types (from `cmd.Parameters.Add(...)` calls)
- Whether it uses a transaction (`SqlTransaction` / `DbTransaction`)
- Whether it uses `SqlBulkCopy` (bulk-copy flag — timeout risk)

**Inline SQL**:
```csharp
cmd.CommandText = "SELECT * FROM BatchQueue WHERE Processed = 0";
```
Flag inline SQL for review — stored procedure migration is preferred.

### 3.3 — ORM Detection

Check for Entity Framework 6 (`DbContext`, `.edmx` files) or Dapper (`QueryAsync`, `ExecuteAsync`).
Record the ORM type and version — affects handler dependency configuration.

---

## Step 4 — Identify Windows-Specific API Surface

**Objective**: Enumerate every Windows-only API call that has no direct Lambda equivalent
and must be replaced or removed.

Scan all source files in each service project for the following patterns. Record file path,
line number, and usage context for each occurrence.

| API / Namespace | Replacement in Lambda |
|-----------------|-----------------------|
| `System.Diagnostics.EventLog.WriteEntry(...)` | `ILambdaLogger.LogInformation(...)` or `Console.WriteLine(...)` → CloudWatch Logs |
| `EventLog.CreateEventSource(...)` | Remove — no EventLog source registration in Lambda |
| `Microsoft.Win32.Registry` | `Environment.GetEnvironmentVariable(...)` sourced from SSM Parameter Store |
| `System.ServiceProcess.ServiceController` | Remove — no service controller concept in Lambda |
| `System.Runtime.InteropServices` (P/Invoke, COM) | **High complexity flag** — manual gate required |
| `System.IO.Pipes` (named pipes) | **High complexity flag** — architectural redesign required |
| `System.Windows.Forms` references | Remove — Lambda has no UI layer |
| `System.Web.HttpContext` | Remove — no HTTP context in non-HTTP Lambda functions |
| `System.Configuration.ConfigurationManager` | `Environment.GetEnvironmentVariable(...)` for all `AppSettings` keys |

---

## Step 5 — Classify Complexity

**Objective**: Assign a complexity grade (Low / Medium / High) and a timeout risk flag to
each service, using the classification rules below.

### Classification Rules

**Automatic High — any of:**
- Uses P/Invoke, COM interop, or `System.Runtime.InteropServices`
- Uses named pipes (`System.IO.Pipes`) or raw socket listeners
- Uses MSDTC distributed transactions (`TransactionScope` across multiple connections)
- Uses linked-server queries or `OPENQUERY` in SQL command text
- Uses `System.Messaging.MessageQueue` (MSMQ) — requires MSMQ-to-SQS architectural redesign

**Automatic Medium — any of (and not already High):**
- Uses `EventLog` write (replacement is straightforward but requires file edits)
- Uses `Registry` read (replacement is straightforward but requires file edits)
- Has more than 3 distinct stored procedure calls
- Uses `DataSet` / `DataTable` manipulation
- Uses `DbTransaction` (wraps multiple SP calls in a transaction — must be preserved in handler)
- Uses `SqlBulkCopy` (bulk copy — timeout risk assessment required)
- `OnCustomCommand` override is present

**Low — all remaining services that do not meet Medium or High criteria.**

### Timeout Risk Assessment

For each service with a loop or batch pattern:
1. Estimate maximum records processed per run from any `SELECT TOP N` or `WHERE Processed = 0` filters.
2. Flag as **timeout risk** if worst-case runtime may exceed 900 seconds (15 minutes).
3. Annotate with recommended mitigation: SQS pagination pattern or Step Functions `Map` state.

---

## Step 6 — Produce service-inventory.json

Write `migration-output/service-inventory.json` with the following structure:

```json
{
  "inventoryDate": "<ISO-8601 timestamp>",
  "solutionFolder": "<path>",
  "sourceFramework": "<moniker>",
  "services": [
    {
      "name": "BatchProcessorService",
      "projectFile": "src/BatchProcessor/BatchProcessor.csproj",
      "language": "CSharp",
      "serviceBaseClass": "BatchProcessorService",
      "complexity": "Low",
      "timeoutRisk": false,
      "triggers": [
        {
          "type": "SystemTimersTimer",
          "intervalMs": 300000,
          "intervalMinutes": 5,
          "proposedEventBridgeExpression": "rate(5 minutes)"
        }
      ],
      "connectionStringKeys": ["MainDbConnection"],
      "storedProcedures": [
        {
          "name": "usp_ProcessBatch",
          "parameters": ["@BatchSize INT", "@ProcessedBy NVARCHAR(50)"],
          "usesTransaction": false,
          "usesBulkCopy": false
        }
      ],
      "windowsApiSurface": [],
      "notes": ""
    }
  ],
  "summary": {
    "total": 1,
    "low": 1,
    "medium": 0,
    "high": 0,
    "timeoutRiskCount": 0
  }
}
```

**Decision gate**: Present the inventory to the user. Confirm:
1. Are all services identified and complexity grades correct?
2. Are all stored procedure names captured accurately?
3. Are connection string key names correct?
4. Do any High-complexity assessments need re-grading?

Do not proceed to migration planning until the user approves the inventory.
