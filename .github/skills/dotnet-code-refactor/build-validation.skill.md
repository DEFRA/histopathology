# Skill: build-validation
 
> **Note — Canonical skill location**: The full, reusable version of this skill (supporting
> dotnet CLI modern build modes) lives at
> `.github/skills/dotnet-framework-upgrade/build-validation.md`.
> This copy is the **application-specific override** for the `dotnet-code-refactor` agent.
> This copy applies **Mode B (dotnet CLI modern)** for Classic→Modern upgrade paths.
 
---
 
## Purpose
Validate that the solution compiles cleanly in Release configuration after all refactoring changes have been applied to a C# `.NET 10` SDK-style project. Capture all errors and warnings, categorise them by error code, and drive iterative fixes until the build produces zero errors and acceptable warning counts.
 
## Trigger Conditions
- Runs within the `dotnet-code-refactor` agent after `code-cleanup-refactor`, `global-exception-handling`, and `appinsights-logging` skills have all completed.
- Re-runs after every batch of fix iterations until zero errors are achieved or the 5-iteration cap is reached.
- The solution must already target `.NET 10` with SDK-style C# project files — this skill does not perform any framework or NuGet upgrades.
 
---
 
## Step 1 — Restore Packages. For BSE Project example
 
```powershell
dotnet restore BSESystem.sln
```
 
Confirm exit code is `0`. If restore fails, report each failed package as a **P0 blocking issue** and halt.
 
---
 
## Step 2 — Run Build in Release Configuration
 
```powershell
dotnet build "BSESystem.sln" \
  --configuration Release \
  --no-restore \
  2>&1 | tee build-output.log
```
Can run on Windows or Linux. For CI/CD pipelines, `ubuntu-latest` runners are valid for .NET 5+ builds.

---
 
## Step 3 — Parse and Categorise Build Output
 
Parse `build-output.log` for dotnet build diagnostic entries. Categorise each:
 
| Category | Severity | Action |
|---|---|---|
| `CS*` | C# compiler error | Fix code |
| `NETSDK*` | .NET SDK tooling error | Fix SDK version, `global.json`, or project file |
| `NU*` | NuGet resolution error | Fix `PackageReference` version or source |
| `warning CS8600–CS8670` | Nullable reference warning | Address in touched files; do not suppress |
| `warning SYSLIB*` | Obsolete API warning | Plan replacement; document in upgrade report |
 
---
 
## Step 4 — Fix Errors Iteratively
 
For each remaining compiler error after initial build:
 
1. Identify file, line, and error code.
2. Determine root cause based on error prefix:

   **C# errors (`CS*`) 

   - `CS0246` / `CS0234` — missing `using` or type not found — namespace moved in upgraded package or `TelemetryHelper`/exception type not yet created.
   - `CS1061` — method or property does not exist — API change in upgraded package; use the modern replacement.
   - `CS8600`–`CS8670` — nullable reference — add null checks or null-forgiving operator only where the value is guaranteed non-null.
   - `CS0103` — name does not exist in context — DI-injected service not registered in `Program.cs`; add the missing `builder.Services` call.
   

 
---
 
## Step 5 — Fix NuGet Package Version Conflicts (NU1605)

For every `NU1605` warning (package downgrade detected):
1. Identify the conflicting package and versions.
2. Update `<PackageReference>` version to the highest required version.
3. Run `dotnet restore` again to confirm resolution.
 
---
 
## Step 6 — Validate Zero Errors
 
Confirm:
- Build tool exits with code `0`.
- Output contains `Build succeeded` (dotnet CLI).
- `0 Error(s)` in the summary line.

---
 
## Step 7 — Capture Warning Baseline
 
Record the count and category of remaining warnings after the zero-error build. This becomes the warning baseline for the project. Include in `html-report-generator` output.
 
---
 
## Step 8 — Produce Web Deploy Package (Optional)
 
If deployment packaging is requested:
 
```powershell
dotnet publish "BSESystem.csproj" `
  --configuration Release `
  --output ".\artifacts" `
  --no-build

---
 
## Outputs
 
| Output | Description |
|---|---|
| `build-output.log` | Raw MSBuild output |
| `build-validation-report.json` | Structured: error count, warning count, errors list, warnings list, exit code |
| Zero-error confirmation | Boolean flag consumed by `html-report-generator` |
 
---
 
## Constraints

- **Always** use `dotnet build` and `dotnet restore` — never use `msbuild` or `nuget.exe restore` for SDK-style `.NET 10` C# projects.
- **Can run on Linux or Windows** — .NET 10 SDK is cross-platform.
- Never suppress compiler warnings by adding `#pragma warning disable` without documenting the reason in the refactor report.
- Never mark the build as passing (`buildStatus: "success"`) when error count is greater than zero.
- Do **not** attempt to fix errors in `*.g.cs` or auto-generated files under `obj/` — regenerate those files instead.