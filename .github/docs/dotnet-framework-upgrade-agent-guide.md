# .NET Framework Upgrade Agent — What It Does & Change Log

**File:** `.github/agents/dotnet-framework-upgrade.agent.md`  
**Last reviewed:** 2026-07-31  
**Owner:** LAP Civica Migration Team  
**Standards alignment:** Defra SDS · Defra AI Toolkit — Deliver with AI

---

## What This Agent Does

### Overview

The **.NET Framework Upgrade Agent** upgrades a .NET solution from any source framework version to a specified target version. It handles project file format conversion, NuGet package resolution, build validation, and HTML report generation. Specific to the four Civica applications: **BSE, Histo, D2R2, and PTLIMS**.

---

### Supported Upgrade Paths

| Scenario | `sourceFramework` | `targetFramework` | Path Type |
|---|---|---|---|
| Legacy WebForms hardening | `net40` | `net48` | Classic → Classic |
| Full framework upgrade | `net462` | `net48` | Classic → Classic |
| Modernisation to LTS | `net462` | `net8.0` | Classic → Modern |
| Modernisation to latest | `net40` | `net10.0` | Classic → Modern |
| SDK version bump | `net8.0` | `net10.0` | SDK → SDK |

---

### Skills (Executed in Order)

| Order | Skill | Purpose |
|---|---|---|
| 1 | `upgrade-path-analysis` | Classify upgrade path, inventory projects, identify high-risk items, produce upgrade plan |
| 2 | `framework-upgrade` | Apply framework version changes to project files and configuration |
| 3 | `nuget-package-upgrade` | Resolve and apply compatible NuGet package versions for the target framework |
| 4 | `build-validation` | Restore packages and build; iterate until zero errors |
| 5 | `upgrade-report-generator` | Produce the styled HTML upgrade report from all skill outputs (always runs last) |

Each skill must complete before the next begins. `upgrade-report-generator` always runs last.

---

### Inputs Required

| Parameter | Required | Description | Example |
|---|---|---|---|
| `solutionFolder` | ✅ Yes | Absolute path to repository root containing `.sln` | `C:\Projects\bse` |
| `sourceFramework` | ✅ Yes | Current framework moniker | `net40`, `net48`, `net8.0` |
| `targetFramework` | ✅ Yes | Target framework moniker | `net48`, `net10.0` |
| `reportOutputFolder` | No | Output folder for HTML report (default: `<solutionFolder>\upgrade-reports`) | `C:\Projects\bse\upgrade-reports` |

---

### Outputs Produced

| Output | Path | Description |
|---|---|---|
| HTML Upgrade Report | `<reportOutputFolder>\upgrade-report-<YYYY-MM-DD>.html` | Self-contained styled HTML report |
| Build Log | `<solutionFolder>\build-output.log` | Raw build tool output |
| Skill JSON Logs | `<reportOutputFolder>\*.json` | Per-skill machine-readable change logs |

> **Note:** `upgrade-reports/` must be added to `.gitignore` — report files must not be committed to source control.

---

### Guardrails Summary

**The agent will:**
- Never hardcode secrets — use placeholder tokens sourced from Key Vault or environment variables
- Respect the upgrade path classification — never apply Classic→Modern steps on a Classic→Classic path
- Surface all blocking issues in the upgrade report with recommended resolutions
- Cap iterative build fix attempts at 5 rounds — surface remaining errors and halt

**The agent will never:**
- Modify `.designer.vb`, `.designer.cs`, or auto-generated files
- Delete WebForms pages — create stub replacements instead
- Proceed past a blocking issue silently

---

### Compliance Profile

| Dimension | Classification / Status |
|---|---|
| AI Toolkit risk level | MEDIUM |
| Human oversight required | Yes — review HTML report before merging |
| Defra SDS standards applied | GitHub Copilot guide, C# coding standards, Security standards, Git branching |

---

## Change Log

### v1.0 — 2026-07-31 — Initial Guide

Initial creation of the .NET Framework Upgrade agent guide.

---

## References

- [`.github/agents/dotnet-framework-upgrade.agent.md`](./../agents/dotnet-framework-upgrade.agent.md) — agent definition
- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — C# coding standards](https://defra.github.io/software-development-standards/standards/csharp_coding_standards/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)

---

*This document is maintained alongside `.github/agents/dotnet-framework-upgrade.agent.md`. Update the change log and "Last reviewed" date whenever the agent is modified.*
