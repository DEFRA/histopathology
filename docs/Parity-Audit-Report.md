# Legacy vs. Current Application — Full Parity Audit Report

**Project:** Histopathology System — VB.NET ASP.NET WebForms → C# .NET 10 Razor Pages (GOV.UK Design System)
**Audit date:** 2026-08-01 (original audit) — **updated 2026-08-01 following remediation batches A–F** — **further updated 2026-08-03 following ISS-022 resolution (Run Log #43)** — **further updated 2026-08-28: Entra ID SAML 2.0 authentication (F-07/ISS-001) implemented and confirmed — see §12**
**Scope:** Every module, screen, feature, workflow, business rule, and CRUD operation in the legacy application (`HistopathologySystem/`), compared against the current application (`src/Histo.Web/` + domain modules).
**Method:** Full directory enumeration of both codebases, 1:1 page-name mapping, repository/service-layer interface inspection for CRUD completeness, and targeted grep of legacy business-rule and authorization patterns (`CheckPermissions()`, `web.config`, `HistopathologyLib`). Cross-referenced against `docs/migration-run-journal.md` Open Issues (ISS-001 → ISS-022) to avoid duplicate reporting.

> **Post-audit update (2026-08-03):** All page-migration gaps identified below are now resolved, including ISS-022 (the final Phase 5 gap — the per-animal Sender/Histology Ref rename workflow, `Admin/EditAnimalRef.cshtml`). **The page-by-page table in §2 and the Executive Summary in §1 have been corrected in place below to reflect current state** — the original pre-remediation figures are retained in §1a and §10 for audit-trail purposes only. Authentication (F-07/ISS-001) remains open and is unaffected by this remediation work — it is now the sole critical pre-production blocker.
>
> **Post-audit update (2026-08-28):** Authentication/authorization (F-07/ISS-001) is now **implemented and confirmed** — Entra ID SAML 2.0 via `ITfoxtec.Identity.Saml2.MvcCore`, with the temporary NTLogin bridge (ADR-006, `Login.cshtml`) decommissioned. This was the last remaining **Critical**-severity finding in this report. See §12 for the full closure detail. The Executive Summary and §6/§7/§9/§11 tables below are corrected in place; historical figures are retained for audit-trail purposes.

---

## 1. Executive Summary

**Current status (2026-08-28):** 60 of 64 legacy pages migrated or functionally superseded; 3 pages (`FinalPrintBatch`, `SubmissionForm`, `SubmissionNotes`) remain blocked on Phase 2 (Reporting, still 0% started); 1 page (`CalendarPopup`) is not applicable (superseded by the native GDS date input). The domain/repository CRUD layer is now at full parity with its consuming UI — no outstanding "backend exists, page missing" gaps remain (ISS-018, ISS-020, ISS-021, ISS-022 all resolved). **Authentication/authorization is now fully implemented** — Entra ID SAML 2.0 via `ITfoxtec.Identity.Saml2.MvcCore`, replacing the temporary NTLogin bridge (ADR-006, decommissioned 2026-08-28) — closing the sole remaining critical pre-production blocker (ISS-001, F-07).

| Area | Legacy count | Migrated / Superseded | Blocked / N/A | Completion |
|---|---|---|---|---|
| ASPX pages | 64 | 60 | 3 blocked on Phase 2, 1 N/A | **~94%** (100% of non-reporting-blocked pages) |
| ASCX user controls | 8 | 8 functionally replaced (see §3) | — | 100% |
| Crystal Reports (.rpt) | 9 | 0 | 9 | **0%** — Phase 2 not started |
| Domain/repository CRUD (Batch, Submission, Animal, Tissue, Block, HistologyRef, QCNote, Lookup) | — | Create/Update/Delete methods present at interface level **and** exposed via UI for every entity | None outstanding | **Full parity** |
| Authentication / authorization (`CheckPermissions()`) | ~60+ call sites across all pages | 60/60 pages gated via `HistoPageModel`'s two-gate model (SAML `ChallengeResult` + `tblUser` group-claim check) | None outstanding | **100% — Implemented** |

**Headline finding (current):** UI migration (Phase 5) is complete for every page that does not depend on Phase 2 Reporting, and authentication/authorization (Phase 2) is now fully implemented via Entra ID SAML 2.0. The application's only remaining pre-production gap is **Crystal Reports migration (Phase 3/Reporting)** — see §11 for the full list of pending migrations and gaps.

---

### 1a. Original Executive Summary (pre-remediation, 2026-08-01 — retained for audit trail)

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
| 1 | `AddSample.aspx` | [Submissions/AddSubmission.cshtml](../src/Histo.Web/Pages/Submissions/AddSubmission.cshtml) | ⚠️ Superseded, with deviations | Consolidated into `AddSubmission` (optional `senderRef` pre-fill). Mouse-range bulk entry and Excel upload not reproduced — see §13. |
| 2 | `AddSubmission.aspx` | [Submissions/AddSubmission.cshtml](../src/Histo.Web/Pages/Submissions/AddSubmission.cshtml) | ✅ | |
| 3 | `AppError.aspx` | [Shared/Error.cshtml](../src/Histo.Web/Pages/Shared/Error.cshtml) | ✅ | Functional equivalent (ASP.NET Core error page pattern) |
| 4 | `ArchiveBlocks.aspx` | [Archive/ArchiveBlocks.cshtml](../src/Histo.Web/Pages/Archive/ArchiveBlocks.cshtml) | ✅ | |
| 5 | `ArchiveMenu.aspx` | [Archive/ArchiveMenu.cshtml](../src/Histo.Web/Pages/Archive/ArchiveMenu.cshtml) | ✅ | |
| 6 | `ArchiveTissues.aspx` | [Archive/ArchiveTissues.cshtml](../src/Histo.Web/Pages/Archive/ArchiveTissues.cshtml) | ✅ | |
| 7 | `AuditLogByDate.aspx` | [AuditLog/AuditLogByDate.cshtml](../src/Histo.Web/Pages/AuditLog/AuditLogByDate.cshtml) | ✅ | |
| 8 | `AuditLogBySubmission.aspx` | [AuditLog/AuditLogBySubmission.cshtml](../src/Histo.Web/Pages/AuditLog/AuditLogBySubmission.cshtml) | ✅ | |
| 9 | `AuditLogByUser.aspx` | [AuditLog/AuditLogByUser.cshtml](../src/Histo.Web/Pages/AuditLog/AuditLogByUser.cshtml) | ✅ | |
| 10 | `AuditLogMenu.aspx` | [AuditLog/AuditLogMenu.cshtml](../src/Histo.Web/Pages/AuditLog/AuditLogMenu.cshtml) | ✅ | |
| 11 | `BatchBlocks.aspx` | [Submissions/SubmissionDetailsBlock.cshtml](../src/Histo.Web/Pages/Submissions/SubmissionDetailsBlock.cshtml) | ✅ Superseded | Batch-wide mode (no `AnimalId`). `Blocks/BlockDetails.cshtml` was deleted in the 2026-08-28 consolidation. |
| 12 | `BatchBlockSummary.aspx` | [Submissions/SampleSummary.cshtml](../src/Histo.Web/Pages/Submissions/SampleSummary.cshtml) + [Submissions/SubmissionDetailsBlock.cshtml](../src/Histo.Web/Pages/Submissions/SubmissionDetailsBlock.cshtml) | ⚠️ Superseded, with deviations | Merged with `BatchSummary.aspx` into one sample list. See §13 for dropped inline histology-ref editing and paging. |
| 13 | `BatchDetails.aspx` | [Batches/BatchDetails.cshtml](../src/Histo.Web/Pages/Batches/BatchDetails.cshtml) | ✅ | |
| 14 | `BatchesForArchiving.aspx` | [Batches/BatchesForArchiving.cshtml](../src/Histo.Web/Pages/Batches/BatchesForArchiving.cshtml) | ✅ | |
| 15 | `BatchesForDispatch.aspx` | [Batches/BatchesForDispatch.cshtml](../src/Histo.Web/Pages/Batches/BatchesForDispatch.cshtml) | ✅ | |
| 16 | `BatchesForEditing.aspx` | [Batches/BatchesForEditing.cshtml](../src/Histo.Web/Pages/Batches/BatchesForEditing.cshtml) | ✅ | |
| 17 | `BatchesNotReceived.aspx` | [Batches/BatchesNotReceived.cshtml](../src/Histo.Web/Pages/Batches/BatchesNotReceived.cshtml) | ✅ | |
| 18 | `BatchesReceived.aspx` | [Batches/BatchesReceived.cshtml](../src/Histo.Web/Pages/Batches/BatchesReceived.cshtml) | ✅ | |
| 19 | `BatchSummary.aspx` | [Submissions/SampleSummary.cshtml](../src/Histo.Web/Pages/Submissions/SampleSummary.cshtml) + [Submissions/SubmissionDetails.cshtml](../src/Histo.Web/Pages/Submissions/SubmissionDetails.cshtml) | ⚠️ Superseded, with deviations | Merged with `BatchBlockSummary.aspx` into one sample list. See §13. |
| 20 | `BlockDetails.aspx` | [Submissions/SubmissionDetailsBlock.cshtml](../src/Histo.Web/Pages/Submissions/SubmissionDetailsBlock.cshtml) | ✅ Superseded | Animal-scoped mode (`AnimalId` supplied). |
| 21 | `BookBlockRef.aspx` | [Bookings/BookBlockRef.cshtml](../src/Histo.Web/Pages/Bookings/BookBlockRef.cshtml) | ✅ | |
| 22 | `BookHistologyRef.aspx` | [Bookings/BookHistologyRef.cshtml](../src/Histo.Web/Pages/Bookings/BookHistologyRef.cshtml) | ✅ | |
| 23 | `BookingMenu.aspx` | [Bookings/BookingMenu.cshtml](../src/Histo.Web/Pages/Bookings/BookingMenu.cshtml) | ✅ | |
| 24 | `CalendarPopup.aspx` | — | ❌* | *Not a functional gap — legacy JS date-picker popup, superseded by the native GDS date input pattern. No action required. |
| 25 | `Cassetted.aspx` | [Batches/Cassetted.cshtml](../src/Histo.Web/Pages/Batches/Cassetted.cshtml) | ✅ | Built in Batch D1 — corrected understanding: this is the "New Submission" type-selection step, not a block-status transition. |
| 26 | `CopyBatch.aspx` | [Batches/CopyBatch.cshtml](../src/Histo.Web/Pages/Batches/CopyBatch.cshtml) | ✅ | Built in Batch D2. |
| 27 | `CopyBatchBlocks.aspx` | [Batches/CopyBatch.cshtml](../src/Histo.Web/Pages/Batches/CopyBatch.cshtml) + [Batches/CopyBatchSummary.cshtml](../src/Histo.Web/Pages/Batches/CopyBatchSummary.cshtml) | ✅ Consolidated | Built in Batch D2. |
| 28 | `CopyBatchBlocksSummary.aspx` | [Batches/CopyBatchSummary.cshtml](../src/Histo.Web/Pages/Batches/CopyBatchSummary.cshtml) | ✅ Consolidated | Built in Batch D2. |
| 29 | `CopyBlocks.aspx` | [Blocks/CopyBlocks.cshtml](../src/Histo.Web/Pages/Blocks/CopyBlocks.cshtml) | ✅ | Built in Batch D2. |
| 30 | `CopySamples.aspx` | [Blocks/CopySamples.cshtml](../src/Histo.Web/Pages/Blocks/CopySamples.cshtml) | ✅ | Built in Batch D2. |
| 31 | `CopySamplesBlocks.aspx` | [Blocks/CopySamples.cshtml](../src/Histo.Web/Pages/Blocks/CopySamples.cshtml) | ✅ Consolidated | Built in Batch D2. |
| 32 | `CopySamplesSummary.aspx` | [Blocks/CopySamplesSummary.cshtml](../src/Histo.Web/Pages/Blocks/CopySamplesSummary.cshtml) | ✅ | Entire "Copy Batch/Samples/Blocks" workflow family (7 pages, #26–32) resolved in Batch D2 — consolidated into a 5-page wizard flow, no new repository code needed. |
| 33 | `EditBatch.aspx` | [Batches/EditBatch.cshtml](../src/Histo.Web/Pages/Batches/EditBatch.cshtml) | ✅ | |
| 34 | `EditHistologyRef.aspx` | [Bookings/EditHistologyRef.cshtml](../src/Histo.Web/Pages/Bookings/EditHistologyRef.cshtml) (pool-counter update) + [Admin/EditAnimalRef.cshtml](../src/Histo.Web/Pages/Admin/EditAnimalRef.cshtml) (per-animal Sender/Histology Ref rename) | ✅ | Two distinct legacy workflows shared this name. Pool-counter page built in Batch A; the true per-animal renamer (`clsAnimal.UpdateAnimalSenderRef`/`UpdateAnimalHistologyRef`) was a genuine backend gap (ISS-022) resolved 2026-08-03 — new repository methods added, page built as `Admin/EditAnimalRef.cshtml`. |
| 35 | `EditQCNote.aspx` | [QC/EditQCNote.cshtml](../src/Histo.Web/Pages/QC/EditQCNote.cshtml) | ✅ | |
| 36 | `ExcelExport.aspx` | `CsvExportHelper` wired into 4 pages (AuditLogByDate, AuditLogBySubmission, AuditLogByUser, SearchArchiveLocation) | ✅ | Built in Batch F — plain CSV export, faithful equivalent to "opens in Excel", no new NuGet dependency. |
| 37 | `FinalPrintBatch.aspx` | — | ❌ | Confirmed genuinely blocked on Phase 2 (Batch F investigation) — its only two actions launch the Crystal Reports popups below; a shell page with two non-functional buttons was judged not to add value and intentionally deferred. |
| 38 | `FixCompletedDates.aspx` | [Admin/FixCompletedDates.cshtml](../src/Histo.Web/Pages/Admin/FixCompletedDates.cshtml) | ✅ | Built in Batch E3. |
| 39 | `Home.aspx` | [Index.cshtml](../src/Histo.Web/Pages/Index.cshtml) | ✅ | |
| 40 | `PickListMaintenance.aspx` | [Admin/PickListMaintenance.cshtml](../src/Histo.Web/Pages/Admin/PickListMaintenance.cshtml) | ✅ | Full CRUD restored — per-row Edit links to #41 added. **ISS-018 resolved.** |
| 41 | `PickListMaintenanceID.aspx` | [Admin/EditLookupItem.cshtml](../src/Histo.Web/Pages/Admin/EditLookupItem.cshtml) | ✅ | Built in Batch E1 — resolves ISS-018. |
| 42 | `PickListUserArea.aspx` | [Admin/PickListUserArea.cshtml](../src/Histo.Web/Pages/Admin/PickListUserArea.cshtml) | ✅ | Built in Batch E1. |
| 43 | `QCNoteForm.aspx` | [QC/AddQCNote.cshtml](../src/Histo.Web/Pages/QC/AddQCNote.cshtml) | ✅ | Built in Batch A — mirrors the legacy two-step Add+Update note-text flow. |
| 44 | `QCNotes.aspx` | [QC/QCNotes.cshtml](../src/Histo.Web/Pages/QC/QCNotes.cshtml) | ✅ | |
| 45 | `QualityData.aspx` | [QC/QualityData.cshtml](../src/Histo.Web/Pages/QC/QualityData.cshtml) + [QC/EditQualityDataTest.cshtml](../src/Histo.Web/Pages/QC/EditQualityDataTest.cshtml) | ✅ | Built in Batch E3 — deliberately simplified to edit one test at a time rather than the legacy multi-select batch-save. |
| 46 | `ReceiveBatch.aspx` | [Batches/ReceiveBatch.cshtml](../src/Histo.Web/Pages/Batches/ReceiveBatch.cshtml) | ✅ | |
| 47 | `SearchArchiveLocation.aspx` | [Search/SearchArchiveLocation.cshtml](../src/Histo.Web/Pages/Search/SearchArchiveLocation.cshtml) | ✅ | Built in Run #27 — hierarchical expand/collapse grids reproduced as flat GOV.UK tables (documented simplification). |
| 48 | `SearchBlockRefs.aspx` | [Search/SearchBlockRefs.cshtml](../src/Histo.Web/Pages/Search/SearchBlockRefs.cshtml) | ✅ | Built in Run #27. |
| 49 | `SearchMenu.aspx` | [Search/SearchMenu.cshtml](../src/Histo.Web/Pages/Search/SearchMenu.cshtml) | ✅ | All 8 linked search screens now built (Runs #27, #35) — **ISS-020 resolved**, no more dead links. |
| 50 | `SearchPMDates.aspx` | [Search/SearchPMDates.cshtml](../src/Histo.Web/Pages/Search/SearchPMDates.cshtml) | ✅ | Built in Run #27. |
| 51 | `SearchSample.aspx` | [Search/SearchSample.cshtml](../src/Histo.Web/Pages/Search/SearchSample.cshtml) | ✅ | Built in Run #27; wired an "Add to batch" action in Batch D1. |
| 52 | `SearchSender.aspx` | [Search/SearchSender.cshtml](../src/Histo.Web/Pages/Search/SearchSender.cshtml) | ✅ | Built in Run #35. |
| 53 | `SearchSubmissions.aspx` | [Search/SearchSubmissions.cshtml](../src/Histo.Web/Pages/Search/SearchSubmissions.cshtml) | ✅ | Built in Run #35. |
| 54 | `SearchTest.aspx` | [Search/SearchTest.cshtml](../src/Histo.Web/Pages/Search/SearchTest.cshtml) | ✅ | Built in Run #35 — simplified to test-item counts only, not the full legacy cross-tab analytics engine (documented simplification). |
| 55 | `SearchUnUsedHistologyRefs.aspx` | [Search/SearchUnUsedHistologyRefs.cshtml](../src/Histo.Web/Pages/Search/SearchUnUsedHistologyRefs.cshtml) | ✅ | Built in Run #35. |
| 56 | `SubmissionDetails.aspx` | [Submissions/SubmissionDetails.cshtml](../src/Histo.Web/Pages/Submissions/SubmissionDetails.cshtml) | ✅ | Built in Batch C. |
| 57 | `SubmissionDetailsBlock.aspx` | [Submissions/SubmissionDetailsBlock.cshtml](../src/Histo.Web/Pages/Submissions/SubmissionDetailsBlock.cshtml) | ✅ | Built in Batch C. |
| 58 | `SubmissionForm.aspx` | — | ❌ | Confirmed in Batch C to be a pure Crystal Reports PDF-export popup (invoked from `FinalPrintBatch.aspx`), not a duplicate of `AddSubmission.cshtml` — reclassified as Phase 2 (Reporting) scope, not a Phase 5 UI gap. |
| 59 | `SubmissionNotes.aspx` | — | ❌ | Confirmed in Batch C to be a pure Crystal Reports PDF-export popup — same disposition as `SubmissionForm.aspx` above. |
| 60 | `SubmissionsOnHold.aspx` | [Batches/SubmissionsOnHold.cshtml](../src/Histo.Web/Pages/Batches/SubmissionsOnHold.cshtml) | ✅ | |
| 61 | `UserMaintenance.aspx` | [Admin/UserMaintenance.cshtml](../src/Histo.Web/Pages/Admin/UserMaintenance.cshtml) + [Admin/AddUser.cshtml](../src/Histo.Web/Pages/Admin/AddUser.cshtml) + [Admin/EditUser.cshtml](../src/Histo.Web/Pages/Admin/EditUser.cshtml) | ✅ | Full CRUD restored per ISS-017 |
| 62 | `ViewImportedData.aspx` | [Search/ViewImportedData.cshtml](../src/Histo.Web/Pages/Search/ViewImportedData.cshtml) | ✅ | Built in Batch F — standalone data view, confirmed not dependent on Crystal Reports. |
| 63 | `ViewSamples.aspx` | [Submissions/ViewSamples.cshtml](../src/Histo.Web/Pages/Submissions/ViewSamples.cshtml) | ✅ | |
| 64 | `ViewSubmissions.aspx` | [Submissions/ViewSubmissions.cshtml](../src/Histo.Web/Pages/Submissions/ViewSubmissions.cshtml) | ✅ | |

**Totals (as of 2026-08-03):** 60 ✅ fully migrated or functionally superseded · 1 ❌* not a real gap (`CalendarPopup`, superseded by native GDS date input) · 3 ❌ genuinely blocked, all on Phase 2 Reporting (`FinalPrintBatch`, `SubmissionForm`, `SubmissionNotes`). No pages remain in the ⚠️ partial state — ISS-018 (PickListMaintenance CRUD) and the SearchMenu dead-link gap (ISS-020) are both resolved.

*Historical totals (2026-08-01, pre-remediation): 30 ✅ fully migrated · 2 ⚠️ partial · 1 ❌* not a real gap · 31 ❌ genuinely missing.*

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
| `MouseNumber.ascx` | `Histo.Core.Domain.ValidationHelpers.ValidateMouseNumber` exists but is called from **zero** Razor Pages (confirmed via full-tree grep, 2026-08-03) | ⚠️ Backend logic ported but unwired — low severity, see F-08 |
| `SenderRef.ascx` | Functionally replaced by inline Sample/Sender Ref input fields on [Submissions/AddSample.cshtml](../src/Histo.Web/Pages/Submissions/AddSample.cshtml), [Search/SearchSender.cshtml](../src/Histo.Web/Pages/Search/SearchSender.cshtml), and [Admin/EditAnimalRef.cshtml](../src/Histo.Web/Pages/Admin/EditAnimalRef.cshtml) | ✅ Replaced (no direct 1:1 port needed — Search module and AddSample built in Runs #35/#37) |

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
| `Histo.Administration` | `ILookupRepository` | ✅ `CreateLookupItemAsync` (ISS-018) | ✅ `UpdateLookupItemAsync` (ISS-018) | n/a | ✅ Full (EditLookupItem, PickListUserArea) — **ISS-018 resolved** |
| `Histo.Submissions` | `IBatchRepository` | ✅ `AddAsync` | ✅ `UpdateAsync`, `UpdateStatusAsync` | n/a | ✅ Full (EditBatch/BatchDetails plus Copy-batch workflow, Batch D2) |
| `Histo.Submissions` | `ISubmissionRepository` | ✅ `AddSubmissionAsync`, `AddAnimalAsync`, `AddTissueAsync` | ✅ `UpdateSubmissionAsync`, `UpdateAnimalAsync`, `UpdateTissueAsync`, `UpdateAnimalSenderRefAsync`/`UpdateAnimalHistologyRefAsync` (ISS-022, 2026-08-03) | ✅ `DeleteAnimalAsync`, `DeleteTissueAsync` | ✅ Full (AddSubmission, SubmissionDetails/SubmissionDetailsBlock, ViewSamples edit/delete, EditAnimalRef) |
| `Histo.Histology` | `IBlockRepository` | ✅ Confirmed — `AddAsync` exists, used by CopyBlocks/CopySamples workflows | — | ✅ `DeleteAsync` | ✅ Full (BlockDetails, CopyBlocks, CopySamples) |
| `Histo.Histology` | `IHistologyRepository` | ✅ Confirmed | ✅ `UpdateRefAsync` | — | ✅ Full — two distinct pages: `Bookings/EditHistologyRef` (pool counter) and `Admin/EditAnimalRef` (per-animal rename, ISS-022) |
| `Histo.QualityControl` | `IQCNoteRepository` | ✅ `AddAsync` | ✅ `UpdateAsync` (rowstamp concurrency) | — | ✅ Full (AddQCNote, EditQCNote) |
| `Histo.AuditLog` | `IAuditLogRepository` | n/a (audit logs are append-only by design) | n/a | n/a | ✅ Read-only by design — not a gap |

**Conclusion (updated 2026-08-03):** Every repository-layer CRUD gap identified in the original audit (ISS-017, ISS-018) and every subsequent "backend exists, UI page missing" gap (ISS-020, ISS-021, ISS-022) has been resolved. There are no remaining CRUD-shaped gaps between the domain/repository layer and the UI layer.

---

## 6. Business Rules and Workflow Parity

| Rule / workflow | Legacy location | Current status |
|---|---|---|
| PG-number auto-reversal | `HistopathologyLib/clsAnimal.vb` | ✅ Ported to `AnimalHelpers` per Phase 4, confirmed via existing `SubmissionServiceAnimalTests` |
| QC Note rowstamp concurrency | `clsQCNote.vb` | ✅ Ported — `QCNoteConcurrencyException` implemented and unit-tested |
| Batch status rowstamp concurrency | `clsBatch.vb` | ✅ `UpdateStatusAsync(..., byte[] rowStamp, ...)` present in `IBatchRepository` |
| **Authorization (`CheckPermissions()`)** | Present in **every** legacy code-behind (confirmed ≥60 call sites across `ArchiveBlocks`, `ArchiveMenu`, `ArchiveTissues`, `AuditLogByDate/BySubmission/ByUser/Menu`, `BatchBlocks`, `BatchesForArchiving`, `BatchesForDispatch`, `BatchesForEditing`, and all remaining pages per ISS-004) | ✅ **Implemented (2026-08-28).** `HistoPageModel.OnPageHandlerExecutionAsync` enforces a two-gate model: Gate 1 (authentication) issues a SAML `ChallengeResult("saml2")` via `ITfoxtec.Identity.Saml2.MvcCore` for unauthenticated requests; Gate 2 (authorization) requires an active `tblUser` row, surfaced as the `AppClaimTypes.GroupName` claim, redirecting to `AccessDenied.cshtml` if absent. The temporary Development-only stub identity and the NTLogin bridge page (ADR-006, `Login.cshtml`) have both been removed. |
| Session-scoped user context (`SessionVars`) | `SessionVars.vb` | ✅ Replaced by `ISessionService`/`SessionService` — populated from Entra ID claims via `Session.PopulateFromClaims(User)` on first request after sign-in, and now gated by the authorization policy above |
| Windows Authentication → Entra ID | Web.config `<authentication mode="Windows">` | ✅ **Implemented (2026-08-28)** — Entra ID SAML 2.0, SP-initiated sign-in via `/Saml2/login` → `POST /Saml2/Acs` (`AuthController`) |

---

## 7. Consolidated Findings

| # | Finding | Severity | Status | Recommendation |
|---|---|---|---|---|
| F-01 | 34 of 64 legacy pages (53%) have no current equivalent, spanning Sample creation, Batch/Block copy workflows (7 pages), the entire Search module (8 of 9 pages), Submission detail/notes, Excel export, and admin data-fix utilities | **High** | New (quantifies existing ISS-004) | Prioritize a Phase 5 backlog ordered by business criticality: Search module and Submission Details are likely highest-traffic; Copy-workflow and admin fix-up pages are likely lowest. Re-plan against `docs/Migration-Plan.md` Batch 6+. |
| F-02 | Reporting module (9 Crystal Reports) at 0% — blocks `FinalPrintBatch`, `ViewImportedData`, and all Submission sub-reports | High | Already tracked (Phase 2 "Not Started") | No new action beyond existing plan; flag as a hard dependency for F-01's Submission/Print pages. |
| F-03 | `ILookupRepository` has no Create/Update for pick-list items; `PickListMaintenanceID.aspx` equivalent missing | Medium | **Resolved** (ISS-018, Batch E1, 2026-08-01) | `CreateLookupItemAsync`/`UpdateLookupItemAsync` added; `Admin/EditLookupItem.cshtml` and `Admin/PickListUserArea.cshtml` built. |
| F-04 | `SearchMenu.cshtml` is a live menu page linking to 8 search screens, none of which exist — a functional dead end for any user navigating to Search | High | **Resolved** (ISS-020, Runs #27/#35, 2026-08-01) | All 8 search screens built; menu links now all functional. |
| F-05 | No "Add QC Note" page exists — `IQCNoteRepository.AddAsync` is unreachable from the UI; only note-editing is exposed | Medium | **Resolved** (ISS-021, Batch A, 2026-08-01) | `QC/AddQCNote.cshtml` built, mirroring the `EditQCNote` pattern. |
| F-06 | `EditHistologyRef.aspx` has no current equivalent despite `IHistologyRepository.UpdateRefAsync` already existing | Medium | **Resolved** (ISS-021, Batch A, 2026-08-01) | `Bookings/EditHistologyRef.cshtml` built for the pool-counter workflow. **Note:** this uncovered a second, distinct gap — the true per-animal renamer — tracked separately as ISS-022 and also now resolved (see below). |
| F-07 | Authentication/authorization is entirely unimplemented in `Histo.Web` — only a Development-only stub identity exists; no `[Authorize]` policies replace any of the ~60+ legacy `CheckPermissions()` call sites | **Critical (pre-production blocker)** | **Resolved 2026-08-28** (ISS-001, D-004) | Entra ID SAML 2.0 implemented via `ITfoxtec.Identity.Saml2.MvcCore`; `HistoPageModel`'s two-gate model replaces all legacy `CheckPermissions()` call sites. NTLogin bridge (ADR-006) decommissioned. This was the only remaining critical-severity finding in this report — all findings F-01–F-10 are now resolved except F-02/F-10 (Reporting/secrets, tracked separately). |
| F-08 | `MouseNumber.ascx` and `SenderRef.ascx` have no confirmed current replacement | Low | **Partially resolved** — `SenderRef.ascx` confirmed replaced (Search module + AddSample built); `MouseNumber.ascx` confirmed as dead/unwired code, not a missing feature | `ValidateMouseNumber` exists in `Histo.Core` but is never called — low-severity cleanup item, see §11. |
| F-09 | `SubmissionForm.aspx` vs. `AddSubmission.cshtml` overlap is unconfirmed | Low | **Resolved** (Batch C, 2026-08-01) | Confirmed distinct — `SubmissionForm.aspx`/`SubmissionNotes.aspx` are pure Crystal Reports PDF-export popups, reclassified as Phase 2 (Reporting) scope, not a Phase 5 UI gap. |
| F-10 | Plaintext SQL credential and `debug="true"` in legacy `Web.config` | High | Open — **scope expanded 2026-08-03** | The identical plaintext credential (`HistologyUser`/`HistologyUser9245`) is now also committed in the **current, migrated app's** [src/Histo.Web/appsettings.json](../src/Histo.Web/appsettings.json) and `appsettings.Development.json`, not only the legacy `Web.config` — this is a more severe finding than originally scoped, since it is in the actively-shipped codebase. Remediate per `azure-infra.instructions.md` §3 (Managed Identity + Key Vault) before any Azure deployment. |

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

## 9. Cross-Reference to Existing Open Issues (updated 2026-08-28)

As of this update, ISS-001 (authentication, F-07), ISS-018, ISS-020, ISS-021, and ISS-022 (all raised by this audit or its remediation batches) are **Resolved**. The following issues remain **Open** and require action before further deployment: ISS-006 (Web.config secrets, scope-expanded to include `src/Histo.Web/appsettings*.json`), ISS-007 (`debug="true"`), ISS-009 (NT login→UPN mapping — superseded in practice by claims-based email/UPN resolution, verify no residual dependency), ISS-010 (key-person risk), ISS-011 (Azure admin/Entra ID dependency — satisfied for dev environment, confirm test/UAT/prod). ISS-004 (all 64 pages required before cutover) is now effectively satisfied for every non-reporting-blocked page — see §1/§2. See §11 for the consolidated list of everything still pending.

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

- ~~**F-07 / ISS-001 (Critical):** Authentication/authorization is still 0% implemented. No page — old or newly built in this remediation — has any `[Authorize]` gating. This remains the top-priority blocker for any non-development deployment.~~ **Resolved 2026-08-28** — see §12.
- ~~**ISS-022 (new, found during Batch A):** The true legacy `EditHistologyRef.aspx` per-animal Sender/Histology Ref renamer has no repository support at all (distinct from the pool-level counter update that was built). Open, medium severity.~~ **Resolved 2026-08-03** (Run Log #43) — see §11.
- **Phase 2 (Reporting):** Still 0% — all 9 Crystal Reports, plus the 3 print-popup pages, remain unmigrated.
- **F-08/F-09 verification items** from the original audit were resolved during remediation (SubmissionForm confirmed distinct/reporting-only; MouseNumber.ascx/SenderRef.ascx dependencies resolved as part of the AddSample/Search work).

---

## 11. Pending Migrations / Unresolved Parity Gaps (as of 2026-08-03)

This section consolidates every gap still open across the application, superseding the
now-resolved items in §7 (Consolidated Findings). Cross-referenced against
`docs/migration-run-journal.md` Open Issues as of the ISS-022 fix (Run Log #43).

| Gap | Area | Status | Severity | Tracking |
|---|---|---|---|---|
| ~~Authentication / Authorization (Entra ID)~~ | ~~`src/Histo.Web/Program.cs`~~ | **Resolved 2026-08-28** — Entra ID SAML 2.0 via `ITfoxtec.Identity.Saml2.MvcCore`; `HistoPageModel` two-gate model live; ADR-006 bridge decommissioned | ~~Critical~~ | ISS-001, F-07 (closed) |
| NT-login → UPN mapping | `Histo.Administration::UserService` | Verify no residual dependency — claims-based resolution (`Session.PopulateFromClaims`) is now the live path | Medium | ISS-009 |
| Azure Entra app registration / group IDs | Programme / Azure admin | Satisfied for dev environment (App ID + Federation Metadata URL received); confirm test/UAT/prod registrations | Medium | ISS-011 |
| Reporting Phase 2 (9 Crystal Reports) | `src/Histo.Reporting/` | 0% — empty project stub, no source files | High | Phase Tracker Phase 2 |
| `FinalPrintBatch.aspx`, `SubmissionForm.aspx`, `SubmissionNotes.aspx` | `src/Histo.Web/Pages/` | Blocked on Reporting Phase 2 — confirmed non-functional shells would add no value until then | High | Run #36, #42 |
| Plaintext SQL credential in **current app** config | `src/Histo.Web/appsettings.json`, `appsettings.Development.json` | Open — same credential as legacy `Web.config`, now also committed in the migrated codebase | High | Scope-expanded ISS-006 |
| Plaintext SQL credential in legacy config | `HistopathologySystem/Web.config` | Open | High | ISS-006 |
| `debug="true"` in legacy Web.config | `HistopathologySystem/Web.config` | Open | Medium | ISS-007 |
| Testing & Cutover (Phase 6) | Programme | Not Started — 90 unit tests exist; zero integration/E2E coverage of the 28 pages built in Batches A–F | Medium | Phase Tracker Phase 6 |
| `MouseNumber.ascx` validation logic unwired | `Histo.Core.Domain.ValidationHelpers.ValidateMouseNumber` | Confirmed dead code — method exists but is called from zero Razor Pages | Low | F-08 |
| Agent filename defect | `.github/agents/modernisation.agent .md` | Trivial, unresolved — trailing space prevents VS Code from loading it | Low | ISS-005 |
| Key-person risk | Programme | Open — Sr Dev 1 sole VB.NET business-rule knowledge holder | High | ISS-010 |

**Note:** All page-migration gaps from the original §2/§7 (F-01, F-03, F-04, F-05, F-06, F-09) are now
**Resolved** per Batches A–F and the 2026-08-03 ISS-022 fix — see §10 addendum and the corrected
§2 table. They are intentionally omitted from this table to avoid re-litigating closed items.

### Prioritized migration plan

**P0 — Critical, blocks any non-dev deployment**
1. ~~Authentication (Phase 1)~~ — **Resolved 2026-08-28.** Entra ID SAML 2.0 wired in `Program.cs`; `[Authorize]`-equivalent two-gate model enforced in `HistoPageModel` for all pages.
2. **Secrets hardening:** Replace the plaintext credential in `appsettings.json`/`appsettings.Development.json` and legacy `Web.config` with Managed Identity connection strings; move remaining secrets to Key Vault.
   - *Dependency:* Key Vault + Azure SQL provisioning (`azure-infra.instructions.md` §3).

**P1 — High, required before hard-switch cutover (ISS-004: no strangler-fig path)**
3. **Reporting (Phase 2):** Build the `ReportDefinition.json` pipeline for the 9 `.rpt` files; unblocks `FinalPrintBatch`/`SubmissionForm`/`SubmissionNotes`.
   - *Risk:* `HistologyReport.rpt` sub-report nesting needs manual ViewModel design (ISS-002); no Phase 0 baseline PDFs captured yet for RMSE validation.
4. **NT-login → UPN mapping (ISS-009):** Verify no residual dependency on the legacy NT-login format now that claims-based resolution is live.

**P2 — Medium, quality/cleanup, can run in parallel**
5. **Testing & Cutover (Phase 6):** Add integration/E2E (Playwright) coverage for the 28 pages built in Batches A–F before any environment cutover, given the hard-switch constraint. Include auth flow coverage (sign-in redirect, session claims population, sign-out) per `auth-aspnetcore.instructions.md`.
6. **Minor cleanup:** Remove or wire up `MouseNumber.ascx`/`ValidateMouseNumber` (F-08); rename `modernisation.agent .md` (ISS-005); confirm `IBlockRepository` Create/Update completeness is fully exercised by the Copy workflows.

**Suggested sequencing:** P0 item 2 (secrets hardening) is now the sole remaining P0 item — Managed Identity DB connection is documented but not yet wired to a live secret value (see `docs/Azure-ManagedIdentity-EntraID-WebJob.md` §2). P1 item 3
(Reporting) can proceed in parallel — no dependency on Auth. P1 item 4 should be verified opportunistically. P2 starts once P0/P1 are substantially underway, and must complete before
the hard-switch cutover per ISS-004.

---

## 12. Authentication Closure — Entra ID SAML 2.0 (2026-08-28)

This section records the closure of F-07/ISS-001, the last remaining **Critical** finding in this report.

| Item | Detail |
|---|---|
| Protocol implemented | SAML 2.0 (SP-initiated), **not** the OIDC/`Microsoft.Identity.Web` approach originally proposed in `docs/Migration-Plan.md` Phase 2 |
| Library | `ITfoxtec.Identity.Saml2.MvcCore` v4.20.1 |
| Authentication gate | `HistoPageModel.OnPageHandlerExecutionAsync` — issues `ChallengeResult("saml2")` for unauthenticated requests |
| Authorization gate | Requires an active `tblUser` row, surfaced as `AppClaimTypes.GroupName`; redirects to `AccessDenied.cshtml` if absent |
| Session population | `Session.PopulateFromClaims(User)` on first request after sign-in, from claims baked in at ACS time |
| SAML endpoints | `GET /Saml2/login` (challenge), `POST /Saml2/Acs` (assertion consumer), `GET /Saml2/Logout` (SLO) — all in `AuthController` |
| Superseded bridge | ADR-006 manual NTLogin page (`Login.cshtml`/`Login.cshtml.cs`) — **deleted** 2026-08-28; see `docs/ADR/ADR-006-manual-login-page-bridge.md` |
| Verification performed | Confirmed via static code inspection: `Program.cs` `AddSaml2`/`app.UseSaml2()` registration, `AuthController` ACS/logout actions, `HistoPageModel` two-gate model, zero remaining references to `Login.cshtml`/`LoginModel` in `src/` |
| Residual follow-ups | Managed Identity DB connection string not yet wired to a live Key Vault secret (tracked as P0 item 2 above); SP signing certificate required before test/UAT/prod (`Saml2:SPCertificateThumbprint`) |

**Conclusion:** F-07/ISS-001 is closed. Authentication/authorization is no longer a blocker for non-development deployment; the remaining pre-production blocker is P0 item 2 (secrets hardening / Managed Identity), not authentication.

---

## 13. Accepted Deviations from Screen Consolidation (2026-09-01)

Several legacy screens were deliberately merged during Phase 5 under the GDS-alignment work in
`docs/TSE-NonTSE-Submission-Workflow-Redesign.md`. The consolidations themselves are approved, but
they carry capability reductions that were previously recorded only in code comments. They are
registered here so the Phase 5 sign-off gate reflects them explicitly.

| # | Deviation | Legacy source | Current state | Severity | Disposition |
|---|---|---|---|---|---|
| D-1 | Inline histology-ref editing removed from the sample list | `BatchBlockSummary.aspx` grid `EditItemTemplate` (`txtHistologyRefEdit` + format validator) | Histology ref is editable only on the per-sample detail page | Low | **Resolved by design (2026-09-01).** Replicating a legacy editable-grid row is a GOV.UK Design System anti-pattern — no such component exists, and it would violate "one thing per page". `AddSubmission` now redirects straight into `SubmissionDetails`/`SubmissionDetailsBlock` after adding a sample (matching legacy's own `SV_AddSampleNextPage` redirect), and every row on `SampleSummary` already carries a one-click "Edit sample" link to a dedicated Histology reference field. Residual cost is N clicks instead of 0 only when bulk-correcting refs across many existing samples in one sitting — accepted as a minor efficiency gap, not a missing capability. |
| D-2 | Grid paging removed from the sample list | `BatchSummary.aspx` / `BatchBlockSummary.aspx` (`AllowPaging="True"` + `DataGridPager`) | Flat table renders all samples | Medium | **Planned separately** — tracked outside this register. |
| D-3 | Mouse-number range bulk entry not reproduced | `AddSample.aspx` / `AddSubmission.aspx` (`MouseNumber1`/`MouseNumber2` range → bulk `NewRecord`) | Samples added one Sender Ref at a time | Medium | Open — confirm whether Mouse Bioassay users still rely on this. |
| D-4 | Excel mouse-number upload not reproduced | `AddSample.aspx::btnUpload_Click` → `ImportMouseNumbers` (OLE DB / Jet, `MOUSE_NUMBERS` sheet) | No bulk import | Medium | Open. Note the legacy implementation depends on the Jet OLE DB provider, which is unavailable on Linux containers — any replacement needs a different reader. |
| D-5 | TSE / Non-TSE submission-type match check dropped from Copy samples | `CopySamples.aspx` | Not reproduced — the migrated `Batch` model carries no batch type | Medium | Open — requires a `BatchType` property before it can be restored. |
| D-6 | "Auto-generate histology ref" option dropped from Copy blocks | `CopyBlocks.aspx` (`cbAutoGenerateHisto`, PG-number reversal, neuropath range lookup) | Target samples keep their existing histology ref | Medium | Open — equivalent logic exists in `AnimalHelpers.ComputePgAutoHistologyRef`. |
| D-7 | Per-block test-type selection absent from block management | `SubmissionDetailsBlock.aspx` per-block checkboxes (EO, H&E, H&E BSE, IHC Prp, IHC Other, Special Stain) | Managed downstream via `QC/QualityData.cshtml` | Low | Open — needs confirmation that block creation populates `BlockTest` rows. |

**Route rename (2026-09-01):** `/Submissions/BatchBlockSummary` → `/Submissions/SampleSummary`, because
"Block" is meaningless for Wet Tissue submissions and GOV.UK treats URLs as user-facing content. A
permanent redirect from the old path is registered in `Program.cs`. The page `<h1>` was already
"Sample summary" and is unchanged.

---

*Generation date: 2026-08-01, updated 2026-08-03, updated 2026-08-28 (authentication closure), updated 2026-09-01 (consolidation deviations, §13). This report is a point-in-time audit based on static code/directory inspection — no live application testing was performed as part of this pass.*
