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
| 5 | UI Migration (Phase 5) | `ui-implementation` | **Substantially Complete** (57/57 non-reporting-blocked pages; 3 pages blocked on Phase 2) | 2026-07-30 | — |
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
| 26 | 2026-07-30 | 5 | `ui-implementation` | **Phase 5 — UI Migration (WebForms → Razor Pages), Session 2 (resumed):** Completed Batch 3 CRUD pages (EditQCNote, BatchDetails, EditBatch, ReceiveBatch, BatchesForArchiving), Batch 4 complex workflow pages (AddSubmission, ViewSamples, BlockDetails, ArchiveBlocks, ArchiveTissues, BookHistologyRef, BookBlockRef), Batch 5 admin pages (UserMaintenance, PickListMaintenance, Error/Shared); wired all domain service + repository DI registrations and `AddSession`/`UseSession` in `Program.cs`; fixed 26 build errors (nullable session IDs, BatchStatus namespace, HistologyRef.Ref property name, Activity import, non-record Batch `with` expression) | **Done** | 26 build errors fixed (all resolved); build succeeded 0 warnings/errors; 88 tests pass, 0 failures | `Program.cs` updated with full DI wiring; all 5 Phase 4 project references active; `app.UseSession()` added to middleware pipeline | [src/Histo.Web/Pages/](/src/Histo.Web/Pages/), [src/Histo.Web/Program.cs](/src/Histo.Web/Program.cs) |
| 42 | 2026-08-01 | 5 | `ui-implementation` | **Batch F — ExcelExport (cross-cutting CSV export) and ViewImportedData; FinalPrintBatch confirmed blocked on Phase 2:** Investigation corrected the parity audit's assumption — neither `ExcelExport.aspx` nor `ViewImportedData.aspx` depends on Crystal Reports. `ExcelExport.aspx` is a generic "render current results as Excel" utility invoked from 4 already-migrated pages (`AuditLogByDate`, `AuditLogBySubmission`, `AuditLogByUser`, `SearchArchiveLocation`). Added a new `CsvExportHelper` (`src/Histo.Web/CsvExportHelper.cs`, plain CSV — no new NuGet dependency, faithful equivalent to "opens in Excel") and an `OnPostExportCsvAsync`/"Export to CSV" govuk-button on all 4 pages, reusing each page's already-held in-memory result set (no requery). Built `Search/ViewImportedData.cshtml(.cs)` ("View Old ICC_Sub data" table selector + filtered results + its own CSV export), added 1 new small `LookupService` unit test. Wired a new nav link from `Index.cshtml`. Confirmed `FinalPrintBatch.aspx` genuinely IS blocked on Phase 2 — its only two actions (`btnPrintBatch`, `btnSubmissionNotes`) launch `SubmissionForm.aspx`/`SubmissionNotes.aspx` popups, both pure Crystal Reports PDF generators (already correctly identified in Batch C, Run #36) with no interactive content of their own; building a shell page whose only two buttons are non-functional was judged not to add real value and was intentionally deferred to Phase 2. | **Done** | ISS-019 — 2 more of the 34 originally-missing pages resolved (with corrected classification); `FinalPrintBatch` reclassified from "missing UI page" to "blocked on Phase 2 reporting", consistent with `SubmissionForm`/`SubmissionNotes` | `src/Histo.Web/CsvExportHelper.cs`, CSV export wiring on 4 pages, `Search/ViewImportedData.cshtml(.cs)` | [src/Histo.Web/CsvExportHelper.cs](../src/Histo.Web/CsvExportHelper.cs), [src/Histo.Web/Pages/Search/ViewImportedData.cshtml](../src/Histo.Web/Pages/Search/ViewImportedData.cshtml) |
| 41 | 2026-08-01 | 5 | `ui-implementation` | **Batch E3 — QualityData and FixCompletedDates:** Built `QC/QualityData.cshtml(.cs)` (batch-wide QC/dispatch worklist across histology/antibody/special-stain block tests, deliberately simplified to edit one test at a time via new `QC/EditQualityDataTest.cshtml(.cs)` rather than the legacy multi-select batch-save) and `Admin/FixCompletedDates.cshtml(.cs)` (admin utility recomputing `CompletedDate` for cassetted batches with fully-dispatched tests, via new `BatchService.FixCompletedDatesAsync`, ported to per-batch independent error handling instead of the legacy single-transaction approach). **Hot-fix applied by calling agent:** the generated `BatchService.FixCompletedDatesAsync`/`TryGetLatestDispatchDate` helper had two overloads differing only by `ref`/`out` parameter modifier (CS0663, does not compile in C#) — fixed by removing the `out` overload and initializing the accumulator inline before the first `ref` call. Verified `dotnet build` (0 errors) and `dotnet test` (88 pass, 1 skipped) after the fix. | **Done** | ISS-019 — 2 more of the 34 originally-missing pages resolved; CS0663 build break introduced by the subagent run was caught and fixed before commit | `QC/QualityData.cshtml(.cs)`, `QC/EditQualityDataTest.cshtml(.cs)`, `Admin/FixCompletedDates.cshtml(.cs)`, `Histo.Submissions/Services/BatchService.cs` (build fix) | [src/Histo.Web/Pages/QC/QualityData.cshtml](../src/Histo.Web/Pages/QC/QualityData.cshtml), [src/Histo.Web/Pages/Admin/FixCompletedDates.cshtml](../src/Histo.Web/Pages/Admin/FixCompletedDates.cshtml) |
| 40 | 2026-08-01 | 5 | `ui-implementation` | **Batch E2 — BatchBlocks, BatchSummary, BatchBlockSummary determined to be already covered:** Verified all 3 pages are functionally superseded by pages built in earlier runs: `BatchBlocks.aspx` by `Blocks/BlockDetails.cshtml` (already documented in its own doc comment); `BatchSummary.aspx`/`BatchBlockSummary.aspx` by `Submissions/ViewSamples.cshtml` + `Submissions/SubmissionDetails.cshtml`/`SubmissionDetailsBlock.cshtml` (Batch C). Found and closed a real gap while verifying this: `SubmissionDetails.cshtml` was orphaned (no page linked to it) and `SubmissionService.DeleteAnimalAsync` was unused — added `OnPostEditAsync`/`OnPostDeleteAsync` handlers plus Tissues/Delete buttons to `Submissions/ViewSamples.cshtml(.cs)`, reusing existing repository/service methods (no new backend code). Flagged that `docs/Parity-Audit-Report.md`'s page-status table is now stale following Batches A–D and should be treated as a point-in-time snapshot, not a live source of truth. | **Done** | ISS-019 — 3 more of the 34 originally-missing pages confirmed as false positives (already covered); 1 navigation gap closed (ViewSamples Edit/Delete wiring) | `Submissions/ViewSamples.cshtml(.cs)` | [src/Histo.Web/Pages/Submissions/ViewSamples.cshtml](../src/Histo.Web/Pages/Submissions/ViewSamples.cshtml) |
| 39 | 2026-08-01 | 5 | `ui-implementation` | **Batch E1 — PickListMaintenanceID and PickListUserArea (resolves ISS-018):** Built `Admin/EditLookupItem.cshtml(.cs)` (unrestricted per-table Add/Edit editor, replacing `PickListMaintenanceID.aspx`, linked from `Admin/PickListMaintenance.cshtml`'s new per-row Edit links) and `Admin/PickListUserArea.cshtml(.cs)` (area-scoped single-table editor for Projects/Contacts, replacing `PickListUserArea.aspx` — confirmed as a genuinely distinct page from `PickListMaintenanceID`, not a sub-flow of it; its legacy entry point is `BatchDetails.aspx`'s New Project/Contact buttons, which the migrated `Batches/BatchDetails.cshtml` doesn't have yet, so it's reachable by direct route for now). Added `CreateLookupItemAsync`/`UpdateLookupItemAsync` to `ILookupRepository`/`LookupRepository`/`LookupService`, refactoring `ResolveSelectProcAsync` into a shared `ResolveLookupProcsAsync` that resolves Select/Update/Insert/Delete SP names together via `GetEditableLookupProcs`; Insert uses Dapper `DynamicParameters` since the Area parameter is conditional per-table. Deliberate simplification: uniform Description/IsActive/UserID(+Area) parameter shape used for all tables rather than guessing at unverifiable legacy per-table SP signatures (no `.sql` source available in-repo to confirm exact Code-keyed vs ID-keyed parameter names). | **Done** | ISS-018 resolved | `Admin/EditLookupItem.cshtml(.cs)`, `Admin/PickListUserArea.cshtml(.cs)`, `ILookupRepository`/`LookupRepository`/`LookupService` Create/Update methods | [src/Histo.Web/Pages/Admin/EditLookupItem.cshtml](../src/Histo.Web/Pages/Admin/EditLookupItem.cshtml), [src/Histo.Web/Pages/Admin/PickListUserArea.cshtml](../src/Histo.Web/Pages/Admin/PickListUserArea.cshtml) |
| 38 | 2026-08-01 | 5 | `ui-implementation` | **Batch D2 — Copy workflow family (7 legacy pages, all resolved):** `CopyBatch.aspx`/`CopyBatchBlocks.aspx` consolidated into `Batches/CopyBatch.cshtml(.cs)` + `Batches/CopyBatchSummary.cshtml(.cs)` (submission-level copy: batch header, submissions, animals, tissues via existing `BatchService`/`SubmissionService`). `CopyBlocks.aspx` → `Blocks/CopyBlocks.cshtml(.cs)` (block+tissue copy across samples in the same batch). `CopySamples.aspx`/`CopySamplesBlocks.aspx`/`CopySamplesSummary.aspx` consolidated into `Blocks/CopySamples.cshtml(.cs)` + `Blocks/CopySamplesSummary.cshtml(.cs)` (copies blocks/tissues from a sample in a *different* submission onto an existing sample in the current batch — corrected understanding: this does not create new animal rows via `CopyAnimalAsync` as initially hypothesised; it reuses the same `BlockService.CopyBlockAsync`/`SubmissionService.CopyTissueAsync` methods already used by `CopyBlocksModel`). No new repository/service code was needed for any of the 7 pages — all copy operations were already supported by Phase 4 methods. Wired dead-link entry points on `BlockDetails.cshtml` (Copy samples) and `BatchDetails.cshtml` (Copy batch, from prior sub-run). Documented simplifications: TSE/Non-TSE batch-type matching not reproduced (no batch-type field in migrated model); auto-generate-histology-ref option on block copy not reproduced (kept target's existing ref); legacy `CopySamplesSummary.aspx`'s independent read-only grid feature not reproduced as a separate capability (superseded by `SubmissionDetailsBlock`/`BlockDetails`). | **Done** | ISS-019 resolved for these 7 pages | `Batches/CopyBatch.cshtml(.cs)`, `Batches/CopyBatchSummary.cshtml(.cs)`, `Blocks/CopyBlocks.cshtml(.cs)`, `Blocks/CopySamples.cshtml(.cs)`, `Blocks/CopySamplesSummary.cshtml(.cs)` | [src/Histo.Web/Pages/Batches/CopyBatch.cshtml](../src/Histo.Web/Pages/Batches/CopyBatch.cshtml), [src/Histo.Web/Pages/Blocks/CopyBlocks.cshtml](../src/Histo.Web/Pages/Blocks/CopyBlocks.cshtml), [src/Histo.Web/Pages/Blocks/CopySamples.cshtml](../src/Histo.Web/Pages/Blocks/CopySamples.cshtml) |
| 37 | 2026-08-01 | 5 | `ui-implementation` | **Batch D1 — AddSample and Cassetted pages, with corrected functional understanding:** Parity audit's initial descriptions of these two pages were wrong and corrected during this run. `AddSample.aspx` is not a separate sample-creation workflow — it's the landing page for adding an *existing* animal (found via `Search/SearchSample`) to the current batch, mirroring the already-migrated `Submissions/AddSubmission.cshtml`. Built `Submissions/AddSample.cshtml(.cs)` accordingly, using existing `SubmissionService.AddAnimalAsync` (no new repository code). `Cassetted.aspx` is not a block-status transition — it's the "Submission Type" step shown after clicking "New submission" on Home, which creates the batch header and routes to Batch Details. Built `Batches/Cassetted.cshtml(.cs)` using existing `BatchService.AddAsync`/`LookupService.GetLookupDataAsync` (no new repository code). Wired two previously-dead links: `Search/SearchSample.cshtml` now has an "Add to batch" action, and `Index.cshtml`'s "New submission" link now correctly points to `/Batches/Cassetted` instead of skipping to `/Submissions/AddSubmission`. | **Done** | ISS-019 partial (2 more of the 34 missing pages resolved, with corrected classification vs. the original audit description) | `Submissions/AddSample.cshtml(.cs)`, `Batches/Cassetted.cshtml(.cs)`, dead-link fixes in `Search/SearchSample.cshtml` and `Index.cshtml` | [src/Histo.Web/Pages/Submissions/AddSample.cshtml](../src/Histo.Web/Pages/Submissions/AddSample.cshtml), [src/Histo.Web/Pages/Batches/Cassetted.cshtml](../src/Histo.Web/Pages/Batches/Cassetted.cshtml) |
| 36 | 2026-08-01 | 5 | `ui-implementation` | **Batch C — Submission detail pages, plus SubmissionForm/SubmissionNotes disposition determined:** Built `Submissions/SubmissionDetails.cshtml(.cs)` (sender ref, editable PM date/histology ref, per-animal tissues add/delete) and `Submissions/SubmissionDetailsBlock.cshtml(.cs)` (per-animal blocks list/delete), both using existing `ISubmissionRepository`/`SubmissionService` and `IBlockRepository`/`BlockService` methods — no new repository code needed, confirming Phase 4 repository layer is comprehensive for this module. **Determined `SubmissionForm.aspx` and `SubmissionNotes.aspx` are NOT missing UI pages** — both are pure Crystal Reports PDF-export popups (invoked from `FinalPrintBatch.aspx`/`ViewSubmissions.aspx`) with no interactive markup; they belong to the Phase 2 reporting migration, not Phase 5 UI implementation, and were correctly excluded from this batch's scope. | **Done** | ISS-019 partial (2 of the 34 missing pages resolved; 2 more reclassified as reporting-phase work, not UI-phase work) | `Submissions/SubmissionDetails.cshtml(.cs)`, `Submissions/SubmissionDetailsBlock.cshtml(.cs)` | [src/Histo.Web/Pages/Submissions/](../src/Histo.Web/Pages/Submissions/) |
| 35 | 2026-08-01 | 5 | `ui-implementation` | **ISS-020 resolved — Search module, remaining 4 of 8 pages:** Built `SearchSender`, `SearchSubmissions`, `SearchTest`, `SearchUnUsedHistologyRefs` Razor Pages under `src/Histo.Web/Pages/Search/`, completing the Search module (all 8 pages now exist). Wired to existing `SubmissionService.GetAnimalsBySenderRefAsync`, `BatchService.SearchAsync`/`GetTestItemRowsAsync`, `LookupService.GetLookupDataAsync`, `UserService.GetAllUsersAsync`; added one missing service passthrough (`HistologyRefService.GetAllUnusedRefsAsync`, repository method already existed); added the missing "Search Test Totals" link to `SearchMenu.cshtml` (menu only listed 7 of 8). `SearchTest` deliberately follows the existing SIMPLIFIED scope decision on `IBatchRepository.GetTestItemRowsAsync` (test-item counts only, not the full legacy premium cross-tab analytics engine). `SearchSender` reimplemented as a standalone search (legacy page was a session-state picker for the not-yet-migrated `AddSample.aspx` workflow), consistent with how `SearchSample` handled the same situation. | **Done** | ISS-020 fully resolved | 8 new Razor Pages total across this and Run #27; 1 new service passthrough; 1 menu link fix | [src/Histo.Web/Pages/Search/](../src/Histo.Web/Pages/Search/) |
| 27 | 2026-07-31 | 5 | `ui-implementation` | **ISS-020 (partial) — Search module, first 4 of 8 pages:** Built `SearchPMDates`, `SearchBlockRefs`, `SearchArchiveLocation`, `SearchSample` Razor Pages under `src/Histo.Web/Pages/Search/`, wired to existing `SubmissionService`/`BlockService` methods (`GetByPmDateRangeAsync`, `GetAnimalsBySenderRefAsync`, `GetTissueArchiveAsync`) and `Histo.Core.Domain.BlockRefRangeHelpers`; added 4 missing read-only passthrough methods to `BlockService` (`GetUsedBlockRefsByHistologyRefAsync`, `GetUsedBlockRefsBySenderRefAsync`, `GetBlockArchiveAsync`, `GetSlideArchiveAsync`) — the Dapper repository methods already existed, only the service-layer passthrough was missing | **Done** | No repository/SP gaps found — all data access pre-existed; `SearchArchiveLocation` legacy logic (hierarchical expand/collapse grids across 3 modes) was significantly more complex than the reference pattern, reproduced here as flat GOV.UK tables per already-documented `SIMPLIFIED` model notes; `SearchSample` legacy page was a session-dependent picker (populated by `AddSubmission.aspx` lookup, redirects to non-existent `AddSample` page) — reimplemented as a standalone read-only Sender Ref search | No new repository/SP methods required; only `BlockService` passthroughs added | Build succeeded 0 errors; 88 tests pass, 1 skipped, 0 failures | [src/Histo.Web/Pages/Search/](/src/Histo.Web/Pages/Search/), [src/Histo.Histology/Services/BlockService.cs](/src/Histo.Histology/Services/BlockService.cs) | All 9 Crystal Reports (.rpt) → Razor HTML + Dapper wiring (3-stage ReportDefinition.json pipeline): Stage 1 parse, Stage 2 templates, Stage 3 wire-up | Not Started | — | — | — |
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
| 28 | 2026-07-30 | 5 | `ui-implementation` | **Hot-fix — Home page shows no modules (all panels hidden):** `GroupName` was always empty string on every request because `SessionService.Populate()` only set `UserName`, `PopulateFromUser()` didn’t exist, and `getUserDetails()` equivalent was never called. Added `PopulateFromUser(User)` to `ISessionService`/`SessionService`; added `OnPageHandlerExecutionAsync` override to `HistoPageModel` that calls `UserService.ResolveUserAsync` when `GroupName` is uninitialised, with a dev-mode stub. | **Done** | ISS-013: all Home page panels hidden; `IsCustomer`/`IsHistoUser`/`IsMaintenance` always `false` | Added `PopulateFromUser`, `OnPageHandlerExecutionAsync` with DB resolve + dev stub | [src/Histo.Web/Pages/HistoPageModel.cs](../src/Histo.Web/Pages/HistoPageModel.cs), [src/Histo.Web/Services/SessionService.cs](../src/Histo.Web/Services/SessionService.cs) |
| 29 | 2026-07-30 | 5 | `ui-implementation` | **Investigation — Header, footer, and page styling not fully migrated:** User reported Home page modules display correctly but header/footer/CSS do not match the legacy application. Investigation found: (1) Bootstrap 5 utility classes used across all migrated CRUD pages but never referenced in `_Layout.cshtml`; (2) `wwwroot/css/Styles.css` is an explicit placeholder stub, never migrated from the actual production theme `HistopathologySystem/Style/vla-ie.css`; (3) no `wwwroot/images/` folder — legacy logo (`vlalogoExtended.gif`) never copied; (4) footer/header markup does not replicate `VLAHeader.ascx`/`VLAFooter.ascx` structure (bordered blue box, Top anchor, Copyright/Department lines). Root cause: `ui-implementation` agent migrated markup and C# logic correctly but never migrated the CSS asset pipeline. Recommended fix documented in ISS-014; not yet applied — pending confirmation. | **Investigated** | ISS-014: header/footer/styling gap — Bootstrap missing, `Styles.css` stub not replaced, no logo asset | — (analysis only, fix pending) | [docs/migration-run-journal.md](migration-run-journal.md) |
| 30 | 2026-07-30 | 5 | `ui-implementation` | **Hot-fix — Applied ISS-014 Bootstrap + VLA theme migration:** Added Bootstrap 5.3.3 CDN `<link>` (with SRI integrity hash) and bundle `<script>` to `_Layout.cshtml`; added logo `<img>` reference (`~/images/vlalogoExtended.gif`) inside `.site-title`; replaced footer markup with `#top` anchor link, Copyright line, and Department line matching `VLAFooter.ascx`; added `<a name="top"></a>` after `<body>` for the footer link target. Replaced `wwwroot/css/Styles.css` placeholder stub with the full VLA theme (blue `#003399` / accent `#33ccff` palette, `.site-header`/`.site-nav`/`.nav-links`, `.site-context`/`.ctx-batch`, `.page-title-bar`, `.home-panels`/`.panel`, `.site-footer`/`.footer-top-link`, `.site-logo`, `table-dark` override, `.text-error`/`.text-warning-legacy`) ported from `HistopathologySystem/Style/vla-ie.css`. Created `wwwroot/images/` and copied `HistopathologySystem/Images/vlalogoExtended.gif` into it. | **Done** | ISS-014: header/footer/styling gap — Bootstrap missing, `Styles.css` stub not replaced, no logo asset | Bootstrap CDN wired, VLA theme CSS migrated, logo asset copied, header/footer markup replaced | [src/Histo.Web/Pages/Shared/_Layout.cshtml](../src/Histo.Web/Pages/Shared/_Layout.cshtml), [src/Histo.Web/wwwroot/css/Styles.css](../src/Histo.Web/wwwroot/css/Styles.css) |
| 31 | 2026-07-31 | 5 | `gds-ui` | **GOV.UK Design System compliance pass — superseded Bootstrap/VLA theme (ISS-014) with real govuk-frontend v6.2.0:** Downloaded and vendored govuk-frontend CSS/JS, GDS Transport fonts (4 files), and all required favicon/crown/icon-mask assets into `wwwroot/govuk/` and `wwwroot/assets/` (unpkg was blocked by the corporate Zscaler proxy; jsDelivr CDN used instead — confirmed working, matches CSS `url()` root-relative asset paths). Rewrote `_Layout.cshtml` with the govuk-frontend v6 page template: skip link, GOV.UK header with inline Tudor Crown SVG logo, `govuk-service-navigation` component (service name + role-based nav), `govuk-main-wrapper`, and full `govuk-footer` (crown SVG, OGL licence SVG, Crown copyright link, Support links heading) — fetched exact reference markup from `alphagov/govuk-frontend` GitHub raw (v6.2.0 tag) rather than reconstructing from memory. Converted `_NavPartial.cshtml` to emit `govuk-service-navigation__item` list items and `_UserContextPartial.cshtml` to plain text (no bespoke CSS). Removed the Bootstrap 5.3.3 CDN entirely. Rewrote `Index.cshtml` home panels using `govuk-grid-row`/`govuk-grid-column-one-third` + `govuk-list` (replacing bespoke `.home-panels`/`.panel`). Converted 15 pages with Bootstrap-dependent markup (tables, buttons, forms, alerts) to real `govuk-table`, `govuk-button`, `govuk-form-group`/`govuk-input`/`govuk-textarea`/`govuk-select`/`govuk-checkboxes`, `govuk-error-summary`, `govuk-warning-text`, and `govuk-summary-list` markup so no page regresses once Bootstrap CSS was removed. Trimmed `Styles.css` down to only the two non-GDS helper classes (`.data-table`, `.filter-form`) still used by pages not yet converted (see ISS-015). | **Done** | ISS-015 raised: 10 pages (audit log search/list pages, `BatchesForDispatch`, `BatchesForEditing`, `BatchesNotReceived`, `BatchesReceived`, `SubmissionsOnHold`, `ViewSubmissions`, `QCNotes`) still use the legacy `.data-table`/`.filter-form`/unstyled `<button>` markup rather than real `govuk-table`/`govuk-fieldset` — not a regression (no Bootstrap dependency), but not yet full GDS component compliance | Vendored govuk-frontend v6.2.0 assets; rewrote layout, nav partial, user-context partial, home page, and 15 CRUD/detail pages to real GDS markup; build succeeded 0 warnings/errors; 88 tests pass, 1 skipped, 0 fail | [src/Histo.Web/Pages/Shared/_Layout.cshtml](../src/Histo.Web/Pages/Shared/_Layout.cshtml), [src/Histo.Web/Pages/Shared/_NavPartial.cshtml](../src/Histo.Web/Pages/Shared/_NavPartial.cshtml), [src/Histo.Web/Pages/Index.cshtml](../src/Histo.Web/Pages/Index.cshtml), [src/Histo.Web/wwwroot/css/Styles.css](../src/Histo.Web/wwwroot/css/Styles.css), [src/Histo.Web/wwwroot/govuk/](../src/Histo.Web/wwwroot/govuk/) |
| 32 | 2026-07-31 | 5 | `gds-ui` | **Hot-fix — Runtime crash on launch: `InvalidOperationException` on service navigation link (regression from Run #31):** User launched the application after the GDS compliance pass and hit `System.InvalidOperationException: 'Cannot override the 'href' attribute for <a>. An <a> with a specified 'href' must not have attributes starting with 'asp-route-' or an 'asp-action', 'asp-controller', 'asp-area', 'asp-route', 'asp-protocol', 'asp-host', 'asp-fragment', 'asp-page' or 'asp-page-handler' attribute.'` on every page load. Root cause: the service-name link in `_Layout.cshtml` was written with **both** a literal `href="/"` **and** `asp-page="/Index"` on the same `<a>` tag — the ASP.NET Core Anchor Tag Helper throws at render time whenever a literal `href` is combined with any `asp-*` navigation attribute, because it cannot determine which one should win. Removed the redundant literal `href="/"`, keeping only `asp-page="/Index"` (which generates the equivalent `href="/"` via the tag helper). Verified with `dotnet build` (0 errors), `dotnet test` (88 pass, 1 skipped), and a live `dotnet run` smoke test confirming the home page returns HTTP 200 and the link renders as `<a class="govuk-service-navigation__link" href="/">Histopathology System</a>` with no exception. | **Done** | ISS-016: `<a>` tag combined literal `href` with `asp-page` tag helper attribute, causing `InvalidOperationException` on every page render | Removed the literal `href="/"` from the service navigation link in `_Layout.cshtml`, keeping only `asp-page="/Index"`. Confirmed no other `.cshtml` file in the project combines a literal `href`/`asp-route-*` with any `asp-page`/`asp-action`/`asp-controller`/`asp-area`/`asp-route`/`asp-protocol`/`asp-host`/`asp-fragment` attribute (regex-searched entire `Pages/` tree). | [src/Histo.Web/Pages/Shared/_Layout.cshtml](../src/Histo.Web/Pages/Shared/_Layout.cshtml) |
| 34 | 2026-08-01 | 5 | `implementation` | **Full legacy-vs-current parity audit (read-only, no code changes):** User requested a full-scope comparison of every module, screen, feature, workflow, business rule, and CRUD operation between the legacy WebForms app and the current Razor/GDS app. Enumerated all 64 legacy `.aspx` pages, 8 `.ascx` controls, 9 Crystal Reports, and all current Razor Pages across 10 subfolders; performed 1:1 name/functional mapping; inspected all 5 domain module repository interfaces (`Histo.Administration`, `Histo.Submissions`, `Histo.Histology`, `Histo.QualityControl`, `Histo.AuditLog`) for Create/Update/Delete completeness; grepped legacy `CheckPermissions()` usage and current `Histo.Web` auth wiring. Findings: only 30/64 pages (47%) migrated; Reporting at 0%; domain/repository CRUD layer is materially ahead of the UI layer (most entities already have Create/Update/Delete methods with no consuming page); authentication/authorization is 0% implemented in code (only a dev-only stub identity exists, no `[Authorize]`/Entra ID wiring anywhere). Produced [docs/Parity-Audit-Report.md](Parity-Audit-Report.md) with a full page-by-page mapping table, CRUD parity table, and 10 prioritized findings (F-01–F-10). | **Done** | New findings F-01–F-10 (see report); confirms and quantifies existing ISS-001, ISS-004, ISS-006, ISS-007, ISS-009, ISS-018 rather than duplicating them | — (documentation/audit only — no code changes) | [docs/Parity-Audit-Report.md](Parity-Audit-Report.md) |
| 33 | 2026-07-31 | 5 | `gds-ui` | **Comprehensive UI/CRUD review — resolved ISS-015 and restored missing User Management CRUD (ISS-017):** User reported GDS-styled buttons/dropdowns still not visible and asked for a full component/CRUD audit. Re-verified the govuk-frontend v6.2.0 asset pipeline end-to-end (file integrity, live HTTP 200 + correct MIME types for the CSS/JS bundles, `_Layout.cshtml` header/footer/crown rendering) — confirmed the asset pipeline was never the fault. Root-caused the visible non-GDS styling to the 10 ISS-015 pages, which were still using `.data-table`/`.filter-form`/bare button/input/select markup — converted all 10 to real govuk-table/govuk-form-group/govuk-input/govuk-select/govuk-button markup, then removed the now-unused helper rules from `Styles.css`. Separately investigated the user's report that User Management CRUD is missing versus the legacy app: confirmed `Admin/UserMaintenance.cshtml`(.cs) was read-only (Get-only) and that the repository/service layer had no Create/Update methods at all — a full-stack gap. Restored it: added Create/Update methods to the User repository/service (mapping to the legacy AddUser/EditUser stored procedures) and Group/Area lookup methods to the Lookup repository/service; built new Add User and Edit User GDS-compliant Razor Pages; added an Add user button, per-row Change links, and a success banner to the User Maintenance list page. Live-verified against the real dev database — the Group/Area drop-downs render actual live data. Also identified an equivalent CRUD gap in PickListMaintenance versus the legacy PickListMaintenanceID.aspx (logged as ISS-018, not implemented — out of session scope). | **Done** | ISS-015 resolved; ISS-017 (User Management CRUD missing) found and resolved; ISS-018 raised (PickListMaintenance CRUD gap, same pattern, not yet implemented) | Converted 10 pages to real GDS markup; removed dead CSS; added Create/Update user methods and Group/Area lookup methods across repository/service layers; added Admin/AddUser and Admin/EditUser pages; updated Admin/UserMaintenance with Add/Change links and status banner. Build: 0 errors. Tests: 88 pass, 1 skipped, 0 fail. Live smoke test: all re-verified pages return HTTP 200; Admin/AddUser renders live Group/Area lookup data from the dev database. | [src/Histo.Administration/Repositories/UserRepository.cs](../src/Histo.Administration/Repositories/UserRepository.cs), [src/Histo.Administration/Repositories/LookupRepository.cs](../src/Histo.Administration/Repositories/LookupRepository.cs), [src/Histo.Web/Pages/Admin/AddUser.cshtml](../src/Histo.Web/Pages/Admin/AddUser.cshtml), [src/Histo.Web/Pages/Admin/EditUser.cshtml](../src/Histo.Web/Pages/Admin/EditUser.cshtml), [src/Histo.Web/Pages/Admin/UserMaintenance.cshtml](../src/Histo.Web/Pages/Admin/UserMaintenance.cshtml) |

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
| 10 | 2026-07-30 | `ui-implementation` | 17:44 | 17:49 | **~5** | Hot-fix: Home page showing no modules (ISS-013). Added `PopulateFromUser`, `OnPageHandlerExecutionAsync` DB resolve + dev stub. Build: 0 warnings/0 errors. Tests: 88 pass, 0 fail. Timing confirmed from file timestamps: `SessionService.cs` 17:48:16, `HistoPageModel.cs` 17:48:40. |
| 11 | 2026-07-30 | `ui-implementation` | 17:50 | 18:05 | **~15** | Investigation: header/footer/styling not migrated (ISS-014). Analysis only — Bootstrap missing, `Styles.css` stub, no logo asset. Fix recommended, not yet applied. |
| 12 | 2026-07-30 | `ui-implementation` | 18:10 | 18:16 | **~6** | Applied ISS-014 fix: Bootstrap 5.3.3 CDN wired into `_Layout.cshtml`, VLA theme CSS migrated into `Styles.css`, logo asset copied to `wwwroot/images/`, header/footer markup replaced. Build: 0 warnings/0 errors. Tests: 88 pass, 1 skipped, 0 fail. Timing confirmed from file timestamps: `_Layout.cshtml` 18:14:58, `Styles.css` 18:15:21. |
| 13 | 2026-07-31 | `gds-ui` | 14:41 | 14:53 | **~12** | GDS compliance pass: vendored govuk-frontend v6.2.0 assets (CSS, JS, 4 fonts, 7 icon/crown images) via jsDelivr (unpkg blocked by Zscaler proxy); rewrote `_Layout.cshtml`, `_NavPartial.cshtml`, `_UserContextPartial.cshtml`, `Index.cshtml`, and 15 CRUD/detail pages to real GOV.UK Design System markup; removed Bootstrap CDN entirely. Build: 0 warnings/0 errors. Tests: 88 pass, 1 skipped, 0 fail. Timing confirmed from file timestamps: `govuk-frontend.min.css` created 14:41:09, `ReceiveBatch.cshtml` (last page converted) modified 14:50:57, build+test verification completed 14:53. |
| 14 | 2026-07-31 | `gds-ui` | 15:09 | 15:13 | **~4** | Hot-fix (ISS-016): removed literal `href="/"` co-existing with `asp-page="/Index"` on the service navigation link in `_Layout.cshtml`, which threw `InvalidOperationException` on every page render. Build: 0 warnings/0 errors. Tests: 88 pass, 1 skipped, 0 fail. Live `dotnet run` smoke test confirmed home page returns HTTP 200 with correctly rendered `href="/"` link and no exception. Timing confirmed by terminal timestamp `Get-Date` at 15:13:42 (fix completion). |
| 15 | 2026-07-31 | `gds-ui` | 15:32 | 16:00 | **~28** | Comprehensive UI/CRUD review: re-verified GDS asset pipeline end-to-end (file integrity + live HTTP 200/MIME checks + rendered header/footer/crown); resolved ISS-015 by converting the remaining 10 non-GDS pages to real `govuk-table`/`govuk-form-group`/`govuk-select`/`govuk-button` markup; found and resolved ISS-017 (User Management CRUD fully missing — no Create/Update anywhere in the stack) by adding repository/service methods and two new GDS Add/Edit User pages; raised ISS-018 (PickListMaintenance has the same CRUD gap, not yet implemented). Build: 0 warnings/0 errors. Tests: 88 pass, 1 skipped, 0 fail. Live smoke test confirmed all pages return HTTP 200 and the new Add User page renders live Group/Area lookup data from the dev database. |
| 16 | 2026-08-01 | `implementation` | — | — | — | Full parity audit (read-only, no timer captured for this documentation-only session) — enumerated legacy vs. current pages, domain CRUD interfaces, and auth wiring; produced `docs/Parity-Audit-Report.md`. — (paste from VS Code Output) |
| 17 | 2026-07-31 | `ui-implementation` | — | — | — (paste from VS Code Output) | ISS-020 partial fix — Search module first 4 of 8 pages (SearchPMDates, SearchBlockRefs, SearchArchiveLocation, SearchSample). Build 0 errors; 88 tests pass, 1 skipped. |
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
| ISS-013 | 2026-07-30 | 5 | `ui-implementation` | `src/Histo.Web/Pages/HistoPageModel.cs`, `SessionService.cs` | Home page shows no modules — all panels hidden because `GroupName` is always empty; `Populate()` was never called and `PopulateFromUser()` did not exist | High | — | **Resolved** | Added `PopulateFromUser(User)` to `ISessionService`/`SessionService`; added `OnPageHandlerExecutionAsync` to `HistoPageModel` to call `UserService.ResolveUserAsync` on every request when session is uninitialised; added dev-mode stub identity. Build: 0 errors. Tests: 88 pass. |
| ISS-014 | 2026-07-30 | 5 | `ui-implementation` | `src/Histo.Web/Pages/Shared/_Layout.cshtml`, `wwwroot/css/Styles.css` | Header, footer, and page styling not migrated — Bootstrap 5 classes used throughout CRUD pages (`table-striped`, `btn-primary`, `form-control`, `alert-danger`) but Bootstrap was never referenced; `Styles.css` is a placeholder stub, never migrated from legacy `Style/vla-ie.css`; no logo/images folder migrated | High | — | **Resolved** | Added Bootstrap 5.3.3 CDN link + bundle script to `_Layout.cshtml`; replaced `Styles.css` with VLA theme ported from `Style/vla-ie.css`; copied `Images/vlalogoExtended.gif` to `wwwroot/images/` and referenced it in the header; replaced footer markup with Top anchor + Copyright + Department lines. Build: 0 errors. Tests: 88 pass. **Superseded 2026-07-31 by ISS-015** — the Bootstrap/VLA theme approach was itself replaced with the real GOV.UK Design System. |
| ISS-015 | 2026-07-31 | 5 | `gds-ui` | `src/Histo.Web/Pages/AuditLog/*.cshtml`, `Batches/BatchesForDispatch.cshtml`, `Batches/BatchesForEditing.cshtml`, `Batches/BatchesNotReceived.cshtml`, `Batches/BatchesReceived.cshtml`, `Batches/SubmissionsOnHold.cshtml`, `Submissions/ViewSubmissions.cshtml`, `QC/QCNotes.cshtml` | 10 pages used the legacy `.data-table` / `.filter-form` / unstyled `<button>` markup instead of real `govuk-table` / `govuk-fieldset` / `govuk-button` components — the most likely cause of the user's follow-up report that GDS buttons/drop-downs were still not visible. | Medium | — | **Resolved** | All 10 pages converted to real `govuk-table`/`govuk-form-group`/`govuk-input`/`govuk-select`/`govuk-button` markup; `.data-table`/`.filter-form` rules removed from `Styles.css`. Verified with a full-tree regex scan for remaining Bootstrap/plain-HTML patterns — zero matches. Build: 0 errors. Tests: 88 pass, 1 skipped. |
| ISS-017 | 2026-07-31 | 5 | `gds-ui` | `src/Histo.Administration/Interfaces/IUserRepository.cs`, `Repositories/UserRepository.cs`, `Services/UserService.cs`, `src/Histo.Web/Pages/Admin/UserMaintenance.cshtml(.cs)` | User Management CRUD was missing entirely versus the legacy app — `Admin/UserMaintenance.cshtml.cs` had only an `OnGetAsync` handler, and `IUserRepository`/`UserService` had no Create or Update methods anywhere in the stack. Full-stack regression, not just a missing UI form. | High | — | **Resolved** | Added `CreateUserAsync`/`UpdateUserAsync` to the User repository/service (mapping to the legacy `AddUser`/`EditUser` stored procedures) and `GetUserGroupsAsync`/`GetUserAreasAsync` to the Lookup repository/service (mapping to `GetluUserGroup`/`GetluUserArea`); added new `Admin/AddUser.cshtml(.cs)` and `Admin/EditUser.cshtml(.cs)` GDS-compliant Razor Pages with validation; added an Add user button, per-row Change links, and a success banner to `Admin/UserMaintenance.cshtml`. Live-verified against the real dev database. |
| ISS-018 | 2026-07-31 | 5 | `gds-ui` | `src/Histo.Web/Pages/Admin/PickListMaintenance.cshtml(.cs)`, `src/Histo.Administration/Interfaces/ILookupRepository.cs` | Same CRUD-gap pattern as ISS-017, found while comparing legacy vs current but not yet implemented (out of session scope). Legacy `PickListMaintenanceID.aspx` supports inline Add/Edit of pick-list rows (Maintenance-group only); current `Admin/PickListMaintenance.cshtml.cs` is read-only and `ILookupRepository` has no Create/Update methods for pick-list rows. | Medium | — | **Resolved** | 2026-08-01: Added `CreateLookupItemAsync`/`UpdateLookupItemAsync` to `ILookupRepository`/`LookupRepository`/`LookupService` with dynamic per-table stored-procedure resolution; built `Admin/EditLookupItem.cshtml(.cs)` and `Admin/PickListUserArea.cshtml(.cs)`. Build 0 errors, tests 88 pass/1 skipped. Maintenance-group gating intentionally omitted, consistent with the rest of the app pending Phase 1/2 auth. |
| ISS-016 | 2026-07-31 | 5 | `gds-ui` | `src/Histo.Web/Pages/Shared/_Layout.cshtml` | Application crashed on every page load immediately after the GDS compliance pass with `System.InvalidOperationException: 'Cannot override the 'href' attribute for <a>...'`. Regression introduced by Run #31 (ISS-015 companion pass) — the service navigation link had both a literal `href="/"` and `asp-page="/Index"` on the same `<a>` tag. | High | — | **Resolved** | Removed the literal `href="/"`, keeping only `asp-page="/Index"`. Verified with `dotnet build` (0 errors), `dotnet test` (88 pass, 1 skipped), and a live `dotnet run` smoke test (home page returns HTTP 200, link renders correctly, no exception). Confirmed no other `.cshtml` file in the project has the same literal-`href` + `asp-*` conflict. |
| ISS-019 | 2026-08-01 | 5 | `implementation` | `src/Histo.Web/Pages/` (whole app) | Full parity audit found only 30 of 64 legacy ASPX pages (47%) have a current Razor Page equivalent — 34 pages missing, including the entire Sample-creation workflow (`AddSample`), the Batch/Block/Sample "Copy" workflow family (7 pages), Submission detail/notes pages, Excel export, and admin data-fix utilities. See [Parity-Audit-Report.md](Parity-Audit-Report.md) §2 for the full page-by-page table. | High | — | Open | Re-plan Phase 5 backlog against `docs/Migration-Plan.md`, prioritized by business criticality per Parity-Audit-Report.md §8. |
| ISS-020 | 2026-08-01 | 5 | `implementation` | `src/Histo.Web/Pages/Search/SearchMenu.cshtml` | `SearchMenu.cshtml` is live and links to 8 legacy search screens, none of which have been migrated — a functional dead end for any user navigating to Search. | High | — | **Resolved** | All 8 pages built: `SearchPMDates`, `SearchBlockRefs`, `SearchArchiveLocation`, `SearchSample` (Run #27), `SearchSender`, `SearchSubmissions`, `SearchTest`, `SearchUnUsedHistologyRefs` (Run #35). Build 0 errors; 88 tests pass, 1 skipped. |
| ISS-021 | 2026-08-01 | 5 | `implementation` | `src/Histo.QualityControl/Interfaces/IQCNoteRepository.cs`, `src/Histo.Histology/Interfaces/IHistologyRepository.cs` | Two small, self-contained CRUD gaps found where the repository/service method already exists but no Razor Page calls it: (1) `IQCNoteRepository.AddAsync` has no "Add QC Note" UI page (legacy `QCNoteForm.aspx`) — only editing existing notes is exposed; (2) `IHistologyRepository.UpdateRefAsync` has no `EditHistologyRef` UI page equivalent. | Medium | — | **Resolved** | Added `QC/AddQCNote.cshtml(.cs)` (mirrors legacy two-step `AddAsync`+`UpdateAsync` note-text flow found in `QualityData.aspx.vb`/`clsQCNote.vb`) and `Bookings/EditHistologyRef.cshtml(.cs)` (wired to the pool-level histology-ref counter semantics, matching the dead link already present in `BookingMenu.cshtml`). Added `HistologyRefService.UpdateRefAsync` service-layer passthrough. Build 0 errors, tests 88 pass/1 skipped. **New gap found during this fix**: the *actual* legacy `EditHistologyRef.aspx` is a per-animal Sender/Histology Ref renamer (`clsAnimal.UpdateAnimalSenderRef`/`UpdateAnimalHistologyRef`) with no current repository method at all — tracked as new issue below (ISS-022). |
| ISS-022 | 2026-08-01 | 5 | `implementation` | `src/Histo.Histology/`, `src/Histo.Submissions/` | Legacy `EditHistologyRef.aspx` is a per-animal Sender Ref / Histology Ref renamer (`clsAnimal.UpdateAnimalSenderRef`/`UpdateAnimalHistologyRef`, SPs `EditAnimalSenderRef`/`EditAnimalHistologyRef`) with PG-number reverse-format checks, next-ref upper-bound checks, and duplicate-ref rejection. No repository method for this workflow exists anywhere in the current codebase — it is a genuine backend gap discovered while resolving ISS-021, distinct from the pool-level counter update (`IHistologyRepository.UpdateRefAsync`) that was already implemented. | Medium | — | Open | Add `UpdateAnimalSenderRefAsync`/`UpdateAnimalHistologyRefAsync` to the appropriate repository (likely `ISubmissionRepository` or a new `IAnimalRepository`), porting the validation rules from `clsAnimal.vb`, then build a Razor Page for it. |

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

### ISS-013 — Home page shows no modules — all panels hidden (Resolved)

| Field | Detail |
|-------|--------|
| **Status** | Resolved |
| **Phase** | 5 |
| **Symptom** | After migration, the Home page (`/`) rendered with no module panels visible. All sections (Submissions, Laboratory, Search & Reports, Bookings & Archive, Administration) were absent. The nav partial also showed only the “View Submissions” link for all users. |
| **Root cause** | Four compounding gaps introduced during migration: (1) `SessionService.Populate()` only wrote `UserName` to session — it never wrote `GroupName`, `GroupID`, `UserID`, or any area fields. (2) `Populate()` was never called anywhere — not in layout, not in `HistoPageModel`, not in middleware. (3) No equivalent of `getUserDetails()` (the legacy `VLAHeader.ascx` DB lookup) was called on request entry, so `GroupName` remained an empty string on every request. (4) Because `IsCustomer`, `IsHistoUser`, and `IsMaintenance` all evaluate `GroupName`, all three returned `false`, hiding every `@if` panel block in `Index.cshtml`. |
| **Fix** | Three targeted changes: **(a)** Added `PopulateFromUser(User user)` to `ISessionService` and implemented it in `SessionService` — writes all seven identity fields (`UserName`, `GroupName`, `GroupID`, `UserID`, `UserEmail`, `UserArea`, `UserAreaID`) mirroring the exact Session writes in the legacy `getUserDetails()`. **(b)** Added `OnPageHandlerExecutionAsync` override to `HistoPageModel` — on each request, checks `GroupName`; if empty, calls `UserService.ResolveUserAsync(ntLogin)` and then `Session.PopulateFromUser(user)`. This mirrors the legacy early-exit pattern (`If sGroupName = ""`). **(c)** Added a Development-environment stub identity (`GroupName = "Maintenance"`) so all panels render during local testing without a live database or authenticated user. The stub is guarded by `IHostEnvironment.IsDevelopment()` and must never reach a non-dev environment. |
| **Owner** | Resolved — 2026-07-30 |

### ISS-014 — Header, footer, and page styling not fully migrated (Resolved)

| Field | Detail |
|-------|--------|
| **Status** | Resolved |
| **Phase** | 5 |
| **Symptom** | Home page modules render and function correctly, but the header, footer, and overall page styling do not match the legacy `.NET Framework 4.0` application. Pages appear mostly unstyled — tables, buttons, forms, and alerts show as raw HTML; the branded blue/white VLA header and footer bar are missing; no application logo appears. |
| **Root cause** | Four compounding gaps, all CSS/asset-pipeline related (no C# or Razor logic defects): <br>**(1)** Dozens of migrated pages (`BatchesForArchiving`, `UserMaintenance`, `PickListMaintenance`, `BlockDetails`, `BatchDetails`, `ArchiveBlocks`, etc.) use Bootstrap 5 utility classes (`table table-striped table-hover`, `table-dark`, `btn btn-primary`, `form-control`, `form-select`, `alert alert-danger`) but Bootstrap CSS/JS was **never referenced** in `_Layout.cshtml` or anywhere else in the project — every such page renders unstyled. <br>**(2)** `wwwroot/css/Styles.css` is an explicit placeholder stub (its own header comment reads *"Phase 5: Replace with full ... styles migrated from legacy HistopathologySystem/Styles.css"*) — and even that comment points at the wrong source file. The actual production theme referenced by all 64 legacy ASPX pages via `<LINK href="Style/vla-ie.css">` is `HistopathologySystem/Style/vla-ie.css`, which defines the VLA blue/white brand palette, `.topnavlinks`, `.topnavtext`, `.bottomnavlink`, `.AppTitle`, `.PageTitle`, `.GridHeader`, `.ErrorText` — none of which were migrated. <br>**(3)** No `wwwroot/images/` folder exists in `Histo.Web` — the legacy logo (`HistopathologySystem/Images/vlalogoExtended.gif`) and other icon assets were never copied over. <br>**(4)** `_Layout.cshtml` header/footer markup does not replicate the legacy `VLAHeader.ascx` / `VLAFooter.ascx` structure (bordered blue box, "Top" anchor, Copyright/Department lines, user context labels styled with `.topnavtext`). |
| **Fix** | **(a)** Added Bootstrap 5.3.3 CDN `<link>` (with SRI integrity hash) + bundle `<script>` to `_Layout.cshtml`. **(b)** Replaced `wwwroot/css/Styles.css` with a migrated theme porting the VLA palette (`#003399` blue / `#33ccff` accent), header/nav/footer/panel styles onto the existing semantic classes (`.site-header`, `.site-nav`, `.nav-links`, `.site-context`, `.ctx-batch`, `.site-footer`, `.home-panels`, `.panel`, `.site-logo`), plus a `table-dark` override and `.text-error`/`.text-warning-legacy` helpers. **(c)** Created `src/Histo.Web/wwwroot/images/` and copied `HistopathologySystem/Images/vlalogoExtended.gif` into it, referenced via a new `<img class="site-logo">` inside `.site-title`. **(d)** Updated footer markup in `_Layout.cshtml` to include a `#top` anchor link, Copyright line, and Department line matching `VLAFooter.ascx`, plus an `<a name="top">` anchor after `<body>` as the link target. Verified with `dotnet build` (0 errors) and `dotnet test` (88 pass, 1 skipped, 0 fail). |
| **Owner** | Resolved — 2026-07-30 |

**Superseded 2026-07-31:** the customer/product decision was to adopt the real GOV.UK Design System instead of a Bootstrap-based bespoke theme. See ISS-015 for the govuk-frontend migration and remaining follow-up work.

---

### ISS-015 — 10 pages still use legacy `.data-table` / `.filter-form` markup instead of real GDS components (Resolved)

| Field | Detail |
|-------|--------|
| **Status** | Resolved |
| **Phase** | 5 |
| **Symptom** | After the GOV.UK Design System compliance pass (see Run Log #31), 10 pages still rendered lists and search forms with the legacy `.data-table` / `.filter-form` CSS helper classes and plain unstyled `<button>`/`<input>`/`<select>` elements, rather than real `govuk-table`, `govuk-form-group`, `govuk-input`, `govuk-select`, and `govuk-button` markup: the three audit log pages (`AuditLogByDate`, `AuditLogBySubmission`, `AuditLogByUser`), `Batches/BatchesForDispatch`, `Batches/BatchesForEditing`, `Batches/BatchesNotReceived`, `Batches/BatchesReceived`, `Batches/SubmissionsOnHold`, `Submissions/ViewSubmissions`, and `QC/QCNotes`. This was raised again by the user in a follow-up review ("I still cannot see the GDS-styled buttons, dropdown controls, and other standard GDS components") — these 10 pages were the root cause, since they were consciously left out of the original GDS pass. |
| **Root cause** | These pages were built in Phase 5 using a distinct, simpler `.data-table`/`.filter-form` convention (not the Bootstrap classes used elsewhere), so they were **not** part of the Bootstrap-removal regression scope for the original GDS compliance pass — removing Bootstrap did not break them, since they never depended on it. They were consciously left out of that pass to keep it bounded; the two helper CSS rules were kept in `Styles.css` specifically so these pages kept rendering correctly until converted. This is exactly what the user encountered when navigating to Audit Log, Batches list, or View Submissions pages — plain unstyled buttons/tables/inputs with no GDS classes at all. |
| **Fix** | Converted every page: replaced `<table class="data-table">` with `<table class="govuk-table">`, marking up `<thead>`/`<tbody>`/`<tr>`/`<th>`/`<td>` with `govuk-table__head`, `govuk-table__body`, `govuk-table__row`, `govuk-table__header` (with `scope="col"`), and `govuk-table__cell`. Replaced `<form class="filter-form">` date/number search forms with individual `govuk-form-group` + `govuk-label` + `govuk-input`/`govuk-select` blocks and a `govuk-button` submit. Replaced bare `<button type="submit">Select</button>`/`Edit`/`Add QC Note` with `govuk-button` (row-level action buttons inside table cells use `govuk-button--secondary govuk-!-margin-bottom-0` to avoid oversized table rows). Removed the `.data-table`/`.filter-form` rules from `Styles.css` entirely (the file now contains no CSS rules — kept only as a stable link target). Verified with a full-tree regex scan for `class="btn"`, `form-control`, `table-striped`, `class="data-table"`, `class="filter-form"`, bare `<button>`/`<input>`/`<select>` without a `govuk-*` class — **zero matches** across the entire `Pages/` tree. Build: 0 errors. Tests: 88 pass, 1 skipped. Live smoke test: all 9 converted pages return HTTP 200. |
| **Owner** | Resolved — 2026-07-31 |

---

### ISS-016 — Application crash on launch: `InvalidOperationException` on service navigation link (Resolved)

| Field | Detail |
|-------|--------|
| **Status** | Resolved |
| **Phase** | 5 |
| **Symptom** | Immediately after the GOV.UK Design System compliance pass (Run Log #31), launching the application and requesting any page threw: `System.InvalidOperationException: 'Cannot override the 'href' attribute for <a>. An <a> with a specified 'href' must not have attributes starting with 'asp-route-' or an 'asp-action', 'asp-controller', 'asp-area', 'asp-route', 'asp-protocol', 'asp-host', 'asp-fragment', 'asp-page' or 'asp-page-handler' attribute.'` This crashed every page render, since the exception originated in the shared `_Layout.cshtml`. |
| **Root cause** | A regression introduced during the ISS-015/Run #31 GDS rewrite: the service navigation "service name" link was written as `<a href="/" class="govuk-service-navigation__link" asp-page="/Index">Histopathology System</a>` — combining a **literal `href="/"`** with the **`asp-page="/Index"` Anchor Tag Helper attribute** on the same element. ASP.NET Core's `AnchorTagHelper` throws at render time whenever a literal `href` is present alongside any `asp-*` navigation attribute (`asp-page`, `asp-action`, `asp-controller`, `asp-area`, `asp-route`, `asp-route-*`, `asp-protocol`, `asp-host`, `asp-fragment`, `asp-page-handler`) because it cannot resolve which value should take precedence — this is a hard runtime guard, not a compile-time warning, so the build had succeeded cleanly and the defect was only caught when the page was actually requested. |
| **Fix** | Removed the literal `href="/"`, keeping only `asp-page="/Index"` (which the tag helper expands to an equivalent `href="/"` at render time — confirmed via live `dotnet run` smoke test showing `<a class="govuk-service-navigation__link" href="/">Histopathology System</a>` rendered with no exception). Searched the entire `Pages/` tree with a regex for any other `<a>` combining a literal `href`/`asp-route-*` with `asp-page`/`asp-action`/`asp-controller`/`asp-area`/`asp-route`/`asp-protocol`/`asp-host`/`asp-fragment` — no other occurrences found, so this was an isolated single-file defect. Verified with `dotnet build` (0 errors, after clearing a stale MSBuild file lock from the previous `dotnet run` session via `dotnet build-server shutdown`), `dotnet test` (88 pass, 1 skipped, 0 fail), and a live app launch returning HTTP 200 on the home page. |
| **Owner** | Resolved — 2026-07-31 |

---

### ISS-017 — User Management CRUD missing (full-stack gap) (Resolved)

| Field | Detail |
|-------|--------|
| **Status** | Resolved |
| **Phase** | 5 |
| **Symptom** | User reported the legacy app's User Management CRUD capability appeared missing from the migrated application. Investigation confirmed `Admin/UserMaintenance.cshtml.cs` has only an `OnGetAsync` handler (a read-only list of users) — no Add, Edit, or Delete functionality exists anywhere on the page. |
| **Root cause** | The gap is full-stack, not just a missing form: `IUserRepository` (and its Dapper implementation `UserRepository`) only ever defined `GetUserByNtLoginAsync`, `GetUsersAsync`, and `GetUsersByAreaAsync` — read-only methods. No `CreateUserAsync`/`UpdateUserAsync` existed at the repository or service layer. Similarly, `ILookupRepository`/`LookupService` had no method to fetch the User Group / User Area pick-lists needed to populate the drop-downs for an Add/Edit form (the legacy `LookupData.GetUserGroups()`/`GetUserAreas()` calling `GetluUserGroup`/`GetluUserArea` had no C# equivalent). The legacy `UserMaintenance.aspx` supported this via an inline-editable `DataGrid` with `Pager.AllowAddNew = True`, `Pager.AllowEdit = True` (`Pager.AllowDelete` was always `False` — deactivation is done via the Active checkbox, not a hard delete), backed by `clsUser.SaveUserData`, which dispatches to the `AddUser` (insert), `EditUser` (update), and `DeleteUser` (unused, since delete is disabled in the UI) stored procedures. This CRUD capability was apparently deferred during the Phase 5 UI migration and never picked back up. |
| **Fix** | **Backend:** Added `CreateUserAsync(User, CancellationToken)` and `UpdateUserAsync(User, CancellationToken)` to `IUserRepository`, implemented in `UserRepository` via Dapper `ExecuteAsync` calls to the `AddUser`/`EditUser` stored procedures with the same parameter set as `clsUser.SaveUserData` (`NTLogin`, `Name`, `Email`, `UserGroup`, `UserArea`, `Active`, plus `ID` for update); wrapped in `UserService.CreateUserAsync`/`UpdateUserAsync` returning `bool` with structured error logging (matching the existing service-layer convention). Added `GetUserGroupsAsync`/`GetUserAreasAsync` to `ILookupRepository`/`LookupRepository`/`LookupService`, calling `GetluUserGroup`/`GetluUserArea` and mapping the `Code`/`Description` result columns onto `LookupItem.ID`/`Name` (these two stored procedures return a different column shape than the generic pick-list procedures, so a dedicated `MapCodeDescription` helper was added rather than a direct Dapper POCO map). **UI:** Added `Admin/AddUser.cshtml(.cs)` and `Admin/EditUser.cshtml(.cs)` — GDS-compliant Razor Pages (`govuk-input` for NT login/name/email, `govuk-select` for Group/Area, `govuk-checkboxes` for Active, `govuk-error-summary` for required-field validation, `govuk-button`/`govuk-button--secondary` for Save/Cancel) following the existing `EditQCNote`/`EditBatch` conventions in this codebase. Updated `Admin/UserMaintenance.cshtml(.cs)` with an "Add user" `govuk-button`, a per-row "Change" `govuk-link` (routing to `EditUser?userId=`), and a `govuk-notification-banner--success` driven by `TempData["StatusMessage"]` after a successful Add/Edit. **Verification:** Build 0 errors; 88 tests pass, 1 skipped; live `dotnet run` smoke test confirmed `/Admin/UserMaintenance`, `/Admin/AddUser` return HTTP 200, and the Group/Area drop-downs on `/Admin/AddUser` render real live data from the dev database (Customer/Histopathology User/Maintenance groups; External Customers/Histopath/Mouse Bioassay/Neuropath/Other VLA/TB Diagnostics areas) — confirming the new stored-procedure-backed lookups work end-to-end against a real database, not just compile. |
| **Owner** | Resolved — 2026-07-31 |

---

### ISS-018 — PickListMaintenance has the same CRUD gap as ISS-017 (Open — not yet implemented)

| Field | Detail |
|-------|--------|
| **Status** | Open |
| **Phase** | 5 |
| **Symptom** | While comparing the legacy app against the current implementation for other missing CRUD capability (per the user's request), the same read-only-list pattern found in ISS-017 was found in `Admin/PickListMaintenance.cshtml.cs` — it has only an `OnGetAsync` handler, listing the editable pick-list tables but with no way to Add or Edit individual rows within a table. |
| **Root cause** | Legacy `PickListMaintenanceID.aspx` (the actual per-table editor, linked from `PickListMaintenance.aspx`) has the same inline-editable-grid pattern as the legacy `UserMaintenance.aspx`: `Pager.AllowAddNew = True`, `Pager.AllowEdit = True` (gated to the "Maintenance" user group), `Pager.AllowDelete = False`. Unlike `UserMaintenance`, the Insert/Update stored procedure names are **not fixed** — they are resolved per-table at runtime via `GetEditableLookupProcs` (already partially ported as `LookupRepository.ResolveSelectProcAsync`, which only resolves the *select* procedure today). `ILookupRepository` has no `CreateLookupItemAsync`/`UpdateLookupItemAsync`, and there is no `PickListMaintenanceID`-equivalent Razor Page. |
| **Fix (recommended, not yet applied)** | Extend `EditableLookup` consumption to resolve `InsertStoredProcedure`/`UpdateStoredProcedure` (both columns already exist on the `EditableLookup` model, returned by `GetEditableLookupProcs`, but are currently unused) instead of only `SelectStoredProcedure`. Add `CreateLookupItemAsync(int tableId, LookupItem item)`/`UpdateLookupItemAsync(int tableId, LookupItem item)` to `ILookupRepository`/`LookupRepository`/`LookupService`, executing the resolved stored procedure name dynamically (mirroring `ResolveSelectProcAsync`). Add a new Razor Page (e.g. `Admin/EditLookupItem.cshtml`) restricted to users in the "Maintenance" group (mirroring the legacy `Session(SessionVars.SV_HeaderGroupName) = "Maintenance"` gate — likely via an `[Authorize(Policy = "Maintenance")]`-style check once Phase 2 auth policies exist, or a manual `Session.GroupName` check in the interim), following the same GDS form pattern established for `Admin/AddUser`/`Admin/EditUser` in ISS-017. This was intentionally **not implemented** in this session to keep the change bounded — the per-table dynamic stored-procedure resolution and Maintenance-group gating make it a meaningfully larger and differently-shaped task than the fixed-schema User CRUD restoration. |
| **Owner** | — |

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
