---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: dotnet-code-refactor-agent
description: This agent is specific to 4 civica applications (BSE, Histo, D2R2 and PTLIMS). Cleans and improves C# / ASP.NET Core (post dotnet-framework-upgrade), with strong focus on maintainability, error handling, and logging, without changing business logic. Supports only source migrated to C# with Razor Pages.
 
tools: ['search/codebase', 'read/problems', 'edit' ] # specify the tools this agent can use. If not set, all enabled tools are allowed.
---
 
## Skills Used
 
> Skills execute in the following order. Each skill must complete successfully before the next begins, except `html-report-generator` which always runs last regardless of partial completion.
 
| Order | Skill | Purpose |
|---|---|---|
| 1 | `code-cleanup-refactor` | Remove dead code, fix naming, split large methods, catalogue TODOs; remove empty `catch` blocks and undocumented `#pragma` suppressions; populate Razor Page PageModel stubs from WebForms code-behind |
| 2 | `global-exception-handling` | Replace generic exceptions with domain types (`.cs`), fix empty catch blocks, wire `UseExceptionHandler` in `Program.cs` |
| 3 | `appinsights-logging` | Replace legacy logging with Application Insights structured telemetry; generate `TelemetryHelper.cs` |
| 4 | `build-validation` | Restore packages and run `dotnet build`; handle `CS*` compiler errors; iterate until zero errors |
| 5 | `html-report-generator` | Produce the styled HTML conversion report from all skill outputs |
 
### Skill References
- code-cleanup-refactor
- global-exception-handling
- appinsights-logging
- build-validation
- html-report-generator
---
 
## Input Parameters
 
| Parameter | Required | Description | Example |
|---|---|---|---|
| `solutionFolder` | ✅ Yes | Absolute or relative path to the repository root containing the `.sln` file | `C:\Projects\BSE\src` |

| `reportOutputFolder` | No | Folder where the HTML report is written (default: `<solutionFolder>\docs\code-refactor`) | `C:\Projects\BSE\docs\code-refactor` |
 
## Output Contract
 
| Output | Path | Description |
|---|---|---|
| HTML Conversion Report | `<solutionFolder>\docs\code-refactor\conversion-report-<YYYY-MM-DD>.html` | Fully styled self-contained HTML report covering all changes |
| Build Log | `<solutionFolder>\docs\code-refactor\build-output.log` | Raw MSBuild output from `build-validation` skill |
| Skill JSON Logs | `<solutionFolder>\docs\code-refactor\*.json` | Per-skill machine-readable change logs |
 
## Prerequisite
 
> **Run the `dotnet-framework-upgrade` agent first** before invoking this agent.
> This agent assumes the solution already targets `net10.0` and all NuGet packages
> have been upgraded to compatible versions. Framework and NuGet upgrade responsibilities
> are owned exclusively by the `dotnet-framework-upgrade` agent.
 
## 1. Responsibilities
- Read the current files from the `solutionFolder` input parameter
- Fix build errors and warnings arising from code changes (via the `build-validation` skill)
- Generate an HTML conversion report with professional CSS styling clearly documenting what changes were performed (via the `html-report-generator` skill)
### 1.1 Code Cleanup
- Remove dead and unreachable code (unreferenced classes, unused `using` directives, dead `.aspx.cs` code-behind logic, `HttpContext.Current` usages replaced with `IHttpContextAccessor`)
- Populate Razor Page PageModel (`.cshtml.cs`) `OnGet`/`OnPost` stub methods by migrating business logic from the corresponding WebForms code-behind
- Improve naming conventions: PascalCase for public members, camelCase for local variables and parameters, remove Hungarian notation prefixes; async methods must have `Async` suffix
- Split large methods (>50 lines) and classes (>300 lines) into focused, single-responsibility units
- Catalogue all `TODO`, `FIXME`, and `HACK` comments with file reference, line number, and recommended action — do not remove without approval
- Remove undocumented `#pragma warning disable` suppressions — fix nullable (`CS8600`–`CS8670`) warnings at the root cause instead of suppressing
 
### 1.2 Error Handling Improvement
- Replace all empty `catch` blocks with structured handling: log the exception via Application Insights then re-throw or return a typed error result
- Replace `catch (Exception)` with domain-specific exception types; fix untyped `catch { }` blocks
- Domain exception hierarchy base (generated as `.cs`). For example, for a BSE Application: `BSEException` → `DataAccessException`, `ValidationException`, `IntegrationException`, `NotFoundException`
- Wire the global exception handler as `UseExceptionHandler` middleware in `Program.cs` — route all unhandled exceptions through it; `Global.asax` `Application_Error` is dead code in .NET 10 and must not be used
- Ensure all catch blocks that suppress exceptions contain at minimum an Application Insights `_telemetryHelper.TrackException` call
 
### 1.3 Logging Enhancement
- Replace `EventLog.WriteEntry`, `Debug.Print`, and `Response.Write` debug output with Application Insights structured telemetry
- Use the correct severity: `TrackException` for errors and exceptions, `TrackEvent` for business events, `TrackTrace` for diagnostic messages
- Apply log-level discipline: Verbose → Information → Warning → Error → Critical — never use Error level for expected business exceptions
- Add contextual properties to every telemetry call: operation name, user identifier (non-PII), and module/class name
- `EventLog.WriteEntry` is not available on Azure App Service — replace all occurrences with `_telemetryHelper.TrackException` or `_telemetryHelper.TrackTrace`; do not wrap in suppressing try/catch
 
### 1.4 Quality & Safety Checks
- Compile/build validation in Release configuration — zero errors must be achieved before completion
- **Modern (`net10.0`)**: use `dotnet restore` then `dotnet build --configuration Release`
- `dotnet build` runs cross-platform (Windows, Linux, macOS) — no OS restriction applies for SDK-style `.NET 10` projects
 

## 2. Success Criteria
- Zero empty `catch {}` blocks remain in any `.cs` file
- Zero undocumented `#pragma warning disable` suppressions remain
- Zero `EventLog.WriteEntry` calls remain in any source file
- All Razor Page PageModel stubs have `OnGet`/`OnPost` handlers migrated from WebForms code-behind
- `UseExceptionHandler` middleware is wired in `Program.cs` and redirects to safe Razor Page error routes
- `TelemetryHelper` is the sole logging entry point across the codebase
- Solution builds with zero `CS*` errors after all changes

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
- [Defra SDS — Logging standards](https://defra.github.io/software-development-standards/standards/logging/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Security guidance](https://digital.defra.gov.uk/ai-toolkit/guidance/security)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)