---
name: code-cleanup-refactor
description: This skill is used by dotnet-code-refactor agent specifically for migrated Civica applications (BSE, Histo, D2R2 and PTLIMS) . Cleans code and remove duplication using best-practice refactoring.
---
 
# Skill: code-cleanup-refactor
 
## Purpose
Scan all source files in the solution, remove dead and unreachable code, enforce naming conventions, split oversized methods and classes, catalogue TODO/FIXME/HACK comments, and remove `On Error Resume Next` patterns — replacing them with structured `Try`/`Catch` blocks.
 
## Trigger Conditions
- Called after `framework-upgrade` Mode B and `nuget-package-upgrade` Mode B have completed.
- The solution targets `.NET 10` with an SDK-style `.csproj`. All application source files are C# (`.cs`). New C# scaffold files (`Program.cs`, `.cshtml` + `.cshtml.cs` stubs) have been created by the framework upgrade.
- Remove empty `catch` blocks swallowing exceptions with no logging.
- Remove undocumented `#pragma warning disable` suppressions — fix nullable (`CS8600`–`CS8670`) warnings at the root cause instead of suppressing.
 
---
 
## Step 1 — Discover All Source Files

Scan the `solutionFolder` recursively for:
- `*.cs` (controllers, services, repositories, helpers)
- `*.cshtml.cs` (Razor Page PageModel files — stubs to be populated)
- Exclude: `*.g.cs`, `*.AssemblyInfo.cs`, auto-generated files under `obj/`

Also scan for TODO/FIXME markers in:
- `*.cshtml` (Razor Page view stubs — read-only catalogue; do not modify view markup)
 
---
 
## Step 2 — Remove Dead and Unreachable Code

| Pattern | Action |
|---|---|
| Unused `using` directives | Remove the `using` line |
| Unreferenced `public`/`private` methods within a class | Remove after confirming no dynamic/reflection binding |
| Code after `return` or `throw` within the same block | Remove unreachable lines |
| `HttpContext.Current` usage | Replace with `IHttpContextAccessor` injected via constructor |
| `System.Web.*` type references not removed by upgrade | Flag as blocking — these have no equivalent in ASP.NET Core; log and raise as manual migration items |
| Dead code-behind logic in `.aspx.cs` files (if any remain on disk) | Migrate business logic to the corresponding `.cshtml.cs` `OnGet`/`OnPost` method; log the migration |
## Step 3 — Enforce Naming Conventions
 
The following conventions apply to both VB.NET and C# unless noted:

| Element | Convention | VB.NET Example | C# Example |
|---|---|---|---|
| Public method | PascalCase | `GetRecord()` | `GetRecord()` |
| Private method | PascalCase | `ValidateInput()` | `ValidateInput()` |
| Async method (C# only) | PascalCase + `Async` suffix | — | `GetRecordAsync()` |
| Local variable | camelCase | `patientId` | `patientId` |
| Private field | `_camelCase` | `_connectionString` | `_connectionString` |
| Parameter | camelCase | `slideName` | `slideName` |
| Constant | UPPER_SNAKE_CASE | `MAX_RETRY_COUNT` | `MAX_RETRY_COUNT` |
| Class / Module | PascalCase | `ApplicationService` | `ApplicationService` |
| Interface | `I` prefix + PascalCase | `ISlideRepository` | `ISlideRepository` |
| Hungarian notation | **Remove prefix** | `strName` → `name`, `blnActive` → `isActive` | `strName` → `name`, `bIsActive` → `isActive` |
| Single-letter variable outside loop | Rename to descriptive name | `x` → `slideIndex` | `x` → `slideIndex` |
 
---
 
## Step 4 — Split Large Methods and Classes
 
### Size Thresholds
 
| Artefact | Maximum Lines | Action When Exceeded |
|---|---|---|
| Method | 50 lines | Extract sub-methods; each extracted method has a single responsibility |
| Class | 300 lines | Extract into focused classes |
| Razor Page PageModel (`src/**/*.cshtml.cs`) | 400 lines | Extract business logic into a service class; keep PageModel to `OnGet`/`OnPost` request/response handling only |
| Nested `if` depth | 3 levels | Refactor with guard clauses (early `return`) or extract to a named method |
' Before — monolithic method
Private Sub ProcessRequest(recordId As Integer)
    ' 80 lines of mixed validation, data access, and rendering
End Sub
 
' After — split into focused methods
Private Sub ProcessRequest(recordId As Integer)
    Dim record = LoadRecord(recordId)
    ValidateRecord(record)
    RenderRecordDetails(record)
End Sub
 
Private Function LoadRecord(recordId As Integer) As RecordDto
    ' data access only
End Function
 
Private Sub ValidateRecord(record As RecordDto)
    ' validation only
End Sub
 
Private Sub RenderRecordDetails(record As RecordDto)
    ' UI binding only
End Sub
```
 
---
 
## Step 5 — Catalogue TODO / FIXME / HACK Comments

Search all files under `src/` for comments containing: `TODO`, `FIXME`, `HACK`, `TEMP`, `WORKAROUND` (case-insensitive).

Scan both C# source files and the Razor Page stubs:
- `src/**/*.cs` — application source
- `src/**/*.cshtml.cs` — Razor Page PageModels (contain `// TODO: Migrate from WebForms` markers)
- `src/**/*.cshtml` — Razor Page view stubs (read-only; do not modify)
 
Search all files under `src/` for comments containing: `TODO`, `FIXME`, `HACK`, `TEMP`, `WORKAROUND` (case-insensitive).

Scan both C# source files and the Razor Page stubs:
- `src/**/*.cs` — application source
- `src/**/*.cshtml.cs` — Razor Page PageModels (contain `// TODO: Migrate from WebForms` markers)
- `src/**/*.cshtml` — Razor Page view stubs (read-only; do not modify)
  "file": "src/relative/path/to/file.cs",
  "line": 142,
  "comment": "// TODO: Replace with stored procedure call",
  "recommendedAction": "Extract to data access layer; use parameterised SqlCommand"
}
```
 
Include the full catalogue as a table in the HTML conversion report.  
**Do not remove** any TODO comment from source — list only.

---

## Step 6 — Remove Silent Error Suppression

Search all `.cs` files for the following patterns and fix each:

#### Pattern 1 — Empty catch blocks

Search all `.cs` files for:
```csharp
catch (Exception)
{
    // empty or comment-only
}
```

**Replacement:**
```csharp
catch (Exception ex)
{
    _telemetryHelper.TrackException(ex, nameof(MethodName),
        new Dictionary<string, string> { ["operation"] = nameof(MethodName) });
    throw; // re-throw unless operation is explicitly fire-and-forget
}
```

#### Pattern 2 — Undocumented pragma suppression

Search all `.cs` files for `#pragma warning disable` without a following comment explaining the reason.

- If the suppressed warning is `CS8600`–`CS8670` (nullable), fix the root cause instead of suppressing.
- If the suppression has a legitimate reason, add an explanatory inline comment.

Log each fix in `docs/code-refactor/code-cleanup-report.json`.

---


 


 
#



C# has no `On Error Resume Next` equivalent, but the same silent-failure intent appears in two patterns:

#### Pattern 1 — Empty catch blocks

Search all `.cs` files for:
```csharp
catch (Exception)
{
    // empty or comment-only
}
```

**Replacement:**
```csharp
catch (Exception ex)
{
    _telemetryHelper.TrackException(ex, nameof(MethodName),
        new Dictionary<string, string> { ["operation"] = nameof(MethodName) });
    throw; // re-throw unless operation is explicitly fire-and-forget
}
```

#### Pattern 2 — Undocumented pragma suppression

Search all `.cs` files for `#pragma warning disable` without a following comment explaining the reason.

- If the suppressed warning is `CS8600`–`CS8670` (nullable), fix the root cause instead of suppressing.
- If the suppression has a legitimate reason, add an explanatory inline comment.

Log each fix in `docs/code-refactor/code-cleanup-report.json`.

---

## Outputs

| Output | Description |
|---|---|
| `docs/code-refactor/code-cleanup-report.json` | Structured log: dead code removed, renames applied, methods split, silent-error-suppression removals, TODO catalogue, PageModel stub migrations |
| All modified `.cs` and `.cshtml.cs` files | Updated in place |
- Do **not** enforce naming conventions on auto-generated code.
- Do **not** modify `.cshtml` Razor Page view markup files — view markup is out of scope for this skill.
- **May** populate `.cshtml.cs` PageModel `OnGet`/`OnPost` stub methods by migrating business logic from the corresponding WebForms code-behind. Never change the class declaration, namespace, or DI constructor added by `framework-upgrade`.