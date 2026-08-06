---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
skill:
  id: dotnet-framework-upgrade
  name: .NET Framework Upgrade
  version: 1.0.0
  owner: Implementation Team
  intent: >
    Upgrades a .NET solution from any source framework version to a specified target version.
    Supports Classic-to-Classic paths (e.g. .NET Framework 4.0 → 4.8), Classic-to-Modern paths
    (e.g. .NET Framework 4.0 → .NET 10), and SDK-to-SDK version bumps (e.g. .NET 8 → .NET 10).
    Handles project file format conversion, NuGet package resolution and CVE remediation,
    MSBuild / dotnet CLI build validation, and self-contained HTML report generation.
    Generic and reusable across any .NET application — not tied to any specific application
    name or technology stack.
  recommended_agent: dotnet-framework-upgrade
  execution_model: instruction-only with sequential skill gates
  scope:
    includes:
      - Upgrade path classification (Classic→Classic, Classic→Modern, SDK→SDK)
      - Project file inventory and high-risk dependency analysis
      - TargetFrameworkVersion / TargetFramework version bumps across all project files
      - web.config httpRuntime and requestValidationMode updates
      - Classic-to-SDK project file format conversion
      - Global.asax → Program.cs middleware skeleton generation
      - WebForms stub Razor Page creation (Classic→Modern path only)
      - web.config <appSettings> → appsettings.json migration (Classic→Modern path only)
      - packages.config → PackageReference migration (Classic→Modern path only)
      - NuGet package version resolution for the target framework
      - CVE-flagged package identification and upgrade
      - Assembly binding redirect generation and updating
      - MSBuild restore + build with iterative error fixing (Classic targets)
      - dotnet restore + build with iterative error fixing (Modern/SDK targets)
      - Self-contained HTML upgrade report generation
    excludes:
      - Deployment to Azure App Service or any hosting environment
      - Key Vault secret provisioning or connection string population
      - Automated migration of WebForms page logic to Razor Pages (stub only)
      - Automated WCF server-side migration (System.ServiceModel host)
      - Modification of *.designer.vb, *.designer.cs, or any auto-generated file
      - Secret values in any generated or modified file
  inputs_required:
    - solutionFolder: "Absolute path to the repository root containing the .sln file"
    - sourceFramework: "Current framework moniker, e.g. net40, net462, net48, net8.0"
    - targetFramework: "Target framework moniker, e.g. net48, net8.0, net10.0"
    - reportOutputFolder: "Optional — folder for the HTML report (default: <solutionFolder>\\upgrade-reports)"
  outputs:
    - "upgrade-reports/upgrade-report-<YYYY-MM-DD>.html — self-contained styled HTML report"
    - "build-output.log — raw build tool output"
    - "upgrade-reports/upgrade-path-analysis-report.json — risk matrix, project inventory, upgrade plan"
    - "upgrade-reports/framework-upgrade-report.json — per-file framework version changes"
    - "upgrade-reports/nuget-upgrade-report.json — per-package version changes and CVE flags"
    - "upgrade-reports/build-validation-report.json — error counts, warnings, build status"
  success_criteria:
    - Build tool exits with code 0 and zero compiler errors
    - All six output files written to the reportOutputFolder
    - HTML report renders correctly with all sections populated
    - No CVE-flagged packages remain at the version that carries the CVE
    - upgrade-reports/ added to .gitignore
  safety:
    - Never hardcode secrets of any kind — use placeholder tokens (__SECRET_NAME__) in generated files.
    - Never modify *.designer.vb, *.designer.cs, or auto-generated files.
    - Never delete WebForms pages — create stub Razor Page replacements instead.
    - Never proceed past a blocking issue without surfacing it in the upgrade report.
    - Never apply Classic→Modern steps on a Classic→Classic upgrade path.
    - Never use dotnet build or dotnet restore for Classic (.NET Framework) targets.
    - Never run Classic builds on a Linux runner — the .NET Framework toolchain is Windows-only.
---

# .NET Framework Upgrade (Skill)

This skill package provides a repeatable, path-aware framework upgrade workflow for any .NET solution.
It classifies the upgrade path first, then gates each subsequent skill on the `pathType` field from
`upgrade-path-analysis-report.json`. Each skill must complete before the next begins.

Execution model: **instruction-only with sequential skill gates**.

---

## Upgrade Path Types

| Path Type | Source | Target | Build Tool |
|-----------|--------|--------|------------|
| **Classic → Classic** | .NET Framework (any) | .NET Framework 4.8 | MSBuild + nuget.exe |
| **Classic → Modern** | .NET Framework (any) | .NET 5 / 6 / 7 / 8 / 9 / 10 | dotnet CLI |
| **SDK → SDK** | .NET Core / .NET 5+ | Higher .NET version | dotnet CLI |

> The `upgrade-path-analysis` skill determines `pathType` and writes it to
> `upgrade-path-analysis-report.json`. Every subsequent skill reads this field before
> executing to select the correct mode.

---

## Skills (Playbooks)

| Order | Skill File | Purpose | Key Inputs | Key Outputs |
|-------|-----------|---------|------------|-------------|
| 1 | [upgrade-path-analysis.skill.md](./upgrade-path-analysis.skill.md) | Classify upgrade path, inventory projects, identify high-risk items, produce upgrade plan | `solutionFolder`, `sourceFramework`, `targetFramework` | `upgrade-path-analysis-report.json` |
| 2 | [framework-upgrade.skill.md](./framework-upgrade.skill.md) | Apply framework version changes, update web.config, convert project files (Classic→Modern), generate Program.cs skeleton, create WebForms stubs | `upgrade-path-analysis-report.json`, project files | `framework-upgrade-report.json`, modified project files |
| 3 | [nuget-package-upgrade.skill.md](./nuget-package-upgrade.skill.md) | Resolve and apply compatible NuGet package versions, remediate CVE-flagged packages, migrate packages.config → PackageReference (Classic→Modern) | `upgrade-path-analysis-report.json`, `packages.config` / `PackageReference` items | `nuget-upgrade-report.json`, updated packages |
| 4 | [build-validation.skill.md](./build-validation.skill.md) | Restore packages and build; parse and fix errors iteratively; achieve zero-error gate | All modified project files, `upgrade-path-analysis-report.json` | `build-validation-report.json`, `build-output.log` |
| 5 | [upgrade-report-generator.skill.md](./upgrade-report-generator.skill.md) | Produce self-contained HTML upgrade report from all skill JSON outputs | All four `*-report.json` files | `upgrade-report-<YYYY-MM-DD>.html` |

> **Skill 5 always runs last**, even if a preceding skill blocked or was skipped. Missing
> sections are marked "Skipped / Not Applied" in the report.

---

## Execution Sequence

```
[START]
  │
  ▼
1. upgrade-path-analysis       ← READ-ONLY; produces pathType
  │  pathType = ClassicToClassic | ClassicToModern | SdkToSdk
  ▼
2. framework-upgrade           ← reads pathType; selects Mode A / B
  │  Mode A = Classic→Classic  (bump TargetFrameworkVersion, web.config httpRuntime)
  │  Mode B = Classic→Modern   (SDK project conversion, Program.cs, stubs, appsettings.json)
  ▼
3. nuget-package-upgrade       ← reads pathType; selects Mode A / B / C
  │  Mode A = packages.config upgrade + HintPath update + nuget.exe restore
  │  Mode B = packages.config → PackageReference migration + dotnet restore
  │  Mode C = PackageReference version bump + dotnet restore  (SDK→SDK)
  ▼
4. build-validation            ← reads pathType; selects Mode A (MSBuild) / Mode B (dotnet CLI)
  │  Iterates up to 5 rounds to fix compiler errors
  │  GATE: zero-error state required to continue
  ▼
5. upgrade-report-generator    ← ALWAYS runs; collects all JSON, writes HTML report
  │
  ▼
[END]
```

### Blocking Issue Policy

If any skill encounters a blocking issue (unresolvable build error, missing CVE-safe package
version, COM dependency that cannot run on target framework, etc.):
1. Log the issue with file path, error code, and recommended resolution in the skill's JSON output.
2. **Do not silently skip** the issue or proceed with workarounds.
3. The `upgrade-report-generator` skill will surface all blocking issues in the "Skipped / Blocked
   Items" section of the HTML report.
4. Cap iterative build fix attempts at **5 rounds** — surface remaining errors and halt.

---

## Output Contract

| Output | Path | Description |
|--------|------|-------------|
| HTML Upgrade Report | `<reportOutputFolder>\upgrade-report-<YYYY-MM-DD>.html` | Self-contained styled HTML report |
| Build Log | `<solutionFolder>\build-output.log` | Raw build tool output |
| Upgrade Path Analysis | `<solutionFolder>\upgrade-reports\upgrade-path-analysis-report.json` | Risk matrix, project inventory, upgrade plan |
| Framework Changes Log | `<solutionFolder>\upgrade-reports\framework-upgrade-report.json` | Per-file framework version changes |
| NuGet Upgrade Log | `<solutionFolder>\upgrade-reports\nuget-upgrade-report.json` | Per-package version changes and CVE flags |
| Build Validation Log | `<solutionFolder>\upgrade-reports\build-validation-report.json` | Error counts, warnings, build status |

---

## Constraints Summary

| Rule | Applies To |
|------|-----------|
| Never use `dotnet build` / `dotnet restore` for Classic targets | Skills 3, 4 |
| Never use `msbuild` / `nuget.exe` for Modern / SDK targets | Skills 3, 4 |
| Never run Classic builds on Linux | Skill 4 (Mode A) |
| Never set `linuxFxVersion` or `reserved: true` on Windows App Service plans | All |
| Never hardcode secrets in any generated or modified file | All |
| Never modify `*.designer.vb`, `*.designer.cs`, auto-generated files | All |
| Never delete WebForms pages — create stubs instead | Skill 2 (Mode B) |
| Never upgrade EntityFramework beyond 6.x for Classic targets | Skill 3 (Mode A) |
| Never suppress compiler warnings with `#Disable Warning` | Skill 4 |
| Always add `upgrade-reports/` to `.gitignore` | Skill 5 |

---

## Standards

This skill is loaded by the [dotnet-framework-upgrade agent](./../../agents/dotnet-framework-upgrade.agent.md). All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/) and [Defra SDS — C# coding standards](https://defra.github.io/software-development-standards/standards/csharp_coding_standards/).
