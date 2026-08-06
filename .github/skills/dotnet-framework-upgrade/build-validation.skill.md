# Skill: build-validation
 
## Purpose
Validate that the solution compiles cleanly after all framework upgrade and NuGet changes have been applied. Supports both the classic MSBuild toolchain (for .NET Framework targets) and the .NET CLI toolchain (for .NET 5+ targets). Iterates until zero compiler errors are achieved.
 
Read `upgrade-path-analysis-report.json` before executing — use `pathType` to select the correct build tool.
 
---
 
## Mode A — Classic Build (Classic → Classic, targeting .NET Framework)
 
### A1 — Restore NuGet Packages
 
```powershell
nuget restore "<solution-file>.sln"
```
 
> **Must use `nuget.exe`** — not `dotnet restore`. The `dotnet restore` command does not support `packages.config`-style solutions.
 
Confirm exit code is `0`. If restore fails, report each failed package as a **P0 blocking issue** and halt.
 
### A2 — Run MSBuild in Release Configuration
 
```powershell
msbuild "<solution-file>.sln" `
  /p:Configuration=Release `
  /p:Platform="Any CPU" `
  /p:DeployOnBuild=false `
  /t:Build `
  /nologo `
  /v:minimal `
  2>&1 | Tee-Object -FilePath build-output.log
```
 
> **Must use MSBuild** — not `dotnet build`. The `dotnet build` command does not support `System.Web`-based projects.
>
> **Must run on Windows** — the .NET Framework build toolchain is not available on Linux.
 
### A3 — Parse and Categorise Errors
 
| Code Prefix | Category | Action |
|---|---|---|
| `MSB*` | MSBuild infrastructure error | Fix project file or `.sln` issue |
| `BC*` | VB.NET compiler error | Fix code |
| `CS*` | C# compiler error | Fix code |
| `warning BC42*` | Unused variable / import | Fix — these are dead code indicators |
| `warning MSB3277` | Assembly version conflict | Add / update binding redirect |
| `warning MSB3245` | Assembly not resolved | Fix `<HintPath>` or re-run NuGet restore |
 
### A4 — Produce Web Deploy Package (Optional)
 
If deployment packaging is requested:
 
```powershell
msbuild "<WebProject>.vbproj" `
  /p:Configuration=Release `
  /p:DeployOnBuild=true `
  /p:WebPublishMethod=Package `
  /p:PackageAsSingleFile=true `
  /p:SkipInvalidConfigurations=true `
  /p:PackageLocation=".\artifacts" `
  /nologo
```
 
---
 
## Mode B — Modern Build (Classic → Modern or SDK → SDK, targeting .NET 5+)
 
### B1 — Restore Packages
 
```bash
dotnet restore "<solution-file>.sln"
```
 
Confirm exit code is `0`. If restore fails, report each unresolved package as a **P0 blocking issue**.
 
### B2 — Build in Release Configuration
 
```bash
dotnet build "<solution-file>.sln" \
  --configuration Release \
  --no-restore \
  2>&1 | tee build-output.log
```
 
Can run on Windows or Linux. For CI/CD pipelines, `ubuntu-latest` runners are valid for .NET 5+ builds.
 
### B3 — Parse and Categorise Errors
 
| Code Prefix | Category | Action |
|---|---|---|
| `CS*` | C# compiler error | Fix code |
| `NETSDK*` | .NET SDK tooling error | Fix SDK version, `global.json`, or project file |
| `NU*` | NuGet resolution error | Fix `PackageReference` version or source |
| `warning CS8600–CS8670` | Nullable reference warning | Address in touched files; do not suppress |
| `warning SYSLIB*` | Obsolete API warning | Plan replacement; document in upgrade report |
 
### B4 — Run Tests (if test projects exist)
 
```bash
dotnet test "<solution-file>.sln" \
  --configuration Release \
  --no-build \
  --logger "trx;LogFileName=test-results.trx"
```
 
Report test failures separately from compiler errors — a test failure does not block the build validation step but is logged in the upgrade report.
 
---
 
## Iterative Fix Loop (Both Modes)
 
For each remaining compiler error after initial build:
 
1. Identify file, line, and error code.
2. Determine root cause:
   - **Missing `Imports` / `using`** — namespace moved in upgraded package.
   - **Changed constructor or method signature** — API change in upgraded package.
   - **Renamed type** — check package release notes for the version delta.
   - **Ambiguous reference** — two packages now expose the same type.
   - **Obsolete API removed** — use the recommended modern API.
3. Apply the minimal fix.
4. Re-run build.
5. Repeat until zero errors.
 
**Cap**: If errors do not reach zero after 5 iterations, surface all remaining errors as **blocking issues** in the upgrade report and halt. Do not apply speculative fixes beyond this point.
 
---
 
## Zero-Error Gate
 
Confirm:
- Build tool exits with code `0`.
- Output contains `0 Error(s)` (MSBuild) or `Build succeeded` (dotnet CLI).
- All `MSB3245` (unresolved assembly) warnings are eliminated.
 
If zero-error state cannot be achieved, log all unresolved errors in `build-validation-report.json` and set `buildStatus: "blocked"`.
 
---
 
## Outputs
 
| Output | Description |
|---|---|
| `build-output.log` | Raw build tool output |
| `build-validation-report.json` | `buildStatus`, error count, warning count, error list, warning baseline, test results (if applicable) |
 
---
 
## Constraints
 
- **Mode A**: Never use `dotnet build` or `dotnet restore`.
- **Mode A**: Never run on a Linux runner — the .NET Framework toolchain is Windows-only.
- **Mode B**: Can use Linux runners for builds targeting .NET 5+.
- Never suppress compiler warnings by adding `#Disable Warning` pragmas without documenting the reason in the upgrade report.
- Never mark the build as passing (`buildStatus: "success"`) when error count is greater than zero.