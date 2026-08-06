# .NET Code Refactor Agent — What It Does & Change Log

**File:** `.github/agents/dotnet-code-refactor.agent.md`  
**Last reviewed:** 2026-07-31  
**Owner:** LAP Civica Migration Team  
**Standards alignment:** Defra SDS · Defra AI Toolkit — Deliver with AI

---

## What This Agent Does

### Overview

The **.NET Code Refactor Agent** cleans and improves C# / ASP.NET Core code after the `dotnet-framework-upgrade` agent has run. It focuses on maintainability, error handling, and structured logging — without changing business logic. Specific to the four Civica applications: **BSE, Histo, D2R2, and PTLIMS**.

> **Prerequisite:** Run the `dotnet-framework-upgrade` agent first. This agent assumes the solution already targets `net10.0` with NuGet packages upgraded.

---

### Skills (Executed in Order)

| Order | Skill | Purpose |
|---|---|---|
| 1 | `code-cleanup-refactor` | Remove dead code, fix naming, split large methods, catalogue TODOs; remove empty `catch` blocks and undocumented `#pragma` suppressions; populate Razor Page PageModel stubs from WebForms code-behind |
| 2 | `global-exception-handling` | Replace generic exceptions with domain types (`.cs`), fix empty catch blocks, wire `UseExceptionHandler` in `Program.cs` |
| 3 | `appinsights-logging` | Replace legacy logging with Application Insights structured telemetry; generate `TelemetryHelper.cs` |
| 4 | `build-validation` | Restore packages and run `dotnet build`; handle `CS*` compiler errors; iterate until zero errors |
| 5 | `html-report-generator` | Produce the styled HTML conversion report from all skill outputs (always runs last) |

Each skill must complete successfully before the next begins. `html-report-generator` always runs last regardless of partial completion.

---

### Inputs Required

| Parameter | Required | Description | Example |
|---|---|---|---|
| `solutionFolder` | ✅ Yes | Absolute or relative path to the repository root | `C:\Projects\BSE\src` |
| `reportOutputFolder` | No | Output folder for HTML report (default: `<solutionFolder>\docs\code-refactor`) | `C:\Projects\BSE\docs\code-refactor` |

---

### Outputs Produced

| Output | Path | Description |
|---|---|---|
| HTML Conversion Report | `<solutionFolder>\docs\code-refactor\conversion-report-<YYYY-MM-DD>.html` | Fully styled self-contained HTML report covering all changes |
| Build Log | `<solutionFolder>\docs\code-refactor\build-output.log` | Raw MSBuild output from `build-validation` skill |
| Skill JSON Logs | `<solutionFolder>\docs\code-refactor\*.json` | Per-skill machine-readable change logs |

---

### Success Criteria

- Zero empty `catch {}` blocks remain in any `.cs` file
- Zero undocumented `#pragma warning disable` suppressions remain
- Zero `EventLog.WriteEntry` calls remain in any source file
- All Razor Page PageModel stubs have `OnGet`/`OnPost` handlers migrated from WebForms code-behind
- `UseExceptionHandler` middleware is wired in `Program.cs`
- `TelemetryHelper` is the sole logging entry point across the codebase
- Solution builds with zero `CS*` errors after all changes

---

### Guardrails Summary

**The agent will:**
- Read code from `solutionFolder` before making any changes
- Fix build errors arising from code changes via the `build-validation` skill
- Generate a styled HTML report documenting all changes

**The agent will never:**
- Change business logic — only code quality and structure
- Modify `.designer.vb`, `.designer.cs`, or auto-generated files
- Suppress compiler warnings without documentation

---

### Compliance Profile

| Dimension | Classification / Status |
|---|---|
| AI Toolkit risk level | MEDIUM |
| Human oversight required | Yes — review HTML report before merging |
| Defra SDS standards applied | GitHub Copilot guide, C# coding standards, Security standards, Logging standards, Git branching |

---

## Change Log

### v1.0 — 2026-07-31 — Initial Guide

Initial creation of the .NET Code Refactor agent guide.

---

## References

- [`.github/agents/dotnet-code-refactor.agent.md`](./../agents/dotnet-code-refactor.agent.md) — agent definition
- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — C# coding standards](https://defra.github.io/software-development-standards/standards/csharp_coding_standards/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Logging standards](https://defra.github.io/software-development-standards/standards/logging/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)

---

*This document is maintained alongside `.github/agents/dotnet-code-refactor.agent.md`. Update the change log and "Last reviewed" date whenever the agent is modified.*
