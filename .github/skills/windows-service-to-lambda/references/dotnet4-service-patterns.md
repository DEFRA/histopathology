# Reference: .NET Framework 4.x Windows Service Patterns

This reference documents the specific code patterns found in .NET Framework 4.x Windows
Service projects that the agent scans for during the Service Inventory skill (Skill 01).

---

## 1. Project File Signals (.csproj / .vbproj)

Classic-format project files (non-SDK-style) contain the following Windows Service markers:

```xml
<!-- .csproj — C# Windows Service -->
<Reference Include="System.ServiceProcess" />

<!-- Output type for Windows Service (not Console) -->
<OutputType>WinExe</OutputType>

<!-- Target framework — classic format -->
<TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
<!-- or v4.5, v4.5.2, v4.6, v4.6.1, v4.6.2, v4.7, v4.7.1, v4.7.2, v4.8 -->
```

For VB.NET projects (`.vbproj`), the same `<Reference Include="System.ServiceProcess" />`
is present.

**Note**: Some Windows Service projects use `<OutputType>Exe</OutputType>` rather than
`WinExe`. Both must be checked — the presence of `ServiceBase` inheritance is the definitive
signal.

---

## 2. ServiceBase Class Structure

### C# (.NET 4.x)

```csharp
using System;
using System.ServiceProcess;
using System.Timers;

namespace BatchProcessor
{
    public partial class BatchProcessorService : ServiceBase
    {
        private Timer _timer;

        public BatchProcessorService()
        {
            InitializeComponent();
            ServiceName = "BatchProcessorService";
        }

        protected override void OnStart(string[] args)
        {
            _timer = new Timer(300000); // 5 minutes
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Start();
        }

        protected override void OnStop()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Business logic entry point — extract this for Lambda handler
            ProcessBatch();
        }

        private void ProcessBatch()
        {
            // Data-access logic — preserved verbatim in Lambda handler
        }
    }
}
```

### VB.NET (.NET 4.x)

```vb
Imports System.ServiceProcess
Imports System.Timers

Public Class BatchProcessorService
    Inherits ServiceBase

    Private _timer As Timer

    Public Sub New()
        Me.ServiceName = "BatchProcessorService"
    End Sub

    Protected Overrides Sub OnStart(args() As String)
        _timer = New Timer(300000) ' 5 minutes
        AddHandler _timer.Elapsed, AddressOf OnTimerElapsed
        _timer.AutoReset = True
        _timer.Start()
    End Sub

    Protected Overrides Sub OnStop()
        If _timer IsNot Nothing Then
            _timer.Stop()
            _timer.Dispose()
        End If
    End Sub

    Private Sub OnTimerElapsed(sender As Object, e As ElapsedEventArgs)
        ' Business logic entry point — extract this for Lambda handler
        ProcessBatch()
    End Sub

    Private Sub ProcessBatch()
        ' Data-access logic — preserved verbatim in Lambda handler
    End Sub
End Class
```

---

## 3. Program.cs / Module.vb Entry Point

The entry point file wires the service into the SCM. It is **not migrated** — the entire
`ServiceBase.Run()` call is removed in Lambda.

### C# Program.cs

```csharp
static class Program
{
    static void Main()
    {
        ServiceBase[] ServicesToRun = new ServiceBase[]
        {
            new BatchProcessorService()
        };
        ServiceBase.Run(ServicesToRun);
        // This entire file is removed in Lambda migration
    }
}
```

### VB.NET Module.vb

```vb
Module Program
    Sub Main()
        Dim ServicesToRun() As ServiceBase = {New BatchProcessorService()}
        ServiceBase.Run(ServicesToRun)
        ' This entire file is removed in Lambda migration
    End Sub
End Module
```

---

## 4. ProjectInstaller — Removed in Migration

The `ProjectInstaller` class is a Windows SCM installer manifest. It has no Lambda
equivalent and is deleted entirely from the output.

```csharp
[RunInstaller(true)]
public partial class ProjectInstaller : Installer
{
    private ServiceProcessInstaller serviceProcessInstaller;
    private ServiceInstaller serviceInstaller;

    public ProjectInstaller()
    {
        // Windows-only — entire class removed in Lambda migration
        serviceProcessInstaller = new ServiceProcessInstaller();
        serviceProcessInstaller.Account = ServiceAccount.NetworkService;
        serviceInstaller = new ServiceInstaller();
        serviceInstaller.ServiceName = "BatchProcessorService";
        serviceInstaller.StartType = ServiceStartMode.Automatic;
        Installers.Add(serviceProcessInstaller);
        Installers.Add(serviceInstaller);
    }
}
```

---

## 5. app.config Structure (Connection Strings and AppSettings)

All `app.config` values are migrated to Lambda environment variables sourced from
Secrets Manager or SSM Parameter Store.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <!-- MIGRATED TO: Secrets Manager secret for connection strings -->
  <connectionStrings>
    <add name="MainDbConnection"
         connectionString="Data Source=SQLSERVER01;Initial Catalog=BatchDB;Integrated Security=True;"
         providerName="System.Data.SqlClient" />
    <add name="AuditDbConnection"
         connectionString="Data Source=SQLSERVER01;Initial Catalog=AuditDB;Integrated Security=True;"
         providerName="System.Data.SqlClient" />
  </connectionStrings>

  <!-- MIGRATED TO: Lambda environment variables -->
  <appSettings>
    <add key="BatchSize" value="500" />
    <add key="RetryCount" value="3" />
    <add key="NotificationEmail" value="ops@example.com" />
  </appSettings>
</configuration>
```

**Migration rule**: Connection string values are **never** migrated as plaintext.
The key name is used to create a Secrets Manager path (e.g.
`/prod/BatchProcessor/MainDbConnection`). AppSettings with non-sensitive values
(batch size, retry count) are migrated as plaintext Lambda environment variables.
AppSettings with sensitive values (API keys, passwords, tokens) must be moved to
Secrets Manager.

---

## 6. ADO.NET Patterns in .NET Framework 4.x Services

These patterns are preserved verbatim in the generated Lambda handler —
only the connection string source changes.

### Stored Procedure with Parameters

```csharp
// Connection string source migrated — all other code preserved verbatim
using (var conn = new SqlConnection(connStr)) // connStr from Secrets Manager
{
    conn.Open();
    using (var cmd = new SqlCommand("usp_ProcessBatch", conn))
    {
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@BatchSize", SqlDbType.Int) { Value = batchSize });
        cmd.Parameters.Add(new SqlParameter("@ProcessedBy", SqlDbType.NVarChar, 50) { Value = "Lambda" });
        cmd.ExecuteNonQuery();
    }
}
```

### DataReader Pattern

```csharp
using (var conn = new SqlConnection(connStr))
{
    conn.Open();
    using (var cmd = new SqlCommand("usp_GetPendingItems", conn))
    {
        cmd.CommandType = CommandType.StoredProcedure;
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                // Process each row — preserved verbatim
            }
        }
    }
}
```

### Transaction Pattern (local — preserved in Lambda)

```csharp
using (var conn = new SqlConnection(connStr))
{
    conn.Open();
    using (var tran = conn.BeginTransaction())
    {
        try
        {
            // Multiple SP calls in a transaction — preserved verbatim
            // Note: MSDTC TransactionScope across multiple connections is a High-complexity blocker
            tran.Commit();
        }
        catch
        {
            tran.Rollback();
            throw;
        }
    }
}
```

### SqlBulkCopy (Timeout Risk — High Complexity Signal)

```csharp
using (var bulkCopy = new SqlBulkCopy(connStr))
{
    bulkCopy.DestinationTableName = "dbo.ProcessedItems";
    bulkCopy.BatchSize = 5000;
    bulkCopy.BulkCopyTimeout = 600; // 10 minutes — Lambda timeout risk
    bulkCopy.WriteToServer(dataTable);
}
```

When `SqlBulkCopy` is detected, the service is flagged for timeout risk assessment.
If the total dataset exceeds safe limits, the service is classified Medium or High complexity.

---

## 7. Common EventLog Patterns

```csharp
// Write to Windows Application Event Log — MIGRATED TO: CloudWatch Logs
EventLog.WriteEntry("BatchProcessorService", "Service started", EventLogEntryType.Information);
EventLog.WriteEntry("BatchProcessorService", $"Error: {ex.Message}", EventLogEntryType.Error);

// Creating event source — REMOVED in Lambda (no EventLog source registration)
if (!EventLog.SourceExists("BatchProcessorService"))
    EventLog.CreateEventSource("BatchProcessorService", "Application");
```

---

## 8. Common Registry Patterns

```csharp
// Registry reads — MIGRATED TO: SSM Parameter Store / Lambda environment variables
using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\MyCompany\BatchProcessor"))
{
    var setting = key?.GetValue("ConfigValue")?.ToString();
}
```

When Registry reads are found, the service is classified as Medium complexity (or High if
Registry writes or ACL modifications are present).
