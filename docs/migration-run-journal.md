# Histopathology System — Migration Agent Run Journal

**Project:** Histopathology System — VB.NET ASP.NET WebForms → C# .NET 10 + Razor Pages  
**Started:** 2026-07-27  
**Assessment ref:** [migration-assessment-net10.md](../.github/docs/migration-assessment-net10.md)

---

## How to Use This Journal

1. **Before each agent run** — note the current time, create a new row in [Run Log](#run-log) with `In Progress` status, and record the **Start Time** in [Session Metrics](#session-metrics).
2. **After each agent run** — note the finish time, update Run Log status, fill in **End Time** and **Duration** in Session Metrics, and link to the agent's output report.
3. **Any issue that is not auto-fixed** — add a row to [Open Issues](#open-issues).
4. **Any architectural or config decision made** — add a row to [Decision Log](#decision-log).

> **Duration tip:** Use the PowerShell one-liner in [Session Metrics](#session-metrics) — run it before starting the agent, then re-run after it finishes to print elapsed minutes automatically.

---

## Phase Tracker

| Phase | Description | Agents | Status | Started | Completed |
|-------|-------------|--------|--------|---------|-----------|
| 0 | Foundation & Safety Net | `testing`, `documentation`, `intelligent-migration`, `requirements-to-scrum-board`, `azure-infra-analyser` | In Progress | 2026-07-27 | — |
| 1 | Authentication Migration | `identity-migration` | **In Progress** | 2026-07-27 | — |
| 2 | Reporting Migration | `pdf-report-modernizer`, `pdf-reference-generator`, `pdf-validation` | Not Started | — | — |
| 3 | Platform Migration (VB→C#/.NET 10) | `dotnet-code-refactor`, `modernise-to-modular-monolith` | In Progress | 2026-07-27 | — |
| 4 | **Domain + Repository Modules** | `implementation` | **Complete** | 2026-07-29 | 2026-07-29 |
| 5 | UI Migration (Phase 5) | `ui-implementation` | **In Progress** | 2026-07-30 | — |
| 6 | Testing & Cutover | `dotnet-test-automation-and-quality-agent`, `playwright-tester-agent`, `testing` | Not Started | — | — |

---

## Run Log

> One row per agent invocation. Add rows chronologically.

| # | Date | Phase | Agent | Scope / Input | Status | Issues Found | Fixes Applied | Output / Linked Report |
|---|------|-------|-------|---------------|--------|--------------|---------------|------------------------|
| 1 | _(yyyy-MM-dd)_ | 0 | `pdf-reference-generator` | Baseline Crystal Reports PDFs before any code changes | Not Started | — | — | — |
| 2 | _(yyyy-MM-dd)_ | 0 | `dotnet-code-refactor` | Pre-conversion cleanup across all 185 VB files | Not Started | — | — | — |
| 3 | 2025-07-27 | 0 | `testing` | Baseline xUnit tests for `Histo.Core` domain layer — 8 testable logic areas discovered in `clsAnimal`, `clsBatch`, `clsBlock`, `Common.vb` | Done | ISS-008 resolved; test coverage limited to pure-logic layer (no DB until Phase 1; no auth until Phase 2) | Created `src/Histo.Core/`, `tests/Histo.Tests/`, `.github/workflows/ci.yml` — see outputs | [docs/Test-Strategy.md](/docs/Test-Strategy.md), [tests/Histo.Tests/](/tests/Histo.Tests/), [.github/workflows/ci.yml](/.github/workflows/ci.yml) |
| 4 | 2026-07-27 | 0 | `documentation` | HLD, LLD, 4 ADRs, Runbook — full codebase discovery across 3 projects, 185 VB files | Done | Plaintext SQL credential in Web.config (see ISS-006); no automated tests exist; Crystal Reports GAC-only dependency confirmed | — (documentation only — no code changes) | [docs/HLD.md](/docs/HLD.md), [docs/LLD.md](/docs/LLD.md), [docs/ADR/](/docs/ADR/), [docs/Runbook.md](/docs/Runbook.md) |
| 5 | 2026-07-27 | 0 | `intelligent-migration` | Programme governance — Intelligent-Migration-Plan, Intelligent-Team-Model, Risk-and-Governance, ROI-and-Budget; anchored to Chaos Report failure patterns; R-001–R-014 risk register; £498K indicative cost model with 42% AI uplift assumption | Done | R-001 (ISS-009 severity confirmed High/High); R-010 key-person risk raised; R-011 Azure admin dependency as critical path blocker | — (documentation only — no code changes) | [docs/Intelligent-Migration-Plan.md](/docs/Intelligent-Migration-Plan.md), [docs/Intelligent-Team-Model.md](/docs/Intelligent-Team-Model.md), [docs/Risk-and-Governance.md](/docs/Risk-and-Governance.md), [docs/ROI-and-Budget.md](/docs/ROI-and-Budget.md) |
| 6 | _(yyyy-MM-dd)_ | 0 | `requirements-to-scrum-board` | Convert assessment → Azure DevOps / Jira backlog CSV | Not Started | — | — | — |
| 7 | _(yyyy-MM-dd)_ | 0 | `azure-infra-analyser` | Config analysis → `infra-analysis.json` | Not Started | — | — | — |
| 8 | 2026-07-27 | 1 | `implementation` | Phase 1 — Solution Scaffold + Infrastructure Module: `HistopathologySystem.slnx`, `src/Histo.Infrastructure/` (IDbConnectionFactory, SqlConnectionFactory, IAppLogger, AppLogger, AppOptions), `src/Histo.Web/` stub (Program.cs, health endpoint, _Layout.cshtml, appsettings.json), 6 domain module stubs (Submissions, Histology, QC, Reporting, AuditLog, Administration), 8 new tests (SqlConnectionFactoryTests, AppOptionsTests) | Done | No issues — all 78 tests pass; `Histo.Web` and `Histo.Infrastructure` build green | Created all `src/` project stubs under existing `src/` folder per user instruction | [src/Histo.Infrastructure/](/src/Histo.Infrastructure/), [src/Histo.Web/](/src/Histo.Web/), [HistopathologySystem.slnx](/HistopathologySystem.slnx) |
| 25 | 2026-07-29 | 4 | `implementation` | **Phase 4 — Domain and Repository Modules (all 5 sub-phases):** Sub-phase 4.1 `Histo.Administration` (User model, IUserRepository/LookupRepository + Dapper impls, UserService/LookupService); Sub-phase 4.2 `Histo.AuditLog` (AuditLogEntry, IAuditLogRepository + multi-SP fan-out impl, AuditLogService); Sub-phase 4.3 `Histo.QualityControl` (QCNote + rowstamp concurrency, IQCNoteRepository + return-value concurrency detection, QCNoteService with QCNoteConcurrencyException propagation); Sub-phase 4.4 `Histo.Histology` (Block + BlockStatus, HistologyRef, IBlockRepository/IHistologyRepository + Dapper impls, BlockService/HistologyRefService with BlockHelpers delegation); Sub-phase 4.5 `Histo.Submissions` (Batch, Animal, BatchSubmission, Tissue + TissueOwner, DomainExceptions, IBatchRepository/ISubmissionRepository + Dapper impls, BatchService/SubmissionService with PG-number auto-reversal via AnimalHelpers); 10 new unit tests added (SubmissionServiceAnimalTests, QCNoteServiceTests, DomainExceptionTests); Moq added to test project | **Done** | No issues — build succeeded 0 warnings/errors; 88 tests pass (up from 78), 0 failures, 1 skipped (integration) | All 5 module `.csproj` files upgraded with Dapper + Microsoft.Data.SqlClient; test project wired to all 5 new modules | [src/Histo.Administration/](/src/Histo.Administration/), [src/Histo.AuditLog/](/src/Histo.AuditLog/), [src/Histo.QualityControl/](/src/Histo.QualityControl/), [src/Histo.Histology/](/src/Histo.Histology/), [src/Histo.Submissions/](/src/Histo.Submissions/) |
| 26 | 2026-07-30 | 5 | `ui-implementation` | **Phase 5 — UI Migration (WebForms → Razor Pages), Session 2 (resumed):** Completed Batch 3 CRUD pages (EditQCNote, BatchDetails, EditBatch, ReceiveBatch, BatchesForArchiving), Batch 4 complex workflow pages (AddSubmission, ViewSamples, BlockDetails, ArchiveBlocks, ArchiveTissues, BookHistologyRef, BookBlockRef), Batch 5 admin pages (UserMaintenance, PickListMaintenance, Error/Shared); wired all domain service + repository DI registrations and `AddSession`/`UseSession` in `Program.cs`; fixed 26 build errors (nullable session IDs, BatchStatus namespace, HistologyRef.Ref property name, Activity import, non-record Batch `with` expression) | **Done** | 26 build errors fixed (all resolved); build succeeded 0 warnings/errors; 88 tests pass, 0 failures | `Program.cs` updated with full DI wiring; all 5 Phase 4 project references active; `app.UseSession()` added to middleware pipeline | [src/Histo.Web/Pages/](/src/Histo.Web/Pages/), [src/Histo.Web/Program.cs](/src/Histo.Web/Program.cs) | All 9 Crystal Reports (.rpt) → Razor HTML + Dapper wiring (3-stage ReportDefinition.json pipeline): Stage 1 parse, Stage 2 templates, Stage 3 wire-up | Not Started | — | — | — |
| 10 | _(yyyy-MM-dd)_ | 2 | `pdf-validation` | RMSE pixel diff + structural checks for all 9 reports vs Phase 0 reference PDFs | Not Started | — | — | — |
| 15 | 2026-07-27 | 3 | `modernise-to-modular-monolith` | Target Architecture, Migration Plan, ADR-005 — Dapper/SP decision gate (Option A confirmed by user) | Done | GetUserByNTLogin SP called with Entra UPN may need NT login format mapping (see ISS-009) | — (planning only — no code changes) | [docs/Target-Architecture.md](/docs/Target-Architecture.md), [docs/Migration-Plan.md](/docs/Migration-Plan.md), [docs/ADR/ADR-005-data-access-dapper-stored-procedures.md](/docs/ADR/ADR-005-data-access-dapper-stored-procedures.md) |
| 16 | _(yyyy-MM-dd)_ | 4 | `ui-implementation` | ASCX controls → `_Layout.cshtml` + Partial Views | Not Started | — | — | — |
| 17 | _(yyyy-MM-dd)_ | 4 | `ui-implementation` | 18 simple display pages → Razor Pages | Not Started | — | — | — |
| 18 | _(yyyy-MM-dd)_ | 4 | `ui-implementation` | 28 CRUD pages → Razor Pages | Not Started | — | — | — |
| 19 | _(yyyy-MM-dd)_ | 4 | `ui-implementation` | 12 complex multi-step workflow pages → Razor Pages | Not Started | — | — | — |
| 20 | _(yyyy-MM-dd)_ | 5 | `azure-infra-planner` | Interview → `infra-plan.json` | Not Started | — | — | — |
| 21 | _(yyyy-MM-dd)_ | 5 | `azure-infra-implementer` | Generate all Bicep modules + CI/CD pipeline | Not Started | — | — | — |
| 22 | _(yyyy-MM-dd)_ | 5 | `devops-pipeline-modernizer` | CI/CD YAML, branching strategy | Not Started | — | — | — |
| 23 | _(yyyy-MM-dd)_ | 6 | `dotnet-test-automation-and-quality-agent` | C# xUnit domain tests + integration tests | Not Started | — | — | — |
| 24 | _(yyyy-MM-dd)_ | 6 | `playwright-tester-agent` | E2E Playwright tests for critical user journeys | Not Started | — | — | — |
| 27 | 2026-07-30 | 5 | `ui-implementation` | **Hot-fix — `IAppLogger` not registered in DI container:** All 8 domain services (`UserService`, `LookupService`, `AuditLogService`, `BlockService`, `HistologyRefService`, `QCNoteService`, `BatchService`, `SubmissionService`) threw `AggregateException` on startup because `IAppLogger` was missing from the DI container. Added `AddTransient<IAppLogger>` factory registration in `Program.cs` using `ILoggerFactory` to create an `AppLogger<IAppLogger>` backed by the Serilog pipeline. | **Done** | ISS-012: `IAppLogger` missing from DI — caused `System.AggregateException` on `Histo.Web` startup; all 8 domain services unresolvable | Added `IAppLogger` transient factory registration in `Program.cs` | [src/Histo.Web/Program.cs](../src/Histo.Web/Program.cs) |

---

## Session Metrics

> Record start and end time for every agent run. Duration is calculated automatically by the script below.

| # | Date | Agent | Start Time | End Time | Duration (mins) | Notes |
|---|------|-------|------------|----------|-----------------|-------|
| 1 | _(yyyy-MM-dd)_ | _(agent name)_ | _(HH:mm)_ | _(HH:mm)_ | — | — |
| 2 | 2026-07-27 | `documentation` | 10:56 | 11:39 | **~43** | File timestamps: HLD.md created 10:56, all docs last-modified 11:39 |
| 3 | 2026-07-27 | `modernise-to-modular-monolith` | 12:05 | 12:14 | **~9** | File timestamps: Target-Architecture.md created 12:05, modified 12:14 |
| 4 | 2026-07-27 | `testing` | 12:29 | 12:44 | **~15** | File timestamps: Test-Strategy.md created 12:29, modified 12:44 |
| 5 | 2026-07-27 | `intelligent-migration` | 12:49 | 12:55 | **~6** | File timestamps: Intelligent-Migration-Plan.md 12:49, ROI-and-Budget.md last-modified 12:55 |
| 6 | 2026-07-27 | `implementation` | 14:35 | 14:38 | **~10** | File timestamps: slnx + Infrastructure created 14:35–14:36, test files created 14:37–14:38; build+test run follows |
| 8 | 2026-07-30 | `ui-implementation` | 15:08 | 15:25 | **~17** | Resumed from Session 1 (prior conversation). Completed all remaining pages and DI wiring. Build: 0 warnings/0 errors. Tests: 88 pass, 0 fail. |
| 9 | 2026-07-30 | `ui-implementation` | 15:40 | 15:43 | **~3** | Hot-fix: `IAppLogger` not registered in DI. Added `AddTransient<IAppLogger>` factory. Build: 0 warnings/0 errors. Tests: 88 pass, 0 fail. |

### Duration Timer Script

Run this **before** starting the agent to capture the start time. When the agent finishes, run it again — it prints the elapsed duration in minutes:

```powershell
# Step 1 — Run BEFORE starting the agent (saves start time to $agentStart)
$agentStart = Get-Date
Write-Host "Timer started at: $($agentStart.ToString('HH:mm:ss'))"

# Step 2 — Run AFTER the agent finishes (calculates elapsed time)
$agentEnd = Get-Date
$elapsed  = [math]::Round(($agentEnd - $agentStart).TotalMinutes, 1)
Write-Host "Agent finished at : $($agentEnd.ToString('HH:mm:ss'))"
Write-Host "Elapsed duration  : $elapsed minutes"
```

> **Tip:** Keep a PowerShell terminal open for the duration of each agent run. `$agentStart` persists in the session as long as the terminal is not closed or the variable is not overwritten.

---

## Open Issues

> Issues found by agents that were NOT auto-fixed — require human action.

| ID | Date | Phase | Agent | File / Area | Issue | Severity | Owner | Status | Resolution |
|----|------|-------|-------|-------------|-------|----------|-------|--------|------------|
| ISS-001 | _(pending)_ | 1 | `identity-migration` | `Program.cs` | No skill covers `AddMicrosoftIdentityWebApp` wiring for .NET 10 — must be implemented manually | High | _(dev name)_ | Open | — |
| ISS-002 | _(pending)_ | 2 | `pdf-report-converter` | `HistologyReport.rpt` | Sub-report nesting (`HistologySubReport.rpt`) requires manual ViewModel design — agent handles template only | Medium | _(dev name)_ | Open | — |
| ISS-003 | _(pending)_ | 3 | any | `HistopathologySystem.vbproj` | `On Error Resume Next` scattered across 185 VB files — `dotnet-code-refactor` must run before conversion | Medium | _(dev name)_ | Open | — |
| ISS-004 | _(pending)_ | 4 | `ui-implementation` | All ASPX pages | No strangler-fig path on .NET 10 — all 64 pages must be ready before cutover (hard switch required) | High | _(dev name)_ | Open | — |
| ISS-005 | _(pending)_ | any | n/a | `.github/agents/modernisation.agent .md` | Filename has a space before `.md` — VS Code cannot load it as an agent | Low | _(dev name)_ | Open | Rename to `modernisation.agent.md` |
| ISS-006 | 2026-07-27 | 0 | `documentation` | `HistopathologySystem/Web.config` | Plaintext SQL credential in `DBConnectionString` (`User Id=HistologyUser;Password=HistologyUser9245`) — must not be committed and must be replaced with Managed Identity before Azure deployment | High | _(dev name)_ | Open | Replace with Managed Identity + Key Vault reference (per azure-infra.instructions.md Section 3) |
| ISS-007 | 2026-07-27 | 0 | `documentation` | `HistopathologySystem/Web.config` | `compilation debug="true"` — debug compilation must be disabled for production | Medium | _(dev name)_ | Open | Set to `false` in production `Web.Release.config` XDT transform |
| ISS-008 | 2025-07-27 | 0 | `documentation` | `HistopathologySystem/` | No automated tests exist — zero unit/integration test coverage | High | _(dev name)_ | **Resolved** | Phase 0 `testing.agent` run created `src/Histo.Core/` + `tests/Histo.Tests/` + CI workflow. 25 unit tests covering 8 business-logic areas. Integration test scaffold in place (skipped until Phase 1). |
| ISS-009 | 2026-07-27 | 3 | `modernise-to-modular-monolith` | `Histo.Administration::UserService` | `GetUserByNTLogin` SP expects `DOMAIN\username` format; Entra ID UPN is `user@domain.com` — mapping required | High | _(dev name)_ | Open | Phase 2 task: one-time data migration or SP parameter normalisation — confirm with DB team before Phase 2 begins |
| ISS-010 | 2026-07-27 | 0 | `intelligent-migration` | Programme | Key-person risk: Sr Dev 1 holds sole institutional knowledge of VB.NET business rules — no redundancy | High | Delivery Lead | Open | Ensure all business rules are in `docs/LLD.md` + named unit tests before Sr Dev 1 begins conversion; pair-review policy enforced |
| ISS-011 | 2026-07-27 | 0 | `intelligent-migration` | Programme — Phase 2 entry criterion | Azure admin must create Entra ID app registration and confirm group IDs before Phase 2 can begin — external dependency on critical path | High | Delivery Lead | Open | Pre-schedule Azure admin engagement during Phase 1; track as critical path item; escalate to Executive Sponsor if blocked |
| ISS-012 | 2026-07-30 | 5 | `ui-implementation` | `src/Histo.Web/Program.cs` | `IAppLogger` not registered in DI container — all 8 domain services unresolvable on startup (`System.AggregateException`) | High | — | **Resolved** | Added `AddTransient<IAppLogger>` factory using `ILoggerFactory` in `Program.cs`. Build: 0 errors. Tests: 88 pass. |

---

## Issue Detail

> Short structured record for each ISS-* item: symptom, root cause, and fix. Add one entry per issue as they are raised or resolved.

---

### ISS-006 — Plaintext SQL credential in Web.config

| Field | Detail |
|-------|--------|
| **Status** | Open |
| **Phase** | 0 |
| **Symptom** | `DBConnectionString` in `HistopathologySystem/Web.config` contains `User Id=HistologyUser;Password=HistologyUser9245` in plaintext, committed to source control. |
| **Root cause** | Legacy on-premises pattern — SQL Auth credentials were stored directly in config. No secrets management was in place. |
| **Fix** | Replace with Managed Identity connection string (`Authentication=Active Directory Default`) before Azure deployment. Store the current credential as a Key Vault secret during transition. Apply `web.Release.config` XDT transform to remove the key in Release builds. See `azure-infra.instructions.md` Section 3. |
| **Owner** | Dev team + Azure admin |

---

### ISS-007 — Debug compilation enabled in Web.config

| Field | Detail |
|-------|--------|
| **Status** | Open |
| **Phase** | 0 |
| **Symptom** | `<compilation debug="true">` in `Web.config` — would be deployed to production as-is. |
| **Root cause** | Development setting left in source; no XDT transform in place to override it for Release. |
| **Fix** | Add `<compilation xdt:Transform="SetAttributes" debug="false" />` to `Web.Release.config`. |
| **Owner** | Dev team |

---

### ISS-008 — No automated test coverage (Resolved)

| Field | Detail |
|-------|--------|
| **Status** | Resolved |
| **Phase** | 0 |
| **Symptom** | Zero unit or integration tests existed across all 185 VB files. No CI pipeline. |
| **Root cause** | Legacy codebase predated automated testing practices; no test framework was ever introduced. |
| **Fix** | Phase 0 `testing` agent created `src/Histo.Core/`, `tests/Histo.Tests/` (88 unit tests), and `.github/workflows/ci.yml`. |
| **Owner** | Resolved |

---

### ISS-009 — NT login format mismatch for Entra ID migration

| Field | Detail |
|-------|--------|
| **Status** | Open |
| **Phase** | 3 |
| **Symptom** | `GetUserByNTLogin` stored procedure expects `DOMAIN\username` format. Entra ID provides `user@domain.com` (UPN). The `SessionService.Populate()` method has a `// TODO Phase 2` comment marking this gap. |
| **Root cause** | Legacy Windows Auth stored NT login names in the `DOMAIN\username` format in the Users table. Entra ID tokens carry UPN format instead. |
| **Fix** | Phase 2 task. Options: (a) one-time data migration to convert NT login values to UPN in the Users table, or (b) add a normalisation step in `UserService.ResolveUserAsync` that strips the domain prefix and appends the tenant domain. Confirm the approach with the DB team and customer before Phase 2 begins. |
| **Owner** | Dev team + DB team |

---

### ISS-010 — Key-person risk: sole VB.NET knowledge holder

| Field | Detail |
|-------|--------|
| **Status** | Open |
| **Phase** | Programme |
| **Symptom** | Sr Dev 1 holds sole institutional knowledge of VB.NET business rules with no redundancy. |
| **Root cause** | Long-tenure single developer; no knowledge transfer or pair-review policy enforced. |
| **Fix** | Ensure all business rules are documented in `docs/LLD.md` and expressed as named unit tests before Sr Dev 1 begins conversion work. Enforce pair-review policy for all Phase 3+ PRs. |
| **Owner** | Delivery Lead |

---

### ISS-011 — Azure admin external dependency blocks Phase 2

| Field | Detail |
|-------|--------|
| **Status** | Open |
| **Phase** | Programme |
| **Symptom** | Entra ID app registration and group IDs are required before Phase 2 (Auth Migration) can begin. This is an external dependency outside the dev team's control. |
| **Root cause** | Azure admin engagement was not pre-scheduled as part of Phase 0 planning. |
| **Fix** | Pre-schedule Azure admin engagement during Phase 1. Escalate to Executive Sponsor if not confirmed within two weeks of Phase 1 completion. Track as critical path item in programme risk register. |
| **Owner** | Delivery Lead |

---

### ISS-012 — `IAppLogger` not registered in DI container (Resolved)

| Field | Detail |
|-------|--------|
| **Status** | Resolved |
| **Phase** | 5 |
| **Symptom** | `System.AggregateException` thrown on `Histo.Web` startup. All 8 domain services (`UserService`, `LookupService`, `AuditLogService`, `BlockService`, `HistologyRefService`, `QCNoteService`, `BatchService`, `SubmissionService`) failed DI validation with: _"Unable to resolve service for type `Histo.Infrastructure.IAppLogger`"_. |
| **Root cause** | `IAppLogger` is a custom infrastructure interface (`Histo.Infrastructure.IAppLogger`) implemented by `AppLogger<T>`. When the domain service DI registrations were added to `Program.cs` during Phase 5, the `IAppLogger` registration was omitted. `Microsoft.Extensions.Logging.ILogger<T>` is registered automatically by the host, but `IAppLogger` is not — it is a project-level abstraction that must be explicitly wired. |
| **Fix** | Added the following registration to `Program.cs` (after `AddSingleton<IDbConnectionFactory>`): `builder.Services.AddTransient<IAppLogger>(sp => { var factory = sp.GetRequiredService<ILoggerFactory>(); return new AppLogger<IAppLogger>(factory.CreateLogger<IAppLogger>()); });` This creates an `AppLogger<IAppLogger>` backed by the Serilog pipeline for every injection site. Build: 0 errors. Tests: 88 pass. |
| **Owner** | Resolved — 2026-07-30 |

---

## Automated Fixes Applied

> Fixes that agents applied without human intervention — for audit and rollback reference.

| # | Date | Phase | Agent | File | Issue | Fix Applied | Commit / Branch |
|---|------|-------|-------|------|-------|-------------|-----------------|
| _(populated during runs)_ | | | | | | | |

---

## Decision Log

> Architectural, configuration, or tooling decisions made during the migration — with rationale and source.

| # | Date | Phase | Agent / Source | Decision | Rationale | Impact |
|---|------|-------|----------------|----------|-----------|--------|
| D-001 | 2026-07-27 | 3 | Assessment | Use **Dapper** for data access migration (not EF Core) | Retains all stored procedures; zero database change risk; eliminates DataSet from application layer with minimal effort | Phase 3 scope |
| D-002 | 2026-07-27 | 4 | Assessment | Replace `UpdatePanel` with **HTMX** (default) | Lowest friction replacement for ASP.NET AJAX on .NET 10; no client-side JS framework required | All ASPX pages using UpdatePanel |
| D-003 | 2026-07-27 | 4 | Assessment | Replace `VLAHeader.ascx` identity resolution with **`_Layout.cshtml` + `IHttpContextAccessor`** | Centralises identity context for all Razor Pages; eliminates `GetUserDetails()` call | Phase 4 layout scaffold |
| D-004 | 2026-07-27 | 1 | Assessment | Use **ASP.NET Core policies** (`[Authorize(Policy = "...")]`) to replace 60+ `CheckPermissions()` methods | Centralised, testable, declarative — policies defined once in `Program.cs` | Phase 1 auth replacement |
| D-005 | 2026-07-27 | 6 | Assessment | **Blue-green deployment** via App Service deployment slots for cutover | No runtime coexistence on .NET 10; slots provide instant rollback without DNS change | Phase 6 cutover plan |

---

## Manual Review Items

> Components that failed agent automated processing or were blocked — cross-referenced with [manual-review-list.md](dotnet-upgrade/manual-review-list.md).

| # | Date | Phase | Agent | Component / File | Reason | Last Error | Next Action | Owner |
|---|------|-------|-------|------------------|--------|------------|-------------|-------|
| _(populated when agents flag items)_ | | | | | | | | |

---

## Per-Phase Summary (Populated After Each Phase Completes)

### Phase 0 — Foundation & Safety Net
- **Status:** Not Started
- **Est. effort (with agents):** 7d
- **Token budget (estimate):** ~250k tokens
- **Actual tokens used:** —
- **Completion gate:** Baseline tests committed; HLD/LLD approved; `infra-analysis.json` generated; reference PDFs committed

### Phase 1 — Authentication Migration
- **Status:** Not Started
- **Est. effort (with agents):** 12d
- **Token budget (estimate):** ~180k tokens
- **Actual tokens used:** —
- **Completion gate:** All 60+ `CheckPermissions()` replaced; sign-in/sign-out round-trip validated; session expiry triggers re-auth; auth equivalence test passes
- **Known gap:** `AddMicrosoftIdentityWebApp` `.NET 10` wiring is manual (ISS-001)

### Phase 2 — Reporting Migration
- **Status:** Not Started
- **Est. effort (with agents):** 9d
- **Token budget (estimate):** ~350k tokens
- **Actual tokens used:** —
- **Completion gate:** All 9 reports pass RMSE validation; Crystal Reports DLL references removed; `report-inventory.json` committed

### Phase 3 — Platform Migration (VB→C#/.NET 10)
- **Status:** Not Started
- **Est. effort (with agents):** 15d
- **Token budget (estimate):** ~500k tokens
- **Actual tokens used:** —
- **Completion gate:** Solution builds on .NET 10; Dapper POCOs replace DataSet in DAL; App Insights wiring live; xUnit tests pass

### Phase 4 — UI Migration (WebForms → Razor Pages)
- **Status:** Not Started
- **Est. effort (with agents):** 118.5d
- **Token budget (estimate):** ~1.2M tokens (largest phase)
- **Actual tokens used:** —
- **Completion gate:** All 64 ASPX pages converted; all 8 ASCX controls replaced; `SessionVars` typed service in place; smoke test passes on all pages

### Phase 5 — Infrastructure & DevOps
- **Status:** Not Started
- **Est. effort (with agents):** 5.5d
- **Token budget (estimate):** ~120k tokens
- **Actual tokens used:** —
- **Completion gate:** Bicep modules deployed to dev; CI/CD pipeline passes all environments; Key Vault secrets populated; Managed Identity SQL grant executed

### Phase 6 — Testing & Cutover
- **Status:** Not Started
- **Est. effort (with agents):** 13.5d
- **Token budget (estimate):** ~200k tokens
- **Actual tokens used:** —
- **Completion gate:** xUnit + integration + E2E tests pass; auth equivalence validated; blue-green slot swap executed; post-cutover smoke test passes

---

## Cross-References

| Artefact | Path |
|----------|------|
| Migration assessment | [docs/migration-assessment-net10.md](migration-assessment-net10.md) |
| Upgrade notes (agent decisions log) | [docs/dotnet-upgrade/upgrade-notes.md](dotnet-upgrade/upgrade-notes.md) |
| Package replacements | [docs/dotnet-upgrade/package-replacements.md](dotnet-upgrade/package-replacements.md) |
| Manual review list | [docs/dotnet-upgrade/manual-review-list.md](dotnet-upgrade/manual-review-list.md) |
| Per-run batch reports | `docs/dotnet-upgrade/upgrade-reports/{yyyyMMdd}.md` |
| Agent config | [.github/config/upgrade-agent.md](../config/upgrade-agent.md) |
| Agent inventory | `.github/agents/` |
