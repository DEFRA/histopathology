# Skill: nuget-package-upgrade
 
## Purpose
Upgrade all NuGet package references in the solution to versions compatible with the target framework. Handles two formats:
 
- **Classic mode** (`packages.config`): edits `packages.config` and updates `<Reference>` `<HintPath>` values in project files.
- **Modern mode** (`PackageReference`): migrates from `packages.config` to inline `<PackageReference>` items in SDK-style project files, or upgrades existing `PackageReference` versions.
 
Read `upgrade-path-analysis-report.json` before executing — use `pathType` to select the correct mode.
 
---
 
## Mode A — Classic packages.config Upgrade (Classic → Classic)
 
### A1 — Discover All packages.config Files
 
Scan the `solutionFolder` recursively for `packages.config`. For each file, extract every `<package>` entry: `id`, `version`, `targetFramework`.
 
Produce a flat table: **Package ID | Current Version | Current targetFramework | Project**.
 
### A2 — Identify Packages Requiring Upgrade
 
Apply rules in order:
 
1. **Framework mismatch**: `targetFramework` not in `{net48, net472, net471, net47, net462, net461, net46}` → flag.
2. **Version behind**: more than one major version behind latest stable release targeting `net48` → flag.
3. **Known incompatible versions** — minimum required versions:
 
| Package ID | Minimum net48-Compatible Version | Notes |
|---|---|---|
| `Microsoft.AspNet.WebPages` | 3.3.0 | |
| `Microsoft.Web.Infrastructure` | 2.0.0 | |
| `Microsoft.ApplicationInsights` | 2.22.0 | |
| `Microsoft.ApplicationInsights.Web` | 2.22.0 | |
| `Newtonsoft.Json` | 13.0.3 | Security backports in 13.x |
| `EntityFramework` | 6.4.4 | Last EF6 release; do **not** upgrade to EF Core |
| `Microsoft.Owin` | 4.2.2 | |
| `Microsoft.Owin.Security` | 4.2.2 | |
| `System.IdentityModel.Tokens.Jwt` | 6.35.0 | |
| `log4net` | 2.0.15 | |
| `NLog` | 5.2.8 | |
 
4. **CVE-flagged**: any package version with a known CVE → flag as **critical**.
 
Do **not** upgrade `EntityFramework` beyond `6.x` — EF Core targets .NET 5+ and is incompatible with classic .NET Framework projects.
 
### A3 — Update packages.config
 
```xml
<!-- Before -->
<package id="Newtonsoft.Json" version="9.0.1" targetFramework="net462" />
 
<!-- After -->
<package id="Newtonsoft.Json" version="13.0.3" targetFramework="net48" />
```
 
### A4 — Update Project File References
 
```xml
<!-- Before -->
<Reference Include="Newtonsoft.Json, Version=9.0.1.0, ...">
  <HintPath>..\packages\Newtonsoft.Json.9.0.1\lib\net45\Newtonsoft.Json.dll</HintPath>
</Reference>
 
<!-- After -->
<Reference Include="Newtonsoft.Json, Version=13.0.0.0, ...">
  <HintPath>..\packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll</HintPath>
</Reference>
```
 
### A5 — Run NuGet Restore
 
```powershell
nuget restore "<solution-file>.sln"
```
 
> Use `nuget.exe` — **not** `dotnet restore`. The `dotnet restore` command does not process `packages.config` files.
 
Confirm all packages download. If any fails, report the package as a blocking issue and revert that entry.
 
### A6 — Update Assembly Binding Redirects
 
For each version-changed package, add or update `<bindingRedirect>` in `web.config`:
 
```xml
<dependentAssembly>
  <assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30ad4fe6b2a6aeed" culture="neutral" />
  <bindingRedirect oldVersion="0.0.0.0-13.0.0.0" newVersion="13.0.0.0" />
</dependentAssembly>
```
 
---
 
## Mode B — PackageReference Migration (Classic → Modern)
 
### B1 — Map packages.config to PackageReference
 
Convert every `<package>` entry in `packages.config` to a `<PackageReference>` item in the SDK-style project file.
 
Remove `packages.config` entirely after migration is confirmed.
 
```xml
<!-- packages.config entry -->
<package id="Newtonsoft.Json" version="9.0.1" targetFramework="net462" />
 
<!-- SDK project PackageReference -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```
 
### B2 — Resolve Target Framework-Compatible Versions
 
For the target framework (e.g., `net10.0`), resolve the latest stable version of each package that provides a compatible target:
 
| Priority | Compatible Target |
|---|---|
| 1 | Exact match: e.g., `net10.0` |
| 2 | `net9.0`, `net8.0`, `net7.0`, `net6.0` |
| 3 | `netstandard2.1`, `netstandard2.0` |
 
Do **not** select a package version whose minimum required framework is higher than the target.
 
### B3 — Handle Packages with No Modern Equivalent
 
Some packages available for .NET Framework have no compatible release for .NET 5+. For each:
 
| Package | Modern Replacement |
|---|---|
| `EntityFramework` (EF6) | `Microsoft.EntityFrameworkCore` (with compatible provider) |
| `System.Web.Http` (WebAPI 2) | `Microsoft.AspNetCore.Mvc` (built-in) |
| `Microsoft.AspNet.WebPages` | `Microsoft.AspNetCore.Mvc.RazorPages` (built-in) |
| `Microsoft.Owin` / `Owin` | ASP.NET Core middleware pipeline (built-in) |
| `log4net` | `Microsoft.Extensions.Logging` + Serilog/NLog provider |
| `Common.Logging` | `Microsoft.Extensions.Logging.Abstractions` |
| `Castle.Windsor` | `Microsoft.Extensions.DependencyInjection` (built-in DI) |
 
Where no replacement is available and the package is required, log a **blocking issue** with a recommended alternative and do not proceed until resolved.
 
### B4 — Run dotnet restore
 
```bash
dotnet restore "<solution-file>.sln"
```
 
Confirm exit code is `0`. Report failed packages as blocking issues.
 
### B5 — Remove packages.config and packages/ Directory
 
After confirming all `PackageReference` items resolve correctly:
- Delete `packages.config` from each project directory.
- Add `packages/` to `.gitignore` if not already present (the NuGet package cache is not committed to source control).
 
---
 
## Mode C — SDK-to-SDK Upgrade (SDK → SDK)
 
### C1 — Update TargetFramework
 
In each SDK-style project file, update:
 
```xml
<!-- Before -->
<TargetFramework>net8.0</TargetFramework>
 
<!-- After -->
<TargetFramework>net10.0</TargetFramework>
```
 
### C2 — Upgrade PackageReference Versions
 
For each `<PackageReference>` where a newer version is available for the target framework:
 
```xml
<!-- Before -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
 
<!-- After -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
```
 
### C3 — Run dotnet restore and Update Implicit Usings
 
After upgrading packages, run:
 
```bash
dotnet restore
dotnet build --configuration Release
```
 
Address any `CS0246` (type not found) or `CS8795` (partial method) errors that arise from package API changes between versions.
 
---
 
## Outputs
 
| Output | Description |
|---|---|
| `nuget-upgrade-report.json` | Structured log: package ID, old version, new version, CVE flags, mode used, blocking issues |
| Updated `packages.config` (Mode A) | All modified |
| Updated project file `<Reference>` / `<PackageReference>` items | All modified |
| Updated `web.config` binding redirects (Mode A) | All modified |
| Deleted `packages.config` (Mode B) | Confirmed removed |
 
---
 
## Constraints
 
- Do **not** use `dotnet add package` or `dotnet restore` for Mode A (classic `packages.config`).
- Do **not** upgrade `EntityFramework` beyond `6.x` for Mode A — EF Core is a separate package and a separate migration effort.
- Do **not** remove a package without confirming it is not referenced in source code.
- Do **not** select a version whose lowest supported framework is higher than the target.
- CVE-flagged packages are **critical priority** — they must be upgraded before any deployed build.