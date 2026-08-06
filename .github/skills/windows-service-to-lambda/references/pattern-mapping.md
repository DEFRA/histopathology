# Reference: Pattern Mapping — .NET Framework 4.x Windows Service → AWS Lambda

This reference documents every Windows Service pattern the agent recognises in
.NET Framework 4.x source code and its direct Lambda-safe equivalent.

---

## 1. Scheduling and Trigger Patterns

| Windows Pattern | Detection Signal | Lambda Trigger | Notes |
|-----------------|----------------|---------------|-------|
| `System.Timers.Timer` at fixed interval | `new Timer(interval)` + `.Elapsed +=` | EventBridge Scheduler `rate(N minutes)` | Minimum interval on EventBridge is 1 minute; sub-minute intervals require SQS polling pattern |
| `System.Threading.Timer` at fixed interval | `new Timer(callback, null, dueTime, period)` | EventBridge Scheduler `rate(N minutes)` | `dueTime` maps to initial delay; ignored in Lambda (EventBridge fires at first cron window) |
| `while(true)` + `Thread.Sleep(ms)` | Infinite loop with `Thread.Sleep` in `OnStart` background thread | EventBridge Scheduler `rate(N minutes)` + Lambda invocation | Sleep duration converted to minutes; Lambda is stateless — loop replaced by recurring invocations |
| `ThreadPool.RegisterWaitForSingleObject` with timeout | `RegisterWaitForSingleObject(waitObject, callback, null, timeout, false)` | EventBridge Scheduler | Timeout parameter determines interval |
| Task Scheduler XML `<CalendarTrigger>` | `.xml` file with `<CalendarTrigger>` + cron schedule | EventBridge Scheduler `cron(...)` | Task Scheduler cron syntax differs from EventBridge; convert carefully (EventBridge uses UTC) |
| `System.IO.FileSystemWatcher` | `new FileSystemWatcher(path)` + `.Created` / `.Changed` events | S3 Event Notification → Lambda trigger | Files must be written to S3 by the upstream process |
| `System.Messaging.MessageQueue` (MSMQ) | `new MessageQueue(queuePath)` + `.Receive()` | SQS Queue → Lambda trigger | MSMQ queues must be replicated to SQS during cutover window |

---

## 2. Lifecycle Method Mapping

| Windows Method | Lambda Equivalent | Notes |
|---------------|------------------|-------|
| `ServiceBase.OnStart(string[] args)` | Lambda handler entry point (`FunctionHandler`) | `args` are unused in Lambda; any config previously passed via SCM args must move to environment variables |
| `ServiceBase.OnStop()` | Lambda does not have an `OnStop` equivalent | Cleanup logic (flushing buffers, closing connections) must be triggered by `CancellationToken` from `ILambdaContext.CancellationToken` |
| `ServiceBase.OnPause()` | No equivalent | EventBridge rule `State: DISABLED` pauses scheduling |
| `ServiceBase.OnContinue()` | No equivalent | EventBridge rule `State: ENABLED` resumes scheduling |
| `ServiceBase.OnCustomCommand(int command)` | EventBridge custom event bus rule with `detail-type` filter | Each `command` integer maps to a distinct `detail-type` string |
| `ProjectInstaller` class | Remove entirely | Lambda has no installer; deployment is via `aws lambda update-function-code` |
| `ServiceBase.RequestAdditionalTime(ms)` | No equivalent | Increase Lambda timeout in IaC; if near 15-min limit, implement chunking |

---

## 3. Configuration and Secrets

| Windows Pattern | Lambda Equivalent | Notes |
|----------------|------------------|-------|
| `ConfigurationManager.AppSettings["key"]` | `Environment.GetEnvironmentVariable("KEY")` | Keys conventionally SCREAMING_SNAKE_CASE in Lambda; map original key names |
| `ConfigurationManager.ConnectionStrings["key"].ConnectionString` | `Environment.GetEnvironmentVariable("KEY_SECRET")` → secret fetched from Secrets Manager at runtime | Never store connection strings as plaintext Lambda env vars |
| `app.config <appSettings>` file | Lambda environment variables sourced from IaC (Terraform/CFn) | `app.config` is not deployed to Lambda |
| `ConfigurationManager` custom config sections | JSON config in Secrets Manager or SSM Parameter Store | Custom section serialisation must be replicated in handler startup code |
| `System.Configuration.ConfigurationSection` | `System.Text.Json.JsonSerializer.Deserialize<T>` from SSM/Secrets Manager JSON string | |
| `Registry.GetValue(keyPath, valueName, default)` | `Environment.GetEnvironmentVariable("VALUE_NAME")` sourced from SSM Parameter Store | Registry is Windows-only; SSM Parameter Store is the cloud equivalent |

---

## 4. Logging

| Windows Pattern | Lambda Equivalent | Notes |
|----------------|------------------|-------|
| `EventLog.WriteEntry(source, message, EventLogEntryType.Information)` | `context.Logger.LogInformation(message)` (C#) | Logs appear in CloudWatch Logs under `/aws/lambda/<function-name>` |
| `EventLog.WriteEntry(source, message, EventLogEntryType.Error)` | `context.Logger.LogError(message)` | |
| `EventLog.WriteEntry(source, message, EventLogEntryType.Warning)` | `context.Logger.LogWarning(message)` | |
| `EventLog.CreateEventSource(source, log)` | Remove entirely — no log source registration in Lambda | |
| `System.Diagnostics.Trace.WriteLine(message)` | `context.Logger.LogInformation(message)` | |
| `log4net` / `NLog` file appenders | Replace with CloudWatch Logs appender or structured `Console.WriteLine(json)` | Lambda captures all `stdout`/`stderr` to CloudWatch Logs automatically |
| `log4net` / `NLog` EventLog appenders | Remove — EventLog appender has no Lambda equivalent | |

---

## 5. Data Access

| Windows Pattern | Lambda Equivalent | Notes |
|----------------|------------------|-------|
| `new SqlConnection(connectionString)` | `new SqlConnection(connStr)` where `connStr` fetched from Secrets Manager | Use `static readonly` lazy-singleton pattern to reuse connections across warm invocations |
| `new OracleConnection(connectionString)` | `new OracleConnection(connStr)` with lazy singleton | Oracle Managed Driver (`Oracle.ManagedDataAccess`) is compatible with .NET 10 |
| `new SqlCommand("usp_Name", conn)` | Preserved verbatim | SP name must not change |
| `cmd.CommandType = CommandType.StoredProcedure` | Preserved verbatim | |
| `cmd.Parameters.Add(new SqlParameter(...))` | Preserved verbatim | All parameter names preserved |
| `SqlDataAdapter` + `DataSet.Fill()` | Preserved verbatim — `System.Data` is available on .NET 10 | `DataSet` / `DataTable` work on Linux (Lambda runs on Amazon Linux 2023) |
| `SqlBulkCopy` (large batches) | Paginated SQS batch approach recommended if total runtime > 15 min | See Skill 06 blocker guidance |
| `new SqlTransaction` / `BeginTransaction()` | Preserved verbatim — local (non-distributed) transactions work in Lambda | MSDTC distributed transactions do not — see High-Complexity blockers |

---

## 6. Thread and Async Patterns

| Windows Pattern | Lambda Equivalent | Notes |
|----------------|------------------|-------|
| `new Thread(() => { ... }).Start()` | `await Task.Run(() => { ... })` | Lambda handler is async; `Thread` is replaced with `Task` |
| `Thread.Sleep(ms)` | `await Task.Delay(ms)` | Only use inside handler if truly needed (e.g. rate-limiting between API calls) |
| `Thread.Join()` | `await task` | |
| `ManualResetEvent` / `AutoResetEvent` | `SemaphoreSlim` or `TaskCompletionSource` | |
| `lock(obj) { ... }` | Preserved verbatim — `lock` works in .NET 10 | Confirm shared state is not crossing invocation boundaries (stateless Lambda) |
| `Parallel.ForEach(...)` | Preserved verbatim — TPL works on Lambda | Beware of memory limits (Lambda max 10 GB); set `MaxDegreeOfParallelism` |

---

## 7. .NET Framework 4.x–Specific Patterns

| .NET 4.x Pattern | .NET 10 Equivalent | Notes |
|-----------------|------------------|-------|
| `Task.Factory.StartNew(...)` | `Task.Run(...)` | Preferred in .NET 10 |
| `async void` event handlers | `async Task` | `async void` is an anti-pattern in Lambda — exceptions are swallowed |
| `using (var scope = new TransactionScope())` (local) | Preserved verbatim — `System.Transactions` is in 10 | MSDTC-dependent `TransactionScope` will fail; local transactions work |
| `dynamic` type | Preserved — `dynamic` works in .NET 10 | No behavioural difference |
| `XmlSerializer` / `DataContractSerializer` | Preserved — both available in .NET 10 | |
| `Type.GetType("AssemblyQualifiedName")` | Review — assembly names change on .NET 10 | Flag for manual review if found |
| `AppDomain.CurrentDomain.BaseDirectory` | `AppContext.BaseDirectory` | Equivalent in .NET 10; `AppDomain` is present but deprecated |

---

## 8. Windows-Only APIs That Must Be Removed

These APIs have no Lambda equivalent. They must be removed and replaced as documented,
or flagged as a High-complexity blocker.

| API | Action |
|-----|--------|
| `Microsoft.Win32.Registry` | Replace with SSM Parameter Store reads |
| `System.ServiceProcess.ServiceController` | Remove — no service controller in Lambda |
| `System.ServiceProcess.ServiceBase` (all methods) | Remove — entire class replaced by Lambda Function class |
| `System.ServiceProcess.ServiceInstaller` / `ProjectInstaller` | Delete file entirely |
| `System.Runtime.InteropServices.DllImport` (P/Invoke) | **High-complexity flag** — requires manual investigation |
| `System.IO.Pipes.NamedPipeServerStream` | **High-complexity flag** — redesign as SQS trigger |
| `System.Messaging.MessageQueue` (MSMQ) | **High-complexity flag** — redesign as SQS trigger |
| `System.Web.HttpContext` | Remove — no HTTP context in non-HTTP Lambda functions |
| `System.Windows.Forms.*` | Remove entirely — Lambda has no UI layer |
| COM interop (`[ComImport]`, `[Guid(...)]`) | **High-complexity flag** — requires manual investigation |
