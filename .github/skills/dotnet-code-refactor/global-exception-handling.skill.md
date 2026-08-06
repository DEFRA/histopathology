---
name: global-exception-handling
description: This skill is used by dotnet-code-refactor agent specifically for migrated Civica applications (BSE, Histo, D2R2 and PTLIMS) . Centralize exception handling and remove redundant try/catch blocks.
---
 
# Skill: global-exception-handling
 
## Purpose
Replace all generic, silent, and unstructured exception handling in C# / ASP.NET Core Razor Pages code with a consistent domain exception hierarchy, fix empty `Catch` blocks in VB.NET source files, and wire a centralised global exception handler in `Program.cs` middleware (`UseExceptionHandler`) so that every unhandled exception is captured, logged via Application Insights, and presented to the user with a safe error page — never a raw stack trace.
 
## Trigger Conditions
- Called after `code-cleanup-refactor` (which has already removed `On Error Resume Next` patterns).
- Runs before `appinsights-logging` so that the `TelemetryClient` calls placed by this skill are consistent with the logging patterns applied in the next skill.
 
---
 
## Step 1 — Define the Domain Exception Hierarchy. For Example , for BSE Application
 
**Determine the output file based on the `language` input before creating the file.**

### language=vb — Create `BSESystem\Exceptions\BSESystemExceptions.vb`

```vb
Namespace BSESystem.Exceptions

    ''' <summary>Base exception for all application-layer errors.</summary>
    Public Class BSEException
        Inherits Exception

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
        End Sub
    End Class

    ''' <summary>Raised when a database or data-access operation fails.</summary>
    Public Class DataAccessException
        Inherits BSEException

        Public Property Operation As String

        Public Sub New(message As String, operation As String, innerException As Exception)
            MyBase.New(message, innerException)
            Me.Operation = operation
        End Sub
    End Class

    ''' <summary>Raised when user-supplied input fails domain validation rules.</summary>
    Public Class ValidationException
        Inherits BSEException

        Public Property FieldName As String

        Public Sub New(message As String, fieldName As String)
            MyBase.New(message)
            Me.FieldName = fieldName
        End Sub
    End Class

    ''' <summary>Raised when an external service or integration call fails.</summary>
    Public Class IntegrationException
        Inherits BSEException

        Public Property ServiceName As String

        Public Sub New(message As String, serviceName As String, innerException As Exception)
            MyBase.New(message, innerException)
            Me.ServiceName = serviceName
        End Sub
    End Class

    ''' <summary>Raised when a requested resource does not exist.</summary>
    Public Class NotFoundException
        Inherits BSEException

        Public Sub New(resourceType As String, resourceId As String)
            MyBase.New($"{resourceType} '{resourceId}' was not found.")
        End Sub
    End Class

End Namespace
```

### language=cs — Create `BSESystem\Exceptions\BSESystemExceptions.cs`

```csharp
namespace BSESystem.Exceptions;

/// <summary>Base exception for all application-layer errors.</summary>
public class BSEException : Exception
{
    public BSEException(string message) : base(message) { }
    public BSEException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>Raised when a database or data-access operation fails.</summary>
public class DataAccessException : BSEException
{
    public string Operation { get; }
    public DataAccessException(string message, string operation, Exception innerException)
        : base(message, innerException) => Operation = operation;
}

/// <summary>Raised when user-supplied input fails domain validation rules.</summary>
public class ValidationException : BSEException
{
    public string FieldName { get; }
    public ValidationException(string message, string fieldName)
        : base(message) => FieldName = fieldName;
}

/// <summary>Raised when an external service or integration call fails.</summary>
public class IntegrationException : BSEException
{
    public string ServiceName { get; }
    public IntegrationException(string message, string serviceName, Exception innerException)
        : base(message, innerException) => ServiceName = serviceName;
}

/// <summary>Raised when a requested resource does not exist.</summary>
public class NotFoundException : BSEException
{
    public NotFoundException(string resourceType, string resourceId)
        : base($"{resourceType} '{resourceId}' was not found.") { }
}
---
 
## Step 2 — Replace Generic Catch Clauses
 
Scan all `.vb` files (language=vb) **or** all `.cs` and `.cshtml.cs` files (language=cs) for generic catch clauses.

For each occurrence, determine which domain exception type is most appropriate based on the enclosing context:

| Context | Replace with |
|---|---|
| Data access method (SqlCommand, stored proc, DataAdapter) | `DataAccessException` |
| Input processing, form validation | `ValidationException` |
| External service call (HTTP, SMTP, file I/O) | `IntegrationException` |
| Resource lookup (GetById, FindByName) that can return nothing | `NotFoundException` |
| All others where no specific type can be determined | Keep `Exception` but add structured logging |

**Before:**
```csharp
try
{
    sqlCmd.ExecuteNonQuery();
}
catch (Exception ex)
{
    lblError.Text = ex.Message;
}
```

**After:**
```csharp
try
{
    sqlCmd.ExecuteNonQuery();
}
catch (SqlException ex)
{
    throw new DataAccessException("Failed to save record.", "SaveRecord", ex);
}
catch (Exception ex)
{
    throw new BSEException("Unexpected error during save.", ex);
}
```

**Before:**
```csharp
try
{
    sqlCmd.ExecuteNonQuery();
}
catch (Exception ex)
{
    lblError.Text = ex.Message;
}
```

**After:**
```csharp
try
{
    sqlCmd.ExecuteNonQuery();
}
catch (SqlException ex)
{
    throw new DataAccessException("Failed to save record.", "SaveRecord", ex);
}
catch (Exception ex)
{
    throw new BSEException("Unexpected error during save.", ex);
}
---
 
## Step 3 — Fix Empty Catch Blocks
 
An empty `Catch`/`catch` block is a silent failure — it swallows the exception without any logging or recovery. All empty blocks must have at minimum a telemetry call.

PROBE_S3_VB

```vb
Catch ex As Exception
    ' (no statements — empty body)
End Try
```

Also detect:
```vb
Catch
    ' (no exception variable, empty body)
End Try
```

### C# Detection (language=cs)

```csharp
catch (Exception)
{
    // empty or comment-only
}
```

Also detect `catch { }` (untyped empty catch).

### VB.NET Fix Pattern

**Before:**
```vb
Try
    SendEmailNotification(caseId)
Catch ex As Exception
End Try
```

**After:**
```vb
Try
    SendEmailNotification(recordId)
Catch ex As Exception
    ' Email notification is non-critical — log and continue
    _telemetryHelper.TrackException(ex, "SendEmailNotification",
        New Dictionary(Of String, String) From {
            {"recordId", recordId.ToString()},
            {"operation", "SendEmailNotification"}
        })
End Try
```

### C# Fix Pattern

**Before:**
```csharp
try
{
    SendEmailNotification(caseId);
}
catch (Exception)
{
}
```

**After:**
```csharp
try
{
    SendEmailNotification(caseId);
}
catch (Exception ex)
{
    // Email notification is non-critical — log and continue
    _telemetryHelper.TrackException(ex, nameof(SendEmailNotification),
        new Dictionary<string, string> { ["caseId"] = caseId.ToString() });
}
> **Never re-throw inside a non-critical fire-and-forget operation** — but the telemetry call is mandatory.
 
Log each fix in `docs/code-refactor/exception-handling-report.json`: file path, line number, original pattern, applied fix.
 
---
 
## Step 4 — Wire Global Exception Handler in Program.cs

`Global.asax.vb` `Application_Error` is **dead code** in .NET 10 — `System.Web` is gone and `Global.asax` is not processed by the runtime. The global exception handler must be wired as `UseExceptionHandler` middleware in `Program.cs`.

Add the following to the `Program.cs` skeleton created by the `framework-upgrade` skill, replacing the `// TODO` placeholder for `Application_Error`:

```csharp
// Program.cs
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
            if (exceptionFeature?.Error is not null)
            {
                var telemetry = context.RequestServices.GetRequiredService<TelemetryClient>();
                telemetry.TrackException(exceptionFeature.Error, new Dictionary<string, string>
                {
                    ["url"] = context.Request.Path.Value ?? string.Empty,
                    ["user"] = context.User?.Identity?.Name ?? "anonymous",
                    ["sessionId"] = context.Session?.Id ?? string.Empty
                });

                context.Response.StatusCode = exceptionFeature.Error switch
                {
                    NotFoundException => StatusCodes.Status404NotFound,
                    UnauthorizedAccessException => StatusCodes.Status403Forbidden,
                    _ => StatusCodes.Status500InternalServerError
                };

                var redirectPath = context.Response.StatusCode switch
                {
                    404 => "/Error/NotFound",
                    403 => "/Error/Forbidden",
                    _ => "/Error/GeneralError"
                };

                context.Response.Redirect(redirectPath);
            }
        });
    });
    app.UseHsts();
}
```

> This replaces the `Application_Error` / `Server.GetLastError()` / `Response.Redirect("~/Error/...")` pattern entirely. The error Razor Pages (`/Error/NotFound`, `/Error/Forbidden`, `/Error/GeneralError`) are stub pages created by `framework-upgrade` and must be populated with safe, user-friendly content by the development team. Never redirect to `.aspx` error pages — they are not served by the .NET 10 runtime.
 
---
 
## Step 5 — Remove EventLog.WriteEntry from Catch Blocks
 
`EventLog.WriteEntry` is not available in the Azure App Service sandbox and will throw `SecurityException` or `InvalidOperationException`. Any `EventLog.WriteEntry` call inside a `Catch` block must be replaced.
 
**Before:**
```vb
Catch ex As Exception
    EventLog.WriteEntry("webApp", ex.Message, EventLogEntryType.Error)
End Try
```
 
**After:**
```vb
Catch ex As Exception
    ' EventLog is not available on Azure App Service — use Application Insights
    _telemetryHelper.TrackException(ex, "ProcessRecord")
    Throw
End Try
```
 
---
 
## Outputs
 
| Output | Description |
|---|---|
| `BSESystem\Exceptions\BSESystemExceptions.cs` | New domain exception hierarchy file |
| `docs\code-refactor\exception-handling-report.json` | Structured log: generic catches replaced, empty blocks fixed, EventLog calls replaced, Program.cs middleware wired |
| All modified `.cs` and `.cshtml.cs` files | Updated in place |
## Constraints

- Do **not** add a `Catch` block to every `Try` — only add handling where there is meaningful recovery or logging to apply.
- Do **not** re-throw inside the `UseExceptionHandler` middleware — it is the terminal boundary.
- Do **not** expose exception details (stack traces, SQL messages) in user-facing error pages or HTTP responses.
- Do **not** suppress auth-related exceptions (from ASP.NET Core authentication or SAML middleware) — these must propagate to allow the auth pipeline to handle them correctly.
- `BSESystem\Exceptions\BSESystemExceptions.cs` is auto-included in SDK-style projects by convention — do **not** add a manual `<Compile>` entry to the project file.