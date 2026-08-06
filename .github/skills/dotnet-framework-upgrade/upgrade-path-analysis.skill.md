# Skill: upgrade-path-analysis
 
## Purpose
Analyse the solution's current framework version, project file format, and dependency surface to determine the correct upgrade path, classify the complexity tier, and produce a structured upgrade plan before any files are modified.
 
## Trigger Conditions
- Always runs **first**, before any changes are applied.
- Takes `sourceFramework` and `targetFramework` as inputs from the agent.
 
---
 
## Step 1 — Classify the Upgrade Path
 
Based on the `sourceFramework` and `targetFramework` agent parameters, classify into one of three path types:
 
| Path Type | Source | Target | Example |
|---|---|---|---|
| **Classic → Classic** | .NET Framework (any) | .NET Framework 4.8 | `net462` → `net48` |
| **Classic → Modern** | .NET Framework (any) | .NET 5 / 6 / 7 / 8 / 9 / 10 | `net40` → `net10.0` |
| **SDK → SDK** | SDK-style .NET Core / .NET 5+ | Higher .NET version | `net8.0` → `net10.0` |
 
> This skill defines the upgrade path that all subsequent skills must follow. Every other skill reads the `pathType` field from `upgrade-path-analysis-report.json` before executing.
 
---
 
## Step 2 — Discover Project Structure
 
Scan the `solutionFolder` recursively and catalogue:
 
| Artefact | Look For |
|---|---|
| Solution file | `*.sln` |
| Classic project files | `*.vbproj`, `*.csproj` containing `<TargetFrameworkVersion>` |
| SDK-style project files | `*.csproj` containing `<Project Sdk="` |
| Package manifests | `packages.config` (classic) or `<PackageReference>` (SDK) |
| Web application entry points | `web.config`, `Global.asax`, `Program.cs`, `Startup.cs` |
| Windows Service projects | `*.csproj` with `OutputType=Exe` and `ServiceBase` reference |
| Test projects | `*Test*.csproj`, `*Tests*.csproj` |
| Database projects | `*.sqlproj` |
 
---
 
## Step 3 — Inventory High-Risk Dependencies
 
For **Classic → Modern** paths only, scan for patterns that have no direct equivalent in .NET 5+:
 
| Risk Item | Detection Pattern | Migration Complexity |
|---|---|---|
| `System.Web` namespace | `Imports System.Web` / `using System.Web` | **High** — no equivalent; must migrate to ASP.NET Core |
| WebForms pages | `*.aspx`, `*.ascx`, `*.master` files | **High** — WebForms is not available on .NET 5+ |
| `Global.asax` | File presence | **Medium** — replace with `Program.cs` middleware |
| `HttpContext.Current` | Static access pattern | **Medium** — replace with `IHttpContextAccessor` |
| `Session["key"]` | `System.Web.SessionState` | **Medium** — replace with `ISession` |
| `Response.Redirect` | Direct `Response` access | **Low** — replace with return type redirects |
| Windows Authentication | `WindowsAuthenticationModule` | **Medium** — requires IIS/Kerberos config on Kestrel |
| COM interop | `<COMReference>` in project file | **High** — COM cannot run in .NET 5+ on Linux |
| `packages.config` | File presence | **Medium** — must migrate to `PackageReference` |
| `System.Configuration.ConfigurationManager` | Namespace usage | **Low** — available via NuGet on .NET 5+ |
| `System.Web.Mail` / `SmtpClient` | Namespace usage | **Low** — `SmtpClient` is obsolete; migrate to MailKit |
| P/Invoke / unmanaged DLLs | `DllImport` attribute | **High** — DLL must target same bitness and OS |
| WCF services (server-side) | `ServiceHost`, `*.svc` files | **High** — `System.ServiceModel` server not in .NET 5+ |
 
Produce a **risk matrix** for every high-risk item found: file path, line number, risk level, recommended action.
 
---
 
## Step 4 — Produce Upgrade Plan
 
Output a structured upgrade plan with phases:
 
### Classic → Classic Upgrade Plan
 
```
Phase 1: framework-upgrade skill
  - Bump TargetFrameworkVersion to v4.8
  - Update httpRuntime targetFramework to 4.8
  - Add requestValidationMode="2.0"
 
Phase 2: nuget-package-upgrade skill
  - Identify outdated packages.config entries
  - Resolve net48-compatible versions
  - Run nuget restore
 
Phase 3: build-validation skill
  - Run MSBuild Release build
  - Fix compiler errors iteratively
  - Achieve zero errors
 
Phase 4: upgrade-report-generator skill
  - Produce conversion HTML report
```
 
### Classic → Modern Upgrade Plan
 
```
Phase 1: upgrade-path-analysis (this skill — completed)
 
Phase 2: framework-upgrade skill (classic→modern mode)
  - Convert project file from classic to SDK-style
  - Replace system-level WebForms dependencies with stubs or equivalents
  - Migrate web.config sections to appsettings.json
  - Migrate Global.asax to Program.cs / middleware
 
Phase 3: nuget-package-upgrade skill (PackageReference mode)
  - Remove packages.config
  - Add PackageReference items for all dependencies
  - Find .NET 10-compatible package versions
 
Phase 4: build-validation skill (dotnet CLI mode)
  - Run dotnet restore + dotnet build --configuration Release
  - Fix compiler errors iteratively
 
Phase 5: upgrade-report-generator skill
  - Produce conversion HTML report with migration notes
```
 
---
 
## Step 5 — Estimate Effort
 
Based on inventory counts, provide an effort estimate guideline:
 
| Metric | Weight |
|---|---|
| Number of `.aspx`/`.ascx` pages (Classic→Modern) | 1–2 days per 10 pages |
| Number of WCF services | 0.5–1 day per service |
| Number of COM references | 1–3 days per COM dependency |
| Number of packages to upgrade | 0.25 days per package |
| High risk items count | +20% buffer per item |
 
> These are approximate planning figures only — not delivery commitments.
 
---
 
## Outputs
 
| Output | Description |
|---|---|
| `upgrade-path-analysis-report.json` | JSON: `pathType`, project inventory, risk matrix, upgrade plan phases, effort estimate |
 
### JSON Schema (excerpt)
```json
{
  "pathType": "ClassicToClassic | ClassicToModern | SdkToSdk",
  "sourceFramework": "net462",
  "targetFramework": "net48",
  "projects": [
    { "path": "src/MyApp.vbproj", "format": "classic", "currentVersion": "v4.6.2" }
  ],
  "highRiskItems": [
    { "file": "src/Pages/SlideList.aspx", "pattern": "WebForms", "risk": "High", "action": "Migrate to Razor Pages" }
  ],
  "upgradePlan": { "phases": [] },
  "estimatedEffortDays": 5
}
```
 
---
 
## Constraints
 
- This skill is **read-only** — it makes no changes to any file.
- Do **not** begin any migration activity until this skill has completed and produced its report.
- If `pathType` is `ClassicToModern` and more than 50 WebForms pages are detected, surface a **confirmation gate** before proceeding — this is a high-effort project-scope migration.