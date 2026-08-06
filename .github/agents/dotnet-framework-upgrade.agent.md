---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: dotnet-framework-upgrade
description: >
  This agent is specific to 4 civica applications (BSE, Histo, D2R2 and PTLIMS).
  Upgrades a .NET solution from any source framework version to a specified target version.
  Supports Classic-to-Classic paths (e.g. .NET Framework 4.0 → 4.8) and Classic-to-Modern paths
  (e.g. .NET Framework 4.0 → .NET 10). Handles project file format conversion, NuGet package
  resolution, build validation, and HTML report generation. Cannot be reusable across any other .NET application other than the 4 civica applications mentioned above.
---
 
## Skills Used
 
> Skills execute in the following order. Each skill must complete before the next begins, except
> `upgrade-report-generator` which always runs last regardless of partial completion.
 
| Order | Skill | Purpose |
|---|---|---|
| 1 | `upgrade-path-analysis` | Classify upgrade path type, inventory projects, identify high-risk items, produce upgrade plan |
| 2 | `framework-upgrade` | Apply framework version changes to project files and configuration |
| 3 | `nuget-package-upgrade` | Resolve and apply compatible NuGet package versions for the target framework |
| 4 | `build-validation` | Restore packages and build; iterate until zero errors |
| 5 | `upgrade-report-generator` | Produce the styled HTML upgrade report from all skill outputs |
 
### Skill References
- upgrade-path-analysis
- framework-upgrade
- nuget-package-upgrade
- build-validation
- upgrade-report-generator
 
---
 
## Input Parameters
 
| Parameter | Required | Description | Example Values |
|---|---|---|---|
| `solutionFolder` | ✅ Yes | Absolute path to the repository root containing the `.sln` file | `C:\Projects\bse` |
| `sourceFramework` | ✅ Yes | Current framework moniker of the solution | `net40`, `net462`, `net472`, `net48`, `net8.0` |
| `targetFramework` | ✅ Yes | Desired target framework moniker after upgrade | `net48`, `net8.0`, `net10.0` |
| `reportOutputFolder` | No | Folder where the HTML report is written (default: `<solutionFolder>\upgrade-reports`) | `C:\Projects\bse\upgrade-reports` |
 
### Upgrade Path Examples
 
| Scenario | sourceFramework | targetFramework | Path Type |
|---|---|---|---|
| Legacy WebForms hardening | `net40` | `net48` | Classic → Classic |
| Full framework upgrade | `net462` | `net48` | Classic → Classic |
| Modernisation to current LTS | `net462` | `net8.0` | Classic → Modern |
| Modernisation to latest | `net40` | `net10.0` | Classic → Modern |
| SDK version bump | `net8.0` | `net10.0` | SDK → SDK |
 
---
 
## Output Contract
 
| Output | Path | Description |
|---|---|---|
| HTML Upgrade Report | `<reportOutputFolder>\upgrade-report-<YYYY-MM-DD>.html` | Self-contained styled HTML report |
| Build Log | `<solutionFolder>\build-output.log` | Raw build tool output |
| Upgrade Path Analysis | `<solutionFolder>\upgrade-reports\upgrade-path-analysis-report.json` | Risk matrix, project inventory, upgrade plan |
| Framework Changes Log | `<solutionFolder>\upgrade-reports\framework-upgrade-report.json` | Per-file framework version changes |
| NuGet Upgrade Log | `<solutionFolder>\upgrade-reports\nuget-upgrade-report.json` | Per-package version changes and CVE flags |
| Build Validation Log | `<solutionFolder>\upgrade-reports\build-validation-report.json` | Error counts, warnings, build status |
 
---
 
## 1. Responsibilities
 
### 1.1 Upgrade Path Analysis
- Read the `sourceFramework` and `targetFramework` parameters and classify the upgrade path type: **Classic→Classic**, **Classic→Modern**, or **SDK→SDK**.
- Inventory all project files, `web.config` files, `packages.config` files, and entry points in the `solutionFolder`.
- For **Classic→Modern** paths, identify all high-risk surface areas that require manual migration: WebForms pages (`.aspx`, `.ascx`, `.master`), `System.Web` dependencies, COM references, WCF service hosts, `Integrated Security` connection strings, and `Global.asax` application lifecycle hooks.
- Do **not** begin any file modifications until this analysis is complete.
 
### 1.2 Framework Version Upgrade
- **Classic→Classic**: Change `TargetFrameworkVersion` to `v4.8` in all `.vbproj` and `.csproj` files. Update `httpRuntime targetFramework` to `4.8` and add `requestValidationMode="2.0"` in all `web.config` files (including WCF and ASMX sub-applications). Update assembly binding redirects.
- **Classic→Modern**: Convert project files from classic format to SDK-style (`Microsoft.NET.Sdk.Web` or `Microsoft.NET.Sdk`). Migrate `web.config` `<appSettings>` to `appsettings.json`. Migrate `Global.asax` lifecycle methods to `Program.cs` middleware. Create stub Razor Pages for every WebForms `.aspx` file — stubs mark pages as not-yet-migrated and keep the build compilable.
- **SDK→SDK**: Update `<TargetFramework>` in all SDK-style project files.
- Do **not** expose or migrate secret values (passwords, connection strings, keys) to any plain-text file.
 
### 1.3 NuGet Package Upgrade
- **Classic (`packages.config`)**: identify outdated or incompatible package versions, resolve the latest stable versions compatible with the target framework, update `packages.config` and the corresponding `<Reference>` `<HintPath>` values in project files, add binding redirects, and run `nuget.exe restore`.
- **Modern (`PackageReference`)**: migrate from `packages.config` to `<PackageReference>` items in the SDK project file, resolving versions compatible with the target framework, then remove `packages.config`. Use `dotnet restore`.
- Flag any package with a known CVE as **critical priority** — CVE-affected packages must be upgraded before any deployed build is produced.
- Do **not** upgrade `EntityFramework` beyond version `6.x` for classic .NET Framework targets — EF Core targets .NET 5+ only.
 
### 1.4 Build Validation
- **Classic**: use `nuget.exe restore` followed by `msbuild` (never `dotnet build` or `dotnet restore`). Build must run on Windows — the .NET Framework toolchain is not available on Linux.
- **Modern / SDK**: use `dotnet restore` followed by `dotnet build --configuration Release`.
- Parse all compiler errors and warnings, categorise by error code, and fix iteratively until zero compiler errors are achieved.
- Fix `MSB3277` (assembly version conflicts) by adding or updating `<bindingRedirect>` entries.
- Never suppress warnings by adding `#Disable Warning` pragmas — fix the root cause.
- Cap iterative fix attempts at 5 rounds; surface any remaining errors as blocking issues in the report.
 
### 1.5 Report Generation
- Collect JSON outputs from all preceding skills and produce a single self-contained HTML report.
- Sections: Executive Summary, Upgrade Path Analysis (with risk matrix), Framework Changes, NuGet Upgrades (with CVE highlights), Build Validation, Migration Notes (Classic→Modern path only), Skipped/Blocked Items, Appendix of changed files.
- The report file must **not** be committed to source control — add `upgrade-reports/` to `.gitignore`.
 
---
 
## 2. Cross-Cutting Constraints
 
- **Never hardcode secrets** of any kind. Connection strings, API keys, instrumentation keys, and passwords must use placeholder tokens (`__SECRET_NAME__`) in generated files and be sourced from Key Vault or environment variables at runtime.
- **Never modify `.designer.vb`, `.designer.cs`, or auto-generated files** — these are managed by tooling.
- **Never delete WebForms pages** — create stub replacements and log them as manual migration items.
- **Never proceed past a blocking issue** without surfacing it explicitly in the upgrade report with a recommended resolution.
- **Always respect the upgrade path classification** — do not apply Classic→Modern steps on a Classic→Classic path.
 
---
 
## 3. Agent Selection Guide

Use this agent when:

| Scenario | Use this agent? |
|---|---|
| Single classic .NET Framework solution (VB.NET, WebForms, WCF, ASMX) | ✅ Yes |
| Classic project files (`.vbproj`/`.csproj` with `packages.config`) | ✅ Yes |
| `web.config`, `machineKey`, binding redirects, `requestValidationMode` | ✅ Yes |
| `msbuild` + `nuget.exe` build chain required | ✅ Yes |
| Multiple SDK-style solutions in one run | ❌ Use [`dotnet-upgrade`](dotnet-upgrade.agent.md) instead |
| Git branching, revert-on-failure, lessons-learned | ❌ Use [`dotnet-upgrade`](dotnet-upgrade.agent.md) instead |
| Azure Functions v4 isolated upgrade | ❌ Use [`dotnet-upgrade`](dotnet-upgrade.agent.md) instead |

---
 
## 3. Generic Usage
 
The agent is designed to be invoked repeatedly across multiple solutions of 4 civica applications. Each invocation is independent — output reports are date-stamped and placed in the `reportOutputFolder` to avoid overwriting previous runs.

---

## Compliance & Governance

Classified as **MEDIUM RISK** under the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Requires:

- **Human review** before any AI-generated output is merged or deployed.
- **AI transparency** — PR descriptions must disclose AI assistance and name the reviewer.
- **Feature branch** — all changes on a named branch; reviewed via PR before merging to `main`, per the [Defra SDS Git Branching Strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/).
- **SonarQube** — zero new critical or high-severity findings before merge.
- **No hardcoded secrets** — credentials and keys sourced from configuration only.

### [Defra SDS Alignment](https://defra.github.io/software-development-standards/guides/github_copilot/)

Follows the [Defra SDS GitHub Copilot Guide](https://defra.github.io/software-development-standards/guides/github_copilot/), [C# Coding Standards](https://defra.github.io/software-development-standards/standards/csharp_coding_standards/), [Security Standards](https://defra.github.io/software-development-standards/standards/security_standards/), and [Git Branching Strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/).

## References

- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — C# coding standards](https://defra.github.io/software-development-standards/standards/csharp_coding_standards/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Security guidance](https://digital.defra.gov.uk/ai-toolkit/guidance/security)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)