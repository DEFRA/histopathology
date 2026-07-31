# Legacy vs. Current Application — Full Parity Audit Report

**Project:** Histopathology System — VB.NET ASP.NET WebForms → C# .NET 10 Razor Pages (GOV.UK Design System)
**Audit date:** 2026-08-01 (original audit) — **updated 2026-08-01 following remediation batches A–F**
**Scope:** Every module, screen, feature, workflow, business rule, and CRUD operation in the legacy application (`HistopathologySystem/`), compared against the current application (`src/Histo.Web/` + domain modules).
**Method:** Full directory enumeration of both codebases, 1:1 page-name mapping, repository/service-layer interface inspection for CRUD completeness, and targeted grep of legacy business-rule and authorization patterns (`CheckPermissions()`, `web.config`, `HistopathologyLib`). Cross-referenced against `docs/migration-run-journal.md` Open Issues (ISS-001 → ISS-022) to avoid duplicate reporting.

> **Post-audit update:** Following this audit, six remediation batches (A–F, Run Log #34–#42 in `docs/migration-run-journal.md`) closed nearly all of the page-migration gaps identified below. **The page-by-page table in §2 is now a historical snapshot as of the original audit** — see the Section 10 addendum for the current, post-remediation state. Authentication (F-07/ISS-001) remains open and unaffected by this remediation work.

---

## 1. Executive Summary (original, pre-remediation)

| Area | Legacy count | Migrated | Missing | Completion |
|---|---|---|---|---|
| ASPX pages | 64 | 30 | 34 | **~47%** |
| ASCX user controls | 8 | 0 direct ports (folded into `_Layout.cshtml`/partials) | — | Functionally replaced, see §4 |
| Crystal Reports (.rpt) | 9 | 0 | 9 | **0%** — Phase 2 not started |
| Domain/repository CRUD (Batch, Submission, Animal, Tissue, Block, HistologyRef, QCNote) | — | Create/Update/Delete methods **present at interface level** for all of these | Lookup/pick-list items only | Backend ahead of UI — most gaps are missing Razor Pages, not missing repository methods |
| Authentication / authorization (`CheckPermissions()`) | ~60+ call sites across all pages | 0 | All | **0%** — no `[Authorize]`, `AddAuthentication`, or Entra ID wiring exists anywhere in `Histo.Web`; only a hard-coded Development-only stub identity |

**Headline finding (as of original audit):** The application was **not at functional parity** with the legacy system. Only 30 of 64 legacy pages (47%) had a current equivalent. **This has since been substantially remediated — see Section 10.**

---

## 2. Page-by-Page Mapping (Legacy → Current)

Legend: ✅ Migrated · ❌ Missing · ⚠️ Partial/Read-only gap (already tracked as an Open Issue)

| # | Legacy page | Current equivalent | Status | Notes |
|---|---|---|---|---|
| 1 | `AddSample.aspx` | — | ❌ | No current page. Sample creation workflow appears entirely absent from `Histo.Web`. |
| 2 | `AddSubmission.aspx` | [Submissions/AddSubmission.cshtml](../src/Histo.Web/Pages/Submissions/AddSubmission.cshtml) | ✅ | |
| 3 | `AppError.aspx` | [Shared/Error.cshtml](../src/Histo.Web/Pages/Shared/Error.cshtml) | ✅ | Functional equivalent (ASP.NET Core error page pattern) |
| 4 | `ArchiveBlocks.aspx` | [Archive/ArchiveBlocks.cshtml](../src/Histo.Web/Pages/Archive/ArchiveBlocks.cshtml) | ✅ | |
| 5 | `ArchiveMenu.aspx` | [Archive/ArchiveMenu.cshtml](../src/Histo.Web/Pages/Archive/ArchiveMenu.cshtml) | ✅ | |
| 6 | `ArchiveTissues.aspx` | [Archive/ArchiveTissues.cshtml](../src/Histo.Web/Pages/Archive/ArchiveTissues.cshtml) | ✅ | |
| 7 | `AuditLogByDate.aspx` | [AuditLog/AuditLogByDate.cshtml](../src/Histo.Web/Pages/AuditLog/AuditLogByDate.cshtml) | ✅ | |
| 8 | `AuditLogBySubmission.aspx` | [AuditLog/AuditLogBySubmission.cshtml](../src/Histo.Web/Pages/AuditLog/AuditLogBySubmission.cshtml) | ✅ | |
| 9 | `AuditLogByUser.aspx` | [AuditLog/AuditLogByUser.cshtml](../src/Histo.Web/Pages/AuditLog/AuditLogByUser.cshtml) | ✅ | |
| 10 | `AuditLogMenu.aspx` | [AuditLog/AuditLogMenu.cshtml](../src/Histo.Web/Pages/AuditLog/AuditLogMenu.cshtml) | ✅ | |
| 11 | `BatchBlocks.aspx` | — | ❌ | Block-per-batch listing/print workflow not migrated. |
| 12 | `BatchBlockSummary.aspx` | — | ❌ | |
| 13 | `BatchDetails.aspx` | [Batches/BatchDetails.cshtml](../src/Histo.Web/Pages/Batches/BatchDetails.cshtml) | ✅ | |
| 14 | `BatchesForArchiving.aspx` | [Batches/BatchesForArchiving.cshtml](../src/Histo.Web/Pages/Batches/BatchesForArchiving.cshtml) | ✅ | |
| 15 | `BatchesForDispatch.aspx` | [Batches/BatchesForDispatch.cshtml](../src/Histo.Web/Pages/Batches/BatchesForDispatch.cshtml) | ✅ | |
| 16 | `BatchesForEditing.aspx` | [Batches/BatchesForEditing.cshtml](../src/Histo.Web/Pages/Batches/BatchesForEditing.cshtml) | ✅ | |
| 17 | `BatchesNotReceived.aspx` | [Batches/BatchesNotReceived.cshtml](../src/Histo.Web/Pages/Batches/BatchesNotReceived.cshtml) | ✅ | |
| 18 | `BatchesReceived.aspx` | [Batches/BatchesReceived.cshtml](../src/Histo.Web/Pages/Batches/BatchesReceived.cshtml) | ✅ | |
| 19 | `BatchSummary.aspx` | — | ❌ | |
| 20 | `BlockDetails.aspx` | [Blocks/BlockDetails.cshtml](../src/Histo.Web/Pages/Blocks/BlockDetails.cshtml) | ✅ | |
| 21 | `BookBlockRef.aspx` | [Bookings/BookBlockRef.cshtml](../src/Histo.Web/Pages/Bookings/BookBlockRef.cshtml) | ✅ | |
| 22 | `BookHistologyRef.aspx` | [Bookings/BookHistologyRef.cshtml](../src/Histo.Web/Pages/Bookings/BookHistologyRef.cshtml) | ✅ | |
| 23 | `BookingMenu.aspx` | [Bookings/BookingMenu.cshtml](../src/Histo.Web/Pages/Bookings/BookingMenu.cshtml) | ✅ | |
| 24 | `CalendarPopup.aspx` | — | ❌* | *Not a functional gap — legacy JS date-picker popup, superseded by the native GDS date input pattern. No action required. |
| 25 | `Cassetted.aspx` | — | ❌ | Marking blocks as "cassetted" (workflow status transition) not migrated. |
| 26 | `CopyBatch.aspx` | — | ❌ | |
| 27 | `CopyBatchBlocks.aspx` | — | ❌ | |
| 28 | `CopyBatchBlocksSummary.aspx` | — | ❌ | |
| 29 | `CopyBlocks.aspx` | — | ❌ | |
| 30 | `CopySamples.aspx` | — | ❌ | |
| 31 | `CopySamplesBlocks.aspx` | — | ❌ | |
| 32 | `CopySamplesSummary.aspx` | — | ❌ | Entire "Copy Batch/Samples/Blocks" workflow family (7 pages, #26–32) has no current equivalent. |
| 33 | `EditBatch.aspx` | [Batches/EditBatch.cshtml](../src/Histo.Web/Pages/Batches/EditBatch.cshtml) | ✅ | |
| 34 | `EditHistologyRef.aspx` | — | ❌ | `IHistologyRepository.UpdateRefAsync` already exists at the repository layer (see §3) — only the UI page is missing. |
| 35 | `EditQCNote.aspx` | [QC/EditQCNote.cshtml](../src/Histo.Web/Pages/QC/EditQCNote.cshtml) | ✅ | |
| 36 | `ExcelExport.aspx` | — | ❌ | No data-export capability in the current app at all. |
| 37 | `FinalPrintBatch.aspx` | — | ❌ | Depends on Crystal Reports (Phase 2, not started). |
| 38 | `FixCompletedDates.aspx` | — | ❌ | Admin data-correction utility. |
| 39 | `Home.aspx` | [Index.cshtml](../src/Histo.Web/Pages/Index.cshtml) | ✅ | |
| 40 | `PickListMaintenance.aspx` | [Admin/PickListMaintenance.cshtml](../src/Histo.Web/Pages/Admin/PickListMaintenance.cshtml) | ⚠️ | Read-only list only. Per-table edit is `PickListMaintenanceID.aspx` (#41) — missing. Tracked as **ISS-018**. |
| 41 | `PickListMaintenanceID.aspx` | — | ❌ | Per-table inline Add/Edit editor. See ISS-018. |
| 42 | `PickListUserArea.aspx` | — | ❌ | |
| 43 | `QCNoteForm.aspx` | — | ❌ | `IQCNoteRepository.AddAsync` exists at repository layer, but current `QC/EditQCNote.cshtml` only edits existing notes — there is no "Add QC Note" Razor Page equivalent to this legacy Create form. |
| 44 | `QCNotes.aspx` | [QC/QCNotes.cshtml](../src/Histo.Web/Pages/QC/QCNotes.cshtml) | ✅ | |
| 45 | `QualityData.aspx` | — | ❌ | |
| 46 | `ReceiveBatch.aspx` | [Batches/ReceiveBatch.cshtml](../src/Histo.Web/Pages/Batches/ReceiveBatch.cshtml) | ✅ | |
| 47 | `SearchArchiveLocation.aspx` | — | ❌ | |
| 48 | `SearchBlockRefs.aspx` | — | ❌ | |
| 49 | `SearchMenu.aspx` | [Search/SearchMenu.cshtml](../src/Histo.Web/Pages/Search/SearchMenu.cshtml) | ⚠️ | Menu shell only migrated — **none** of the 8 legacy search screens it links to (#47, #48, #50–55) have been built. The entire Search module is functionally a dead-end menu. |
| 50 | `SearchPMDates.aspx` | — | ❌ | |
| 51 | `SearchSample.aspx` | — | ❌ | |
| 52 | `SearchSender.aspx` | — | ❌ | |
| 53 | `SearchSubmissions.aspx` | — | ❌ | |
| 54 | `SearchTest.aspx` | — | ❌ | |
| 55 | `SearchUnUsedHistologyRefs.aspx` | — | ❌ | |
| 56 | `SubmissionDetails.aspx` | — | ❌ | Distinct from `Batches/BatchDetails.cshtml` — no submission-level detail page exists. |
| 57 | `SubmissionDetailsBlock.aspx` | — | ❌ | |
| 58 | `SubmissionForm.aspx` | — | ❌* | *Possible overlap with `AddSubmission.cshtml` — recommend a source-level confirmation (read `SubmissionForm.aspx.vb`) before treating as fully distinct, since legacy WebForms occasionally split a single logical form into two ASPX files (list + form partial). |
| 59 | `SubmissionNotes.aspx` | — | ❌ | |
| 60 | `SubmissionsOnHold.aspx` | [Batches/SubmissionsOnHold.cshtml](../src/Histo.Web/Pages/Batches/SubmissionsOnHold.cshtml) | ✅ | |
| 61 | `UserMaintenance.aspx` | [Admin/UserMaintenance.cshtml](../src/Histo.Web/Pages/Admin/UserMaintenance.cshtml) + [Admin/AddUser.cshtml](../src/Histo.Web/Pages/Admin/AddUser.cshtml) + [Admin/EditUser.cshtml](../src/Histo.Web/Pages/Admin/EditUser.cshtml) | ✅ | Full CRUD restored per ISS-017 |
| 62 | `ViewImportedData.aspx` | — | ❌ | |
| 63 | `ViewSamples.aspx` | [Submissions/ViewSamples.cshtml](../src/Histo.Web/Pages/Submissions/ViewSamples.cshtml) | ✅ | |
| 64 | `ViewSubmissions.aspx` | [Submissions/ViewSubmissions.cshtml](../src/Histo.Web/Pages/Submissions/ViewSubmissions.cshtml) | ✅ | |

**Totals:** 30 ✅ fully migrated · 2 ⚠️ partial (already tracked as ISS-018, plus SearchMenu newly identified) · 1 ❌* not a real gap (CalendarPopup) · 31 ❌ genuinely missing.

---

## 3. ASCX User Controls

All 8 legacy user controls have a **functional** (not literal 1:1) replacement:

| Legacy control | Current replacement | Status |
|---|---|---|
| `VLAHeader.ascx` | [Shared/_Layout.cshtml](../src/Histo.Web/Pages/Shared/_Layout.cshtml) header + `HistoPageModel` session resolution | ✅ Replaced |
| `VLAFooter.ascx` | `_Layout.cshtml` GOV.UK footer | ✅ Replaced |
| `DataGridPager.ascx` | Native GDS pagination component (per-page implementation) | ✅ Replaced (pattern only — confirm consistent use across all list pages) |
| `Batch.ascx` | Inlined into `Batches/*.cshtml` partials/view models | ✅ Replaced |
| `HistologyRef.ascx` | Inlined into `Bookings/BookHistologyRef.cshtml` / `Blocks/BlockDetails.cshtml` | ✅ Replaced |
| `CalendarDate.ascx` | Native `<input type="date">` / GDS date input | ✅ Replaced |
| `MouseNumber.ascx` | Not confirmed — no direct reference found in current page set | ⚠️ Verify usage in submission/animal forms once `AddSample.aspx` equivalent is built |
| `SenderRef.ascx` | Not confirmed — depends on `SearchSender.aspx`/sender lookups, which are missing (see §2, item 52) | ❌ Effectively missing, tied to Search module gap |

---

## 4. Reporting Module (Crystal Reports)

**Status: 0 of 9 reports migrated.** `src/Histo.Reporting/` exists only as an empty project stub (`.csproj`, `bin/`, `obj/` — no source files). This matches the Phase Tracker ("Phase 2 — Reporting Migration: Not Started") and is not a new finding, but it blocks several of the missing pages above:

- `HistologyReport.rpt`, `HistologySubReport.rpt` — no consuming page exists (`FinalPrintBatch.aspx` missing)
- `QCNote.rpt` — no consuming page exists
- `SubmissionAntibodiesReport.rpt`, `SubmissionBlocksReport.rpt`, `SubmissionHistologyReport.rpt`, `SubmissionNotesReport.rpt`, `SubmissionSpecialStainReport.rpt`, `SubmissionTissuesReport.rpt` — all tied to missing `SubmissionDetails.aspx`/`SubmissionNotes.aspx`/`SubmissionDetailsBlock.aspx` pages

**Recommendation:** Reporting migration (Phase 2) and the missing "print"/"export" pages in §2 (`FinalPrintBatch`, `ExcelExport`, `ViewImportedData`) should be planned together — they share the same underlying data and are currently blocked on the same Crystal Reports removal work.

---

## 5. CRUD Parity — Domain / Repository Layer

This is the most important nuance of this audit: **the backend domain layer (Phase 4) is materially ahead of the UI layer (Phase 5).** Interface inspection shows Create/Update/Delete methods already exist for most entities — the gap is predominantly *missing Razor Pages that would call them*, not missing business logic.

| Module | Repository | Create | Update | Delete | UI exposure |
|---|---|---|---|---|---|
| `Histo.Administration` | `IUserRepository` | ✅ `CreateUserAsync` (ISS-017) | ✅ `UpdateUserAsync` (ISS-017) | n/a (legacy has no hard delete either — deactivate via Active flag) | ✅ Full (AddUser/EditUser/UserMaintenance) |
| `Histo.Administration` | `ILookupRepository` | ❌ None | ❌ None | n/a | ⚠️ Read-only list only — **ISS-018, open** |
| `Histo.Submissions` | `IBatchRepository` | ✅ `AddAsync` | ✅ `UpdateAsync`, `UpdateStatusAsync` | n/a | ✅ Partial (EditBatch/BatchDetails exist; Copy-batch workflow pages #26–29 missing) |
| `Histo.Submissions` | `ISubmissionRepository` | ✅ `AddSubmissionAsync`, `AddAnimalAsync`, `AddTissueAsync` | ✅ `UpdateSubmissionAsync`, `UpdateAnimalAsync`, `UpdateTissueAsync` | ✅ `DeleteAnimalAsync`, `DeleteTissueAsync` | ⚠️ Only `AddSubmission` is exposed; no UI for `SubmissionDetails`/`SubmissionNotes`/animal or tissue edit-delete |
| `Histo.Histology` | `IBlockRepository` | Not confirmed in this pass — recommend explicit check | — | ✅ `DeleteAsync` | ⚠️ `BlockDetails` (view) exists; no Add/Copy-blocks UI (#27–29) |
| `Histo.Histology` | `IHistologyRepository` | Not confirmed in this pass | ✅ `UpdateRefAsync` | — | ❌ No `EditHistologyRef` UI page exists to call `UpdateRefAsync` |
| `Histo.QualityControl` | `IQCNoteRepository` | ✅ `AddAsync` | ✅ `UpdateAsync` (rowstamp concurrency) | — | ⚠️ Only Edit is exposed (`EditQCNote`); no "Add QC Note" page to call `AddAsync` (legacy `QCNoteForm.aspx`) |
| `Histo.AuditLog` | `IAuditLogRepository` | n/a (audit logs are append-only by design) | n/a | n/a | ✅ Read-only by design — not a gap |

**Conclusion:** Beyond the already-tracked ISS-018 (Lookup/PickList CRUD), there is **no additional repository-layer CRUD gap** of the same severity as ISS-017 was. The remaining CRUD-shaped gaps are all "backend method exists, UI page does not" — i.e., they are properly UI-migration backlog items, not domain-layer regressions.

---

## 6. Business Rules and Workflow Parity

| Rule / workflow | Legacy location | Current status |
|---|---|---|
| PG-number auto-reversal | `HistopathologyLib/clsAnimal.vb` | ✅ Ported to `AnimalHelpers` per Phase 4, confirmed via existing `SubmissionServiceAnimalTests` |
| QC Note rowstamp concurrency | `clsQCNote.vb` | ✅ Ported — `QCNoteConcurrencyException` implemented and unit-tested |
| Batch status rowstamp concurrency | `clsBatch.vb` | ✅ `UpdateStatusAsync(..., byte[] rowStamp, ...)` present in `IBatchRepository` |
| **Authorization (`CheckPermissions()`)** | Present in **every** legacy code-behind (confirmed ≥60 call sites across `ArchiveBlocks`, `ArchiveMenu`, `ArchiveTissues`, `AuditLogByDate/BySubmission/ByUser/Menu`, `BatchBlocks`, `BatchesForArchiving`, `BatchesForDispatch`, `BatchesForEditing`, and all remaining pages per ISS-004) | ❌ **Not implemented.** No `[Authorize]` attribute, `AddAuthentication`, or `AddMicrosoftIdentityWebApp` call exists anywhere in `Histo.Web/Program.cs` or any Razor Page. The only identity mechanism present is a **hard-coded Development-environment stub** (`GroupName = "Maintenance"`, added under ISS-013) that grants full access unconditionally in dev. This is consistent with Phase Tracker's Phase 1 status of "In Progress" but confirms **zero production-ready authorization exists today** — every migrated page is currently wide open with no group/role gating equivalent to the legacy Customer/Histopathology User/Maintenance group checks. |
| Session-scoped user context (`SessionVars`) | `SessionVars.vb` | ✅ Replaced by `ISessionService`/`SessionService` (ISS-013 fix) — functionally equivalent for `GroupName`/`UserID`/`UserArea` propagation, but **not yet gated by any authorization policy** (see above) |
| Windows Authentication → Entra ID | Web.config `<authentication mode="Windows">` | ❌ Not started — ISS-001/ISS-009/D-004 all still Open |

---

## 7. Consolidated Findings

| # | Finding | Severity | Status | Recommendation |
|---|---|---|---|---|
| F-01 | 34 of 64 legacy pages (53%) have no current equivalent, spanning Sample creation, Batch/Block copy workflows (7 pages), the entire Search module (8 of 9 pages), Submission detail/notes, Excel export, and admin data-fix utilities | **High** | New (quantifies existing ISS-004) | Prioritize a Phase 5 backlog ordered by business criticality: Search module and Submission Details are likely highest-traffic; Copy-workflow and admin fix-up pages are likely lowest. Re-plan against `docs/Migration-Plan.md` Batch 6+. |
| F-02 | Reporting module (9 Crystal Reports) at 0% — blocks `FinalPrintBatch`, `ViewImportedData`, and all Submission sub-reports | High | Already tracked (Phase 2 "Not Started") | No new action beyond existing plan; flag as a hard dependency for F-01's Submission/Print pages. |
| F-03 | `ILookupRepository` has no Create/Update for pick-list items; `PickListMaintenanceID.aspx` equivalent missing | Medium | Already tracked as **ISS-018** | No new action — implement per the existing ISS-018 remediation plan. |
| F-04 | `SearchMenu.cshtml` is a live menu page linking to 8 search screens, none of which exist — a functional dead end for any user navigating to Search | High | New | Treat the Search module as a dedicated Phase 5 batch; until built, consider hiding or disabling the non-functional links from `SearchMenu.cshtml` to avoid presenting broken navigation to users. |
| F-05 | No "Add QC Note" page exists — `IQCNoteRepository.AddAsync` is unreachable from the UI; only note-editing is exposed | Medium | New | Add a `QC/AddQCNote.cshtml` page mirroring the existing `EditQCNote` pattern; this is a small, self-contained slice since the repository/service method already exists. |
| F-06 | `EditHistologyRef.aspx` has no current equivalent despite `IHistologyRepository.UpdateRefAsync` already existing | Medium | New | Small, self-contained slice — same shape as F-05. |
| F-07 | Authentication/authorization is entirely unimplemented in `Histo.Web` — only a Development-only stub identity exists; no `[Authorize]` policies replace any of the ~60+ legacy `CheckPermissions()` call sites | **Critical (pre-production blocker)** | Already tracked (ISS-001, D-004, Phase 1 "In Progress") but this audit confirms **0% code-level progress**, not partial progress | This must be resolved before any environment beyond local development is exposed. Recommend engaging the `Identity Migration Agent (Entra ID for .NET Framework 4.8)`-equivalent workflow for .NET 10/Entra ID as the next priority slice, ahead of further page migration, since every currently-migrated page is unprotected. |
| F-08 | `MouseNumber.ascx` and `SenderRef.ascx` have no confirmed current replacement | Low | New | Re-verify once `AddSample.aspx` and `SearchSender.aspx` equivalents are built (F-01) — these controls are tied to those missing pages. |
| F-09 | `SubmissionForm.aspx` vs. `AddSubmission.cshtml` overlap is unconfirmed | Low | New (needs verification) | Read `SubmissionForm.aspx.vb` to confirm whether this is a duplicate of `AddSubmission` or a genuinely distinct edit form, before adding it to the missing-pages backlog. |
| F-10 | Plaintext SQL credential and `debug="true"` in legacy `Web.config` | High | Already tracked (ISS-006, ISS-007) | No new action — remediate per existing plan before any Azure deployment, per `azure-infra.instructions.md` §3. |

---

## 8. Required Actions for Full Parity (Prioritized)

1. **Authentication (blocker, do first):** Implement Entra ID authentication and replace all `CheckPermissions()` equivalents with `[Authorize(Policy = ...)]` per D-004. Zero pages are currently protected. (F-07)
2. **Search module rebuild:** Build the 8 missing search screens linked from `SearchMenu.cshtml`, or disable the dead links until built. (F-04)
3. **Submission detail/notes pages:** `SubmissionDetails`, `SubmissionDetailsBlock`, `SubmissionNotes` — core workflow pages with no current equivalent. (F-01)
4. **Small self-contained CRUD slices:** `QC/AddQCNote.cshtml` and `Admin/EditHistologyRef.cshtml` — repository methods already exist, only the page is missing. (F-05, F-06)
5. **PickListMaintenance CRUD:** Complete per the existing ISS-018 plan. (F-03)
6. **Sample/Batch/Block copy workflow (7 pages):** `AddSample`, `CopyBatch*` (4), `CopyBlocks`, `CopySamples*` (3), `Cassetted`. Evaluate whether all are still business-required before committing migration effort — some may be superseded by simplified GDS workflows rather than ported 1:1.
7. **Reporting (Phase 2):** Begin Crystal Reports → Razor/PDF migration; this unblocks `FinalPrintBatch`, `ExcelExport`, `ViewImportedData`, and the Submission sub-report pages. (F-02)
8. **Verification items:** Confirm `SubmissionForm.aspx` overlap with `AddSubmission` (F-09) and `MouseNumber.ascx`/`SenderRef.ascx` disposition (F-08) before final backlog sign-off.

---

## 9. Cross-Reference to Existing Open Issues

This audit does not duplicate but **confirms and quantifies** the following existing journal issues: ISS-001 (auth wiring), ISS-004 (all 64 pages required before cutover — this audit shows only 30 done), ISS-006/ISS-007 (Web.config secrets/debug), ISS-009 (NT login→UPN mapping), ISS-018 (PickListMaintenance CRUD). No issue in this report contradicts the existing journal; F-01 through F-09 (excluding duplicates) are **new** findings raised by this audit and should be added to Open Issues as ISS-019 through ISS-02x by the next journal update.

---

---

## 10. Addendum — Post-Remediation State (2026-08-01, same day)

Following the original audit above, the user directed a full remediation pass prioritizing page-migration completion over defect resolution. Six batches (A–F) were executed via the `ui-implementation` agent; full details are in `docs/migration-run-journal.md` Run Log #34–#42.

### What was resolved

| Batch | Pages | Outcome |
|---|---|---|
| A | AddQCNote, EditHistologyRef (pool-level counter) | Built — repository methods already existed |
| B | All 8 Search module pages | Built — resolves ISS-020 (dead menu links) |
| C | SubmissionDetails, SubmissionDetailsBlock | Built. `SubmissionForm`/`SubmissionNotes` determined to be Crystal Reports PDF popups, not missing UI — reclassified as Phase 2 (reporting) scope |
| D1 | AddSample, Cassetted | Built, with corrected functional understanding vs. original audit's assumptions (Cassetted is the "New Submission" type-selection step, not a block-status transition) |
| D2 | CopyBatch/CopyBatchBlocks/CopyBatchBlocksSummary, CopyBlocks, CopySamples/CopySamplesBlocks/CopySamplesSummary (7 legacy pages) | Built as a consolidated 5-page wizard flow — no new repository code needed |
| E1 | PickListMaintenanceID, PickListUserArea | Built — resolves **ISS-018** |
| E2 | BatchBlocks, BatchSummary, BatchBlockSummary | Determined to be **already covered** by `BlockDetails`/`ViewSamples`/`SubmissionDetails` — no new pages needed; closed a real navigation gap (ViewSamples Edit/Delete) found during verification |
| E3 | QualityData, FixCompletedDates | Built. A CS0663 build-breaking error introduced by the subagent (duplicate `ref`/`out` overload) was caught and fixed by the calling agent before proceeding |
| F | ExcelExport, ViewImportedData | Built. Corrected the original audit's assumption that these depend on Crystal Reports — neither does. `ExcelExport` was a cross-cutting CSV export utility added to 4 already-migrated pages; `ViewImportedData` is a standalone data view. `FinalPrintBatch` was confirmed as genuinely blocked on Phase 2 (its only two actions launch the Crystal Reports popups) |

### Updated completion figures

| Metric | Original audit | Post-remediation |
|---|---|---|
| Distinct Razor Pages in `src/Histo.Web/Pages/` | 30 | **57** |
| Legacy pages requiring no separate port (superseded/already-covered) | Not yet determined | 7 (`CalendarPopup` superseded by native date input; `BatchBlocks`/`BatchSummary`/`BatchBlockSummary` covered by other pages; `SubmissionForm`/`SubmissionNotes`/`FinalPrintBatch` are Phase 2 reporting, not Phase 5 UI) |
| Remaining genuinely-missing UI pages | 34 | **0** |
| Build status | 0 errors (baseline) | 0 errors, 0 warnings |
| Test status | 88 pass, 1 skipped | 90 pass, 1 skipped (2 new tests added) |

**Effectively, Phase 5 (UI Migration) is now complete for every page that does not depend on Phase 2 (Reporting).** The only remaining page-shaped gap is `FinalPrintBatch.aspx` and its two Crystal Reports popups (`SubmissionForm.aspx`, `SubmissionNotes.aspx`), which cannot be meaningfully built until Phase 2 reporting migration begins.

### What remains open (unaffected by this remediation)

- **F-07 / ISS-001 (Critical):** Authentication/authorization is still 0% implemented. No page — old or newly built in this remediation — has any `[Authorize]` gating. This remains the top-priority blocker for any non-development deployment.
- **ISS-022 (new, found during Batch A):** The true legacy `EditHistologyRef.aspx` per-animal Sender/Histology Ref renamer has no repository support at all (distinct from the pool-level counter update that was built). Open, medium severity.
- **Phase 2 (Reporting):** Still 0% — all 9 Crystal Reports, plus the 3 print-popup pages, remain unmigrated.
- **F-08/F-09 verification items** from the original audit were resolved during remediation (SubmissionForm confirmed distinct/reporting-only; MouseNumber.ascx/SenderRef.ascx dependencies resolved as part of the AddSample/Search work).

---

*Generation date: 2026-08-01. This report is a point-in-time audit based on static code/directory inspection — no live application testing was performed as part of this pass.*
