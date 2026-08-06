---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
skill:
  id: dotnet-code-refactor
  name: .NET Code Refactor
  version: 1.0.0
  owner: Platform/Engineering
  intent: >
    Improve the maintainability, reliability, and observability of migrated C# / ASP.NET Core
    codebases without altering business logic. Removes dead code, enforces naming
    conventions, replaces unstructured error handling with a domain exception hierarchy,
    and wires Application Insights structured telemetry throughout the application.
  recommended_agent: dotnet-code-refactor-agent
  scope:
    includes:
      - Dead code removal and naming convention enforcement
      - Empty catch block removal and undocumented `#pragma warning disable` suppression removal
      - Domain exception hierarchy creation and generic catch replacement
      - Global exception handler wiring in Program.cs via UseExceptionHandler middleware
      - Application Insights NuGet installation and TelemetryHelper DI setup
      - EventLog.WriteEntry replacement with structured telemetry calls
      - Populating Razor Page PageModel stubs (OnGet/OnPost) migrated from WebForms code-behind
    excludes:
      - Business logic changes of any kind
      - Framework version upgrades (use dotnet-framework-upgrade skill)
      - Authentication / identity changes (use identity-migration-dotnet skill)
      - Auto-generated files (*.designer.vb, *.g.vb, AssemblyInfo.vb)
      - Secret values — must never be written to any file
  inputs_required:
    - solutionFolder: "Absolute path to the repository root containing the .sln file"
    - targetFramework: "Target framework moniker, e.g. net10.0"
  outputs:
    - BSESystem/Exceptions/BSESystemExceptions.cs
    - BSESystem/Logging/TelemetryHelper.cs
    - appsettings.json (ApplicationInsights.ConnectionString placeholder added)
    - Program.cs (UseExceptionHandler middleware and TelemetryHelper DI registration added)
    - docs/code-refactor/code-cleanup-report.json
    - docs/code-refactor/exception-handling-report.json
    - docs/code-refactor/logging-enhancement-report.json
    - docs/code-refactor/build-output.log
    - docs/code-refactor/conversion-report-<YYYY-MM-DD>.html
    - All modified .cs source files (updated in place)
  success_criteria:
    - Zero empty catch {} blocks remain in any .cs file
    - Zero undocumented #pragma warning disable suppressions remain
    - Zero EventLog.WriteEntry calls remain in any source file
    - UseExceptionHandler middleware is wired in Program.cs and redirects to safe Razor Page error routes
    - TelemetryHelper is the sole logging entry point across the codebase
    - All Razor Page PageModel stubs have OnGet/OnPost handlers migrated from WebForms code-behind
    - Solution builds with zero errors after all changes
  safety:
    - Never change business logic or authorization outcomes.
    - Never modify *.designer.cs, *.g.cs, or AssemblyInfo.cs files.
    - Never modify the HTML structure of .cshtml view stubs — only populate PageModel (.cshtml.cs) OnGet/OnPost handlers.
    - Never write secret values (connection strings, keys, passwords) to any file.
    - Never expose stack traces or SQL error messages in user-facing pages.
    - Never suppress auth-related exceptions from ASP.NET Core authentication or SAML middleware.
---
 
# .NET Code Refactor (Skill)
 
This skill provides a structured, repeatable three-phase workflow to clean, harden, and
instrument C# `.NET 10` / ASP.NET Core  codebases for Azure App Service hosting.
Each phase must complete before the next begins.
 
## When to use this skill
Use this skill after `framework-upgrade` and `nuget-package-upgrade` have completed and
the solution builds cleanly against the target framework. Do not use it on code that still
contains unresolved build errors — fix the build first.
 
## Execution Order
 
Run the sub-skills in the following sequence — each depends on outputs from the previous:
 
| Order | Skill file | Purpose |
|---|---|---|
| 1 | [code-cleanup-refactor.md](./code-cleanup-refactor.md) | Remove dead code, enforce naming, split large methods, remove empty catch blocks and undocumented `#pragma` suppressions, populate Razor Page stubs |
| 2 | [global-exception-handling.md](./global-exception-handling.md) | Define domain exception hierarchy in C#, replace generic catches, wire `UseExceptionHandler` in `Program.cs` |
| 3 | [appinsights-logging.md](./appinsights-logging.md) | Install Application Insights packages, create `TelemetryHelper.cs` (DI-registered), replace all legacy logging |
 
## Inputs

| Input | Required | Description |
|---|---|---|
| `solutionFolder` | Yes | Absolute path to the repository root |
| `targetFramework` | Yes | Target framework moniker (e.g. `net10.0`) |
| `BSESystem/Exceptions/BSESystemExceptions.cs` (language=cs) | global-exception-handling |
| `docs/code-refactor/exception-handling-report.json` | global-exception-handling |
| `Program.cs` (UseExceptionHandler wired) | global-exception-handling |
| `BSESystem/Logging/TelemetryHelper.cs` (language=cs) | appinsights-logging |
| `appsettings.json` (ConnectionString placeholder) | appinsights-logging |
| `docs/code-refactor/logging-enhancement-report.json` | appinsights-logging |
| `docs/code-refactor/build-output.log` | build-validation |
| `docs/code-refactor/conversion-report-<YYYY-MM-DD>.html` | html-report-generator |
 
## Safety Guardrails
 
- **Never** change business logic or authorization outcomes.
- **Never** modify auto-generated files (`*.designer.vb`, `*.g.vb`, `AssemblyInfo.vb`).
- **Never** modify the HTML structure of `.cshtml` view stubs — only populate `PageModel` (`.cshtml.cs`) `OnGet`/`OnPost` handlers.
- **Never** write secret values (connection strings, keys, passwords) to any file.
 
---

## Standards

This skill is loaded by the [dotnet-code-refactor agent](./../../agents/dotnet-code-refactor.agent.md). All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/) and [Defra SDS — C# coding standards](https://defra.github.io/software-development-standards/standards/csharp_coding_standards/).