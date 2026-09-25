# Session Metrics

> Extracted from `migration-run-journal.md` on 2026-08-21.
> Durations from 2026-08-19 onwards are derived from VS Code session store timestamps.
> All previously blank `—` duration cells filled 2026-08-21 with journal-stated or complexity-based estimates.
> Rows 53–70 added 2026-08-21 retrospectively — agent runs missing from original capture; durations are complexity-based estimates.

---

## Phase status snapshot (verified 2026-09-24)

> Cross-checked against actual repo source, not re-estimated. See `docs/PHASE_COMPLETION_REPORT.md` for the full breakdown and sources.

| Phase | Status | Evidence rows in this table |
|---|---|---|
| 1 — Authentication (Entra ID SAML 2.0) | **Complete** | Rows 71–72 (`Identity-migration`), row 73 (`gds-ui` — AccessDenied/ServiceProblem), row 77 (Live Dev SAML sign-in chain debugging, 2026-08-28) |
| 2 — Reporting (Crystal Reports â†’ QuestPDF) | **Complete** â€” all 9 reports confirmed (HistologyReport + HistologySubReport, QCNote, SubmissionNotes + its 5 embedded sub-reports rendered as `SubmissionNotesRenderer.cs` tables) | Row 137 (HistologyReport/SubmissionForm parity rebuild), row 138 (QCNoteRenderer fix), row 144 (QCNoteRenderer PR-review fix); 5-sub-report mapping verified 2026-09-24 directly against `SubmissionNotesDataSetBuilder.cs`/`SubmissionNotesRenderer.cs` |
| 3 — Platform Migration (VBâ†’C#/.NET 10) | **Complete** â€” zero `.vb` files remain under `src/`; `HistopathologySystem.vbproj` not referenced in the active `.slnx` | Row 34 (`modernise-to-modular-monolith`), Run #68 in `migration-run-journal.md` (9 `IService` interfaces + 5 DI modules); verified 2026-09-24 by grep across `src/` |
| 5 — UI Migration | **Complete** (63/64 pages per `Parity-Audit-Report.md`) | Rows 8–70 and surrounding UI-fix rows throughout this table |
| 6 — Testing & Cutover | **In progress** — no dedicated Playwright/E2E project confirmed; xUnit suite large and growing | Ongoing `dotnet test` counts cited throughout this table |

---

## Agent Run Timing Table

| # | Date | Agent | Start Time | End Time | Duration | Notes |
|---|------|-------|------------|----------|----------|-------|
| 1 | _(yyyy-MM-dd)_ | _(agent name)_ | _(HH:mm)_ | _(HH:mm)_ | — | — |
| 2 | 2026-07-27 | `documentation` | 10:56 | 11:39 | **~43 min** | File timestamps: HLD.md created 10:56, all docs last-modified 11:39 |
| 3 | 2026-07-27 | `modernise-to-modular-monolith` | 12:05 | 12:14 | **~9 min** | File timestamps: Target-Architecture.md created 12:05, modified 12:14 |
| 4 | 2026-07-27 | `testing` | 12:29 | 12:44 | **~15 min** | File timestamps: Test-Strategy.md created 12:29, modified 12:44 |
| 5 | 2026-07-27 | `intelligent-migration` | 12:49 | 12:55 | **~6 min** | File timestamps: Intelligent-Migration-Plan.md 12:49, ROI-and-Budget.md last-modified 12:55 |
| 6 | 2026-07-27 | `implementation` | 14:35 | 14:38 | **~10 min** | File timestamps: slnx + Infrastructure created 14:35–14:36, test files created 14:37–14:38 |
| 8 | 2026-07-30 | `ui-implementation` | 15:08 | 15:25 | **~17 min** | Resumed from Session 1. Completed all remaining pages and DI wiring. Build: 0 errors. Tests: 88 pass. |
| 9 | 2026-07-30 | `ui-implementation` | 15:40 | 15:43 | **~3 min** | Hot-fix: `IAppLogger` not registered in DI. |
| 10 | 2026-07-30 | `ui-implementation` | 17:44 | 17:49 | **~5 min** | Hot-fix: Home page showing no modules (ISS-013). |
| 11 | 2026-07-30 | `ui-implementation` | 17:50 | 18:05 | **~15 min** | Investigation: header/footer/styling not migrated (ISS-014). Analysis only. |
| 12 | 2026-07-30 | `ui-implementation` | 18:10 | 18:16 | **~6 min** | Applied ISS-014 fix: Bootstrap CDN, VLA theme CSS, logo asset. Build: 0 errors. |
| 13 | 2026-07-31 | `gds-ui` | 14:41 | 14:53 | **~12 min** | GDS compliance pass: govuk-frontend v6.2.0 assets, Layout/NavPartial/15 pages rewritten. Build: 0 errors. |
| 14 | 2026-07-31 | `gds-ui` | 15:09 | 15:13 | **~4 min** | Hot-fix (ISS-016): `href` + `asp-page` conflict on Layout service nav link. |
| 15 | 2026-07-31 | `gds-ui` | 15:32 | 16:00 | **~28 min** | ISS-015 resolved (10 pages) + ISS-017 User Management CRUD added. Build: 0 errors. |
| 16 | 2026-08-01 | `implementation` | — | — | **~45 min** | Full parity audit (documentation only). |
| 17 | 2026-07-31 | `ui-implementation` | — | — | **~35 min** | ISS-020 partial: Search module first 4 pages. |
| 18 | 2026-08-03 | `ui-implementation` | — | — | **~30 min** | ISS-022 resolved: EditAnimalRef per-animal Sender/Histology Ref rename. |
| 19 | 2026-08-04 | `GitHub Copilot` | ~14:00 | ~14:45 | **~45 min** | Run #53 — cross-module gap audit, raised ISS-025 through ISS-029. |
| 20 | 2026-08-04 | `ui-implementation` | 15:19 | 15:25 | **~6 min** | Run #54 — ISS-025 batch list columns/Quick-Go. |
| 21 | 2026-08-04 | `ui-implementation` | 15:54 | 15:58 | **~4 min** | Run #55 — ISS-026 ViewSubmissions full filter form. |
| 22 | 2026-08-04 | `ui-implementation` | 15:59 | 16:03 | **~4 min** | Run #56 — ISS-027/028/029 Audit Log, QCNotes, UserMaintenance. |
| 23 | 2026-08-04 | `ui-implementation` | 17:46 | 17:49 | **~3 min** | Run #57 — ISS-030 ViewSubmissions dropdowns + Clear Search. |
| 24 | 2026-08-05 | `GitHub Copilot` | — | — | **~25 min** | Run #58 — ISS-031/032/033 PickList/EditUser Dapper mappers. |
| 25 | 2026-08-05 | `GitHub Copilot` | — | — | **~20 min** | Run #59 — ISS-034 regression: RuntimeBinderException in ExpandoObject mappers. |
| 26 | 2026-08-05 | `GitHub Copilot` | — | — | **~10 min** | Run #60 — ISS-035 UserMaintenance ShowActiveOnly inverted default. |
| 27 | 2026-08-05 | `GitHub Copilot` | — | — | **~35 min** | Run #61 — ISS-036/037/041 ViewSubmissions dropdowns, EditBatch Status, home nav. |
| 28 | 2026-08-05 | `GitHub Copilot` | — | — | **~30 min** | Run #62 — ISS-038/039/040 EditQualityDataTest Code-keyed dropdowns, BatchDetails fields, BatchesForArchiving. |
| 29 | 2026-08-06 | `GitHub Copilot` | — | — | **~15 min** | Run #63 — Booking/Archive/nav label parity fixes. |
| 30 | 2026-08-06 | `GitHub Copilot` | — | — | **~45 min** | Run #64 — Search/navigation audit, raised ISS-042–045. No code changes. |
| 31 | 2026-08-06 | `GitHub Copilot` | 16:47 | 16:57 | **~10 min** | Run #65 — ISS-042/043/044/045 all resolved (15 files). |
| 32 | 2026-08-07 | `GitHub Copilot` | — | — | **~25 min** | Run #66 — ADR-006 manual login bridge, Login.cshtml created. |
| 33 | 2026-08-07 | `GitHub Copilot` | — | — | **~40 min** | Run #67 — Help Pages investigation + GDS implementation. |
| 34 | 2026-08-10 | `GitHub Copilot` | 12:52 | 13:00 | **~8 min** | Run #68 — Modular monolith IService interfaces (9 interfaces, 5 DI modules). |
| 35 | 2026-08-12 | `gds-ui` | 18:02 | 18:04 | **~2 min** | Run #69 — GDS user context moved into service nav header. |
| 36 | 2026-08-14 | `GitHub Copilot` | — | — | **~15 min** | Run #70 — NullableDateTimeTypeHandler for dd/MM/yyyy SP date columns. |
| 37 | 2026-08-14 | `GitHub Copilot` | — | — | **~5 min** | Run #71 — BatchSearchResult.SubmittedBy int?→string?. |
| 38 | 2026-08-07 | `GitHub Copilot` | — | — | **~35 min** | Run #72 — SearchSubmissions row-select action panel. |
| 39 | 2026-08-14 | `GitHub Copilot` | 15:17 | 15:37 | **~20 min** | Run #73 — ViewSubmissions row-select action panel. |
| 40 | 2026-08-14 | `GitHub Copilot` | 15:38 | 15:55 | **~15 min** | Run #74 — 3 root causes: antiforgery token, duplicate SelectedBatchId, CopyBatch route param. |
| 41 | 2026-08-14 | `GitHub Copilot` | 16:00 | 16:40 | **~40 min** | Run #75 — BatchDetails 4-gap fix: CustomerReceivedDate, DateReturned page, ReturnPage session, gated buttons. |
| 42 | 2026-08-14 | `GitHub Copilot` | 17:00 | 17:50 | **~50 min** | Run #76 — GAP-5: batch-level histology/test type selections completely missing. |
| 43 | 2026-08-17 | `GitHub Copilot` | — | — | **~10 min** | Run #77 — BatchDetails duplicate block removed, title + button label fixes. |
| 44 | 2026-08-17 | `GitHub Copilot` | — | — | **~15 min** | Run #78 — BatchDetails Species/Status display + ghost disabled buttons removed. |
| 45 | 2026-08-17 | `GitHub Copilot` | — | — | **~20 min** | Run #79 — GAP-6: 6 missing BatchDetails fields (Entered By/Area, Submitted By/Area, Submitted As, SafeToHandle). |
| 46 | 2026-08-17 | `GitHub Copilot` | — | — | **~25 min** | Run #80 — ViewSubmissions buttons all disabled after Select; BatchBlockSummary route fix. |
| 47 | 2026-08-18 | `GitHub Copilot` | — | — | **~35 min** | Run #81 — ViewSamples/BatchBlockSummary page separation, real ViewSamples.cshtml built. |
| 48 | 2026-08-18 | `GitHub Copilot` | — | — | **~20 min** | Run #82 — BatchBlockSummary gating, Copy Sample link, "Not started" label. |
| 49 | 2026-08-18 | `GitHub Copilot` | — | — | **~45 min** | Run #83 — Multi-bug fix 6 issues (AuditLog, UserMaintenance, EditUser, Picklist, EditAnimalRef, BatchBlockSummary). |
| 50 | 2026-08-18 | `GitHub Copilot` | — | — | **~25 min** | Run #84 — Picklist 3-fix session (Code validation, @Original_Code, too many arguments). |
| 51 | 2026-08-19 | `GitHub Copilot` | 11:21 | 17:00 | **~339 min (5h 39m)** | Run #85 — Full CopyBatch + BatchBlockSummary multi-fix session. See sub-task breakdown below. |
| 52 | 2026-08-21 | `GitHub Copilot` | 08:34 | 12:12 | **~218 min (3h 38m)** | Run #86 — UI fixes: BatchBlockSummary tissue/view-mode, EditLookupItem Area column, CopyBatch picker flow. See sub-task breakdown below. |
| 53 | 2026-07-29 | `implementation` | — | — | **~90 min** | Run #25 — Phase 4: all 5 domain + repository modules (Administration, AuditLog, QC, Histology, Submissions), 10 new unit tests; 88 tests pass. |
| 54 | 2026-08-01 | `ui-implementation` | — | — | **~30 min** | Run #35 — ISS-020 remaining 4 search pages: SearchSender, SearchSubmissions, SearchTest, SearchUnUsedHistologyRefs. |
| 55 | 2026-08-01 | `ui-implementation` | — | — | **~25 min** | Run #36 — Batch C: SubmissionDetails + SubmissionDetailsBlock pages. |
| 56 | 2026-08-01 | `ui-implementation` | — | — | **~25 min** | Run #37 — Batch D1: AddSample + Cassetted pages + dead-link fixes in SearchSample and Index. |
| 57 | 2026-08-01 | `ui-implementation` | — | — | **~50 min** | Run #38 — Batch D2: Copy workflow family (7 legacy pages → CopyBatch, CopyBatchSummary, CopyBlocks, CopySamples, CopySamplesSummary). |
| 58 | 2026-08-01 | `ui-implementation` | — | — | **~40 min** | Run #39 — Batch E1: EditLookupItem + PickListUserArea + CreateLookupItemAsync/UpdateLookupItemAsync full stack (ISS-018). |
| 59 | 2026-08-01 | `ui-implementation` | — | — | **~20 min** | Run #40 — Batch E2: BatchBlocks/BatchSummary confirmed superseded; ViewSamples Edit/Delete navigation gap closed. |
| 60 | 2026-08-01 | `ui-implementation` | — | — | **~35 min** | Run #41 — Batch E3: QualityData + EditQualityDataTest + FixCompletedDates; CS0663 BatchService hot-fix applied. |
| 61 | 2026-08-01 | `ui-implementation` | — | — | **~30 min** | Run #42 — Batch F: CsvExportHelper + ViewImportedData + CSV export wired to 4 pages; FinalPrintBatch deferred (Phase 2). |
| 62 | 2026-08-03 | `ui-implementation` | — | — | **~25 min** | Run #44 — ISS-023 BatchType full stack (TSE/NonTSE radio, session, SP param) + ISS-024 nav link fix. |
| 63 | 2026-08-03 | `gds-ui` | — | — | **~35 min** | Run #45 — GDS compliance: Search module 10 pages + BeforeHeading section slot added to _Layout.cshtml. |
| 64 | 2026-08-03 | `gds-ui` | — | — | **~25 min** | Run #46 — GDS compliance: Bookings, Archive, Audit Log modules (12 pages; ArchiveMenu route fix included). |
| 65 | 2026-08-03 | `gds-ui` | — | — | **~25 min** | Run #47 — Functionality Traceability Matrix created (docs/Functionality-Traceability-Matrix.md, 22-row legacy mapping). |
| 66 | 2026-08-03 | `gds-ui` | — | — | **~50 min** | Run #48 — GDS compliance: all remaining 34 pages across Batches, Submissions, QC, Admin, Blocks modules. |
| 67 | 2026-08-03 | `gds-ui` | — | — | **~15 min** | Run #49 — Home page + nav label reverts: "Receive submissions", "Booking", "Pick list maintenance"; 3 new links explained. |
| 68 | 2026-08-03 | `gds-ui` | — | — | **~10 min** | Run #50 — Search menu label reverted to "Search outputs"; FixCompletedDates legacy provenance confirmed (URL-only). |
| 69 | 2026-08-03 | `gds-ui` | — | — | **~5 min** | Run #51 — Remove FixCompletedDates from Index.cshtml Administration panel (preserves legacy URL-only behaviour). |
| 70 | 2026-08-03 | `gds-ui` | — | — | **~5 min** | Run #52 — Remove "Batches received" from Index.cshtml Laboratory panel and _NavPartial.cshtml. |
| 71 | 2026-08-24 | `Identity-migration` | — | — | **40 min** | Run Entra id integration agents |
| 72 | 2026-08-24 | `Identity-migration` | — | — | **10 min** | SAML config validation |
| 73 | 2026-08-24 | `gds-ui` | — | — | **15 min** | Access denied and There is a problem with the service pages implmentation - An unhandled exception occurred while processing the request. |
| 74 | 2026-08-27 | `GitHub Copilot` | — | — | **~330 min (5h 30m)** | Run #87 — TSE/NON-TSE submission workflow GDS redesign (docs/TSE-NonTSE-Submission-Workflow-Redesign.md) + route-based state/access-guard rollout (Phase 0–2) + BatchAccessDecision unit tests + per-block tissue/pre-booked-ref/bulk-block additions + New Submission Create/Edit flow fixes (AddSubmission Sender Ref search + Sample Blocks redirect + BatchSubmissionID resilience, Cassetted submission-type default fix, BatchDetails native date input) + BatchDetails button-visibility/journey gating (IsViewMode/CanPrint) and redundant task-list cleanup. Duration is a complexity-based estimate — no exact start/end timestamps captured. See sub-task breakdown below. |
| 75 | 2026-08-27 | `GitHub Copilot` | 22:47 | 23:50 | **~63 min (1h 03m)** | Run #88 — Submission scenario gating (Add/Edit/Copy sample per journey), `BlockDetails`→`SubmissionDetailsBlock` consolidation, `BatchBlockSummary` animal-list merge fix, and two `AddAnimal` stored-procedure bugs fixed (`PMDate` DBNull `dbType`, `AddAnimal` too-many-arguments parameter mismatch vs real SP). Duration per user-reported start/end time. |
| 76 | 2026-08-28 | `GitHub Copilot` | — | — | **~25 min** | Run #89 — Submission-journey navigation verification (all 4 journeys' back/cancel targets confirmed correct) + GDS button-alignment fixes across 4 files (`EditBatch` Samples link into button-group, `SubmissionDetailsBlock` missing `data-module`, `AddSubmission` Add sample/Cancel/Check-historical-data button-groups, `BatchBlockSummary` Add sample/Done button-groups). Duration is a complexity-based estimate. Build: 0 errors. |
| 77 | 2026-09-01 | `GitHub Copilot` | — | — | **~120 min (2h 00m)** | Run #90 — ReceiveBatch legacy-parity rebuild (status choice, date/time/received-by, post-fixation checkboxes, rejection reason, repeat-blocks indicator, read-only view mode) + `Batch`/`BatchRepository`/`BatchService` post-fixation extensions + `develop` merge into feature branch (58 commits ahead, 8 conflicting files resolved) + post-merge type-mismatch fix (`SubmittedArea`/`OtherSubmittedArea` int?→string?). Duration is a complexity-based estimate. Build: 0 errors. Tests: 144 passed, 1 skipped. |
| 78 | 2026-09-02 | `GitHub Copilot` | — | — | **~45 min** | Run #91 — `LogError` argument-order build error fix; ReceiveBatch "Received" radio missing `<label>`/`aria-controls` typo fix (reverted and reapplied per user request); Copilot PR review comment addressed (`ReceiveBatchModel.OnPostSaveAsync` dead error-handling branch — `UpdateAsync` rethrows rather than returning `false`; fixed with `try/catch` + corrected doc comments); local dev-only Entra ID auth bypass investigated and fixed across several iterations (uncommitted, not part of this branch's merge). Build: 0 errors. Tests: 144 passed, 1 skipped. |
| 77 | 2026-08-28 | `GitHub Copilot` | 14:01 | 16:27 | **~146 min (2h 26m)** | Run #90 — Live Dev deployment debugging of the Entra ID SAML 2.0 sign-in chain: (1) added `UseForwardedHeaders()`; (2) diagnosed the fix wasn't deployed (feature branch unmerged), merged via PR; (3) added `PostConfigure<CookieAuthenticationOptions>("saml2", ...)` forcing a relative redirect on `OnRedirectToLogin`/`OnRedirectToAccessDenied` after the proxy still didn't yield the correct `Request.Host`; (4) fixed `IDX10214: Audience validation failed` by populating `saml2Config.AllowedAudienceUris`; also fixed `_Layout.cshtml` nav/user-context chrome rendering for anonymous requests. Confirmed via Log stream: SAML assertion now validates successfully end-to-end. Remaining blocker (infra, not code): `HistologyDb` SQL connection unreachable in Dev. Build: 0 errors. |
| 78 | 2026-09-01 | `GitHub Copilot` | 12:15 | 19:07 | **~60 min** | Run #91 — Dev-only auth bypass in `HistoPageModel` (hardcoded principal, `DevAuthBypass` config flag, remove `SignInAsync`, make bypass unconditional to fix empty-session panel bug); submission-type routing analysis across all 5 types (Wet Tissue/Pre-Cassetted/Wax Block/Stained/Unstained) for Create/Edit/View/Copy journeys: fixed `OnPostSelect` to route Wet Tissue to `SubmissionDetails`; added `BatchId` route to `SubmissionDetails` back-links; added view-mode guards to `SubmissionDetails`; restored Edit sample visibility in View mode. Build: 0 errors. Duration estimated from session timestamps. |
| 79 | 2026-09-01 | `GitHub Copilot` | — | — | **~170 min (complexity-based estimate)** | Run #92 — Full TSE/Non-TSE parity audit (Create/View/Edit/Copy across all 5 submission types) surfacing and fixing 2 defects (Wet Tissue Edit-sample POST routing, `SearchSubmissions` Edit-gating inversion); logged 7 accepted-consolidation deviations D-1–D-7 in `Parity-Audit-Report.md` §13; Module-to-Page Mapping nav audit restoring 2 orphaned pages (`SubmissionsOnHold`, `Bookings/EditHistologyRef`) and refreshing the mapping table in `Migration-Plan.md`; GDS route rename `BatchBlockSummary`→`SampleSummary` (301 redirect + `BatchNo` caption for submission-type visibility); D-1 disposition downgraded to "Resolved by design" (GDS anti-pattern rationale); Add-sample post-submit redirect regression fixed (`AddSubmission` now routes straight into `SubmissionDetailsBlock`/`SubmissionDetails` per legacy `SV_AddSampleNextPage` behaviour, closing an extra-click gap on every sample add); diagnosed and resolved a stale-build false-negative during user retest (`dotnet test` does not rebuild `Histo.Web`; a locked prior `.exe` was serving pre-fix code). Build: 0 errors, 0 warnings. Tests: 144 pass, 1 skipped. |
| 74 | 2026-09-02 | `gds-ui` | — | — | **2 min** | Access denied shows the navigation and context that has fixed |
| 80 | 2026-09-02 | `GitHub Copilot` | — | 17:20 | **~90 min (complexity-based estimate)** | Run #19 — Create / Edit / View Submission workflow review vs legacy + Pick List Management integration. Single-commit session (`91deb3f` at 17:20:49), so start time is not derivable from git; estimate based on scope (6 legacy pages analysed via 2 parallel Explore subagents, 12 files changed across 6 Razor Pages). Build 0 errors; `dotnet test` 145 passed / 0 failed. Nine parity issues raised (ISS-R20 … ISS-R28) |
| 81 | 2026-09-02 | `GitHub Copilot` | — | 19:26 | **~20 min (complexity-based estimate)** | Run #20 — Wet Tissue Edit sample silent-redirect bug. User supplied a browser Network tab trace proving `GET /Submissions/SubmissionDetails` itself 302'd instead of rendering. Root cause: `SubmissionDetailsModel.LoadAnimalAsync` only checked the plain animal table and silently redirected to `SampleSummary` on a miss — same defect class already fixed in `SubmissionDetailsBlockModel` but never ported to this twin page. Fixed via block-animal-table merge + removed the silent redirect (view's dormant "Sample not found" branch now reachable) + null guards on 3 handlers. Commit `b5daeb2`. Build 0 errors, 3 pre-existing warnings; `dotnet test` 145 passed / 0 failed, 1 skipped. |
---

| 118 | 2026-09-10 | `GitHub Copilot` | — | — | **~55 min (complexity-based estimate)** | Run #57 — New `Histo.WebJobs` project: annual histology-reset TimerTrigger WebJob, 4 build-error fixes (missing WebJobs.Extensions package, OutputType=Exe, SqlClient version conflict, Logging:LogLevel not binding), Application Insights via `Microsoft.ApplicationInsights.WorkerService` (2 wrong-package attempts before landing on the correct API), and full config alignment with `Histo.Web`'s real conventions (top-level `APPLICATIONINSIGHTS_CONNECTION_STRING`, Managed-Identity connection string, removed per-environment appsettings file loading). Build 0 errors, 1 pre-existing NU1603 warning. |
| 119 | 2026-09-11 | `GitHub Copilot` | — | — | **~10 min (complexity-based estimate)** | Run #58 — Header navigation GDS review (Q&A only). Confirmed `_Layout.cshtml`/`_NavPartial.cshtml` correctly use the official GOV.UK Service Navigation component; flagged missing `aria-current="page"` on the active link as a follow-up. Explained the Menu toggle is stock `govuk-frontend` JS behaviour, no gap. No code changes. |
| 120 | 2026-09-11 | `GitHub Copilot` | — | — | **~40 min (complexity-based estimate)** | Run #59 — SearchArchiveLocation grid presentation iterated per user sample data (grouped/rowspan), then reverted to a fully flat table per GDS's own caution against merged cells — both implementations round-tripped in the same session. Restored the Tissue code dropdown (tissue lookup, table 9) after an external edit had reverted it to free text, and reordered Archive location to sit directly after Sender ref. Build 0 errors; `dotnet test` 281 total, 280 passed, 1 skipped. |
| 121 | 2026-09-11 | `GitHub Copilot` | — | — | **~45 min (complexity-based estimate)** | Run #60 — SearchArchiveLocation Slide archive "no records" root-caused via a live DB test (proving the SP itself works) to an empty-string-vs-NULL bug in 5 filter parameters passed straight from posted form fields; fixed with a `NullIfEmpty` helper. A first retest false-negative was traced to a stale running `Histo.Web` process serving a pre-fix build — killed and rebuilt. Build 0 errors; `dotnet test` 281 total, 280 passed, 1 skipped. |
| 122 | 2026-09-11 | `GitHub Copilot` | — | — | **~30 min (complexity-based estimate)** | Run #61 — SearchArchiveLocation + SearchBlockRefs "not both" validation relaxed to "at least one" after confirming via `sp_helptext` that the underlying stored procedures tolerate both Sender ref and Histology ref being supplied (Sender ref takes precedence). `SearchBlockRefs.SearchAsync` now explicitly prefers the Sender-ref-only stored procedure when both are present. Rewrote 1 unit test to match the new precedence behaviour. Build 0 errors; `dotnet test` 281 total, 280 passed, 1 skipped. |
| 123 | 2026-09-11 | `GitHub Copilot` | — | — | **~15 min (complexity-based estimate)** | Run #62 — SearchBlockRefs blank-Search-click validation message fix. Root-caused to the GET form producing an identical query-string-less URL for "blank submit" and "first visit", making them indistinguishable server-side; added a hidden `Submitted` marker field to tell them apart, plus 1 new regression test. Build 0 errors; `dotnet test` 282 total, 281 passed, 1 skipped. |
| 124 | 2026-09-12 | `GitHub Copilot` | — | — | **~40 min (complexity-based estimate)** | Run #63 — SearchArchiveLocation Slide archive full legacy-parity fix. User asked whether the migrated implementation aligned with legacy `ProcessSelectedArchiveSearch`/`GetAnimalSlideArchiveInformation`; confirmed via live `sp_helptext` against LocalDB that legacy fans out across 3 stored procedures (`GetAnimalStainArchiveInformation` + `GetAnimalBatches`/per-batch `GetAnimalAntibodiesArchiveInformation` + `GetAnimalHistologyArchiveInformation`), while the migrated `BlockRepository.GetSlideArchiveAsync` only called the first — a documented but unescalated "SIMPLIFIED" shortcut from the original migration run. Rewrote to reproduce the full merge. Build 0 errors. |
| 125 | 2026-09-12 | `GitHub Copilot` | — | — | **~25 min (complexity-based estimate)** | Run #64 — ViewSamples "not both" validation relaxed to "at least one" (same class of fix as SearchArchiveLocation/SearchBlockRefs, confirmed via `sp_helptext` that both `GetAnimalBatchTissues`/`GetAnimalBlockTissues` tolerate either/both refs) + `NullIfEmpty` applied to 4 filter parameters to fix the empty-string-vs-NULL SP filter bug. 3 new unit tests. Build 0 errors. |
| 126 | 2026-09-12 | `GitHub Copilot` | — | — | **~10 min (complexity-based estimate)** | Run #65 — SearchSubmissions "no results on initial load" + GDS search pattern Q&A. Confirmed the empty grid on first visit is deliberate (matches every sibling Search page and legacy's own `Page_Load`) and is the GDS-recommended pattern, not a defect. No code changes. |
| 127 | 2026-09-12 | `GitHub Copilot` | — | — | **~50 min (complexity-based estimate)** | Run #66 — Button spacing GDS remediation across 14 files. Audited every unwrapped `govuk-button--secondary` occurrence app-wide and wrapped genuine bare-adjacent-button cases in `govuk-button-group`, matching the existing `BlockDetails.cshtml` convention; deliberately left 2 structurally-different cases unwrapped (`CopySamples.cshtml`, `QualityData.cshtml`) and flagged them to the user. Build 0 errors; `dotnet test` 285 total, 284 passed, 1 skipped (after stopping a stray locked `Histo.Web` process blocking the build). |
| 128 | 2026-09-13 | `GitHub Copilot` | — | — | **~50 min (complexity-based estimate)** | Run #68 — Local dev `DevAuthBypass` 400-error root cause. First pass fixed 6 pages' `asp-page-handler`-on-button-not-form antiforgery bug; user correctly rejected `[IgnoreAntiforgeryToken]` as a security shortcut, leading to the real root cause: `DevAuthBypass` set `HttpContext.User` in a page filter, which runs *after* the antiforgery authorization filter — moved the bypass identity assignment into real middleware in `Program.cs` so the GET (token issue) and POST (token validation) see the same identity. Dev-only; production SAML flow unaffected. Build 0 errors. |
| 129 | 2026-09-13 | `GitHub Copilot` | — | — | **~60 min (complexity-based estimate)** | Run #69 — Wet Tissue/Block CRUD parity fixes: hint text on `AddSubmission`/`SearchSender`; `SearchSender` converted to `GridPageModel` + always-search on load; Delete Tissue silent-failure fix (bogus `@UserID` param on `DeleteTissueAsync`); `BlockDetails` Add-block now provisions the block immediately (matching legacy's in-memory-dataset behaviour) instead of hiding Tissues/Tests until first save; 2 more bugs found verifying both Add/Edit-sample journeys end-to-end — `UpdateAnimalAsync`/`EditAnimal` extra unsupported parameters and a `PMDate` string→datetime type-conversion bug that silently failed the whole UPDATE; `SubmissionDetails` Histology Reference field removed entirely (confirmed never shown on this Wet-Tissue-only page in legacy) with the existing value now carried through unchanged on PM-date-only saves. Build 0 errors. |
| 130 | 2026-09-14 | `GitHub Copilot` | — | — | **~30 min (complexity-based estimate)** | Run #70 — `SampleSummary` Finish button called a nonexistent `EditBatchStatus` SP; fixed to `EditBatchStatus_temp`, then — after the user questioned its legacy provenance — corrected again once confirmed as an orphaned DB artifact with zero legacy references, reproducing legacy's real Done-button logic (`IsBlocked`/status/`AllTissuesAssigned`) instead. `ViewSubmissions` default-sort fallback fixed to newest-first (`OrderByDescending(r => r.ID)`), matching legacy's `ID DESC` default. Build 0 errors. |
| 131 | 2026-09-14 | `GitHub Copilot` | — | — | **~35 min (complexity-based estimate)** | Run #71 — `BlockDetails` pre-cassetted Add-block dead-end (zero pre-booked refs) fixed with a clear error + "Book a block reference" link instead of a silent blank form; `BatchDetails` Create/Edit Project/Pathologist dropdowns empty root-caused to passing the area *name* instead of the numeric area code to `GetProjectsByAreaAsync`/`GetContactsByAreaAsync`; `ArchiveBlocks`/`ArchiveTissues` Back/Done return-page bug fixed via an explicit `ReturnPage` query parameter (was unconditionally overwriting the session return page). Build 0 errors. |
| 132 | 2026-09-15 | `GitHub Copilot` | — | — | **~25 min (complexity-based estimate)** | Run #72 — `EditBatch` dead "Pre-cassetted" checkbox removal exposed and fixed a real data-corruption bug: the always-`false`-bound property was silently resetting `IsPreCassetted` on every save. `AddUser`/`EditUser` invalid GOV.UK width classes (`govuk-input--width-40/70`, not real GDS classes) replaced with compliant fluid-width markup. Build 0 errors. |
| 133 | 2026-09-15 | `GitHub Copilot` | — | — | **~40 min (complexity-based estimate)** | Run #73 — `SafeToHandle` fixed from an unrepresentable 2-state checkbox to a real 3-state GDS radio (Yes/No/unanswered) on both `BatchDetails`/`EditBatch`, closing ISS-R27; confirmed the "at least one sample" Finish gate already existed and fixed the actual gap (post-create landing-page UX) by redirecting straight to Sample Summary, addressing ISS-R22; fixed a `Cassetted.cshtml` error-summary link pointing at a nonexistent element id; fixed `SampleSummary`'s Add sample/Finish button misalignment (a wrapping `<form>` was the flex child instead of the button). Build 0 errors; `dotnet test` 286/287 passed, 1 pre-existing skip. |
| 134 | 2026-09-16 | `GitHub Copilot` | — | — | **~45 min (complexity-based estimate)** | Run #74 — Mouse Bioassay/Neuropath removal impact validated against `Batch`/`SubmitterArea`/`luHistologyRefType` records and GxP/GDS compliance (`docs/Mouse-Bioassay-Neuropath-Removal-ChangeReport.md` §6–13); found and fixed a real regression risk — `EditUser` could silently reassign a user already on a now-retired area since the dropdown only listed active areas — via `EnsureCurrentAreaVisibleAsync` + Area-change-gated whitelist validation. Fixed 6 stale test-mock signatures. Build 0 errors; `dotnet test` 264 passed, 1 skipped. |
| 135 | 2026-09-17 | `GitHub Copilot` | — | — | **~90 min (complexity-based estimate, iterative)** | Run #75 — `src/Database/` SQL folder reorganized through several naming iterations (final: `Deploy/`, `Migrations/`, `StoredProcedures/`), fixing a `.gitignore` `[Rr]elease/` collision that was hiding `Deploy.sql` from git along the way. Added a database-deployment step (`CopyFiles@2` + `AzureCLI@2`/`sqlcmd`) to `.azure-devops/pipeline.yaml`; diagnosed a DB login failure to the pipeline's service principal never being provisioned as a database user (flagged as ISS-R29, a DBA action). Fixed 4 further pipeline SQL errors from live run logs: Windows-backslash `:r` includes on Linux agents, `Msg 5074` (dependent index), `Msg 3723` (constraint-backed index), `Msg 1934` (`ANSI_NULLS`/`QUOTED_IDENTIFIER` session state) — plus a new `IX_User_Email` index. Build 0 errors (SQL not compiled). |
| 136 | 2026-09-18 | `GitHub Copilot` | — | — | **~50 min (complexity-based estimate)** | Run #76 — Compared the Mouse Bioassay/Neuropath removal against its business requirement and found it partially aligned: widened `GroupAreaMappingHelpers`' overly strict 1:1 Group/Area whitelist to permit legacy's real cross-area combinations; fixed `LookupItems`/`EditLookupItem` to show/preserve retired-area names and assignments (same `EnsureCurrentAreaVisibleAsync` pattern as `EditUser`); root-caused `SearchSubmissions`' "Submitted area" filter being a silent no-op for *every* value (not just Mouse Bioassay/Neuropath) to binding the dropdown's option value to `LookupItem.Code`, which `GetUserAreasAsync`'s mapper never populates — fixed to bind `@a.ID` instead. Build 0 errors; `dotnet test` 267 passed, 1 skipped. |
| 137 | 2026-09-21 | `GitHub Copilot` | — | — | **~240 min (complexity-based estimate, iterative)** | Run #67 — HistologyReport (Submission Form) PDF legacy Crystal Reports parity rebuild + configurable pagination. Iterative QuestPDF layout/data fidelity work against the legacy reference image using DB-verified batch-10243 data: `HistologyReportDataSetBuilder` Code-keyed lookups (Fixation/TimeReceived/SubmittedAs tables 10/3/11), animal-count-derived Total Samples, and a full `BuildHistologyTable` rewrite replicating legacy `CreateBatchTestTable` (special-stain/antibody expansion, 8-row cap); `HistologyReportRenderer` fixes (blue-text removal, fixed-height black title bars for NON-TSE alignment, checkbox corner-glitch, fixed-height clamped Comments box, footer A/B/C/D panel layout); `SubmissionNotesRenderer` heading/rule fixes; and configurable `Reporting:HistologyReportRowsPerPage` (default 13, ≤0 guarded) wired through the renderer param, page-model `IConfiguration`, and `appsettings.json`. Cannot rasterise PDFs locally (no pdftoppm/ImageMagick) — fidelity confirmed via user screenshots of the generated test PDF. Build 0 errors, 15 pre-existing warnings; `dotnet test` HistologyReport filter 34 passed. |
| 138 | 2026-09-21 | `GitHub Copilot` | — | — | **~15 min (complexity-based estimate)** | Run #77 — QCNoteRenderer duplicate heading fix. Verified via live DB and the legacy reference image that `QCText` already contains the full "Sender Ref/Histo Ref/Block Ref/Test" header plus its data row as one space-padded plain-text blob — the renderer's own separate static header table was duplicating it. Removed the redundant header; `QCText` now renders as a single monospaced block. Build 0 errors; `dotnet test` 13 QC Note tests pass. |
| 139 | 2026-09-21 | `GitHub Copilot` | — | — | **~180 min (complexity-based estimate)** | Run #78 — Export to Excel: pre-implementation legacy-column analysis across all 12 export pages (via the real `ExcelExport.aspx.vb` + each page's own export-table code), then full ClosedXML/.xlsx implementation. Found and fixed the major gap: `SearchSubmissions`/`ViewSubmissions` legacy export is a dedicated 16-column table, not the 6–9 on-screen grid columns — added the 6 missing `BatchSearchResult` fields and rebuilt both exports to match exactly. New `ExcelExportHelper` (typed cells) replaces `CsvExportHelper`; all 12 handlers converted; added `ExcelDataReader`-based validation tests. Build 0 errors; `dotnet test` 280 passed, 1 skipped. |
| 140 | 2026-09-21 | `GitHub Copilot` | — | — | **~40 min (complexity-based estimate)** | Run #79 — Audit Log (By Date/Submission/User) "no data" root-caused via live DB verification to `AuditLogEntry.ID` being declared `int` while the SP's `[AuditLog].[ID]` column is genuinely heterogeneous (varchar codes for lookup-table edits, numeric strings otherwise) — any non-numeric row threw a swallowed `FormatException`, emptying the whole result set. Fixed by changing `ID` to `string?` (never displayed/used elsewhere). Build 0 errors; `dotnet test` AuditLog filter 4/4 pass. |
| 141 | 2026-09-21 | `GitHub Copilot` | — | — | **~35 min (complexity-based estimate)** | Run #80 — `EditUser.cshtml` was missing its Active checkbox entirely (silently deactivating every saved user); added the same GDS checkbox block used on `AddUser.cshtml`. Investigated "Pick List Projects Area not saving" and confirmed via `sp_helptext` + the untouched legacy VB source that `EditluProjects` has never accepted `@Area` on update, in legacy or now; made the Area field read-only with an explanatory hint rather than leave a misleading editable control. Build 0 errors; `dotnet test` 5/5 pass. |
| 142 | 2026-09-22 | `GitHub Copilot` | — | — | **~30 min (complexity-based estimate)** | Run #81 — Investigated a claim that the Neuropath/Mouse Bioassay deactivation broke Area-saving on the legacy Projects pick-list page; confirmed the migration only touches `luUserArea` and is unrelated. Found 5 per-area `EditluProjectsX` stored procedures that looked like a plausible mechanism but reference tables that no longer exist anywhere in the database and are never called by any legacy code — pre-existing orphaned artifacts from before a schema consolidation, not a regression. Reverted the prior read-only Area fix back to editable per the user's request to defer the decision. |
| 143 | 2026-09-22 | `GitHub Copilot` | — | — | **~15 min (complexity-based estimate)** | Run #82 — Renamed the `ExportCsv` handler to `ExportExcel` across all 12 export pages (razor + code-behind) plus 1 test, now that every export produces a real `.xlsx`. Naming-only, no behaviour change. Build 0 errors; `dotnet test` 280 passed, 1 skipped. |
| 144 | 2026-09-22 | `GitHub Copilot` | — | — | **~20 min (complexity-based estimate)** | Run #83 — QCNoteRenderer PR-review fix: Run #77's header-removal risked omitting required headings for genuinely free-text saved notes (`QCNoteDataSetBuilder` copies `QCText` verbatim; only the Edit-screen's blank-note default text embeds the header). Added a normalized `TestSummary` field built from live SP row data, independent of `QCText`; renderer now always shows both. Accepted trade-off: unedited-save notes can still duplicate the table. Build 0 errors; `dotnet test` 13 QC Note tests pass. |
| 145 | 2026-09-23 | `GitHub Copilot` | — | — | **~30 min (complexity-based estimate)** | Run #84 — Search-screen two-column GDS layout rollout (8 pages) + QCNotes "QC Number is mandatory" validation + full SearchTest legacy-parity rebuild. Live sqlcmd verification found 5 of 6 legacy count SPs missing from the DB entirely; reproduced the counting logic via new raw-SQL repository methods against `BlockHistology`/`BlockAntibodies`/`BlockStain` + their TCCodes junction tables (no migration/SP changes needed). Added Histology/Antibodies/Special Stain checkboxes, pivoted "Analyse results" cross-tab, "Analyse submissions" drill-down, 2 Excel exports, restored "Outputs menu" button. 3 new unit tests. Build 0 errors; `dotnet test` 284 total, 283 passed, 1 pre-existing skip. |
| 146 | 2026-09-23 | `GitHub Copilot` | — | — | **~40 min (complexity-based estimate)** | Run #85 — Browser Back button losing search results on 4 of 8 Search-module pages, root-caused to POST-based forms (browser history only caches GET). Converted `ViewSamples`/`SearchTest`/`SearchPMDates`/`SearchArchiveLocation` to the already-established GET-based pattern (`SupportsGet` bind properties, `Submitted` marker, `_SortableHeader`/`_Pagination` partials, GET export links) — matching `SearchSubmissions`/`SearchBlockRefs` which were already correct. Fixed 14 pre-existing stale test compile errors in `SearchValidationTests.cs` found as a side effect. Build 0 errors; `dotnet test` 284 total, 283 passed, 1 pre-existing skip. |
| 147 | 2026-09-23 | `GitHub Copilot` | — | — | **~35 min (complexity-based estimate)** | Run #93 — Reapplied Run #85's GET-based Search-module conversion after a teammate's `git pull` reverted all 4 pages plus 2 dependent test files back to their pre-fix POST-based state. Restored the exact content from a repo-memory snapshot taken before the revert; found and fixed a leftover duplicate `PageSize`/`SortColumn`/`SortDesc`/`PageNumber` block in `SearchPMDatesModel` hiding `GridPageModel`'s own members (CS0108) once the base class was restored. Build 0 errors; `dotnet test` 287 total, 286 passed, 1 skipped. |
| 148 | 2026-09-23 | `GitHub Copilot` | — | — | **~25 min (complexity-based estimate)** | Run #94 — `SampleSummary` "Unknown submission type" caption root-caused to `GetLookupDataAsync(11)` (LOOKUP_SUBMITTEDAS) missing `includeInactive: true` — code 6 "Fresh Frozen" is inactive, so any batch submitted under it (or any other since-deactivated type) silently failed to resolve. Fixed `SampleSummaryModel`/`AddSubmissionModel`. Confirmed via Q&A this is unrelated to the Mouse Bioassay/Neuropath removal (different lookup table, predates that change). Build 0 errors; `dotnet test` 287 total, 286 passed, 1 skipped. |
| 149 | 2026-09-23 | `GitHub Copilot` | — | — | **~40 min (complexity-based estimate)** | Run #95 — 4-part UI fix: removed a non-legacy-parity PM Date field from `Blocks/BlockDetails.cshtml` (confirmed absent from legacy's real `.aspx` markup); grouped "Next block"/"Done" buttons onto one line; converted `SubmissionDetailsBlock.cshtml`'s header to a two-column layout for Sender ref/PM date (Histology reference left as-is); fixed `CopyBlocks`' Back/Cancel/success redirects always landing on the batch-wide `BatchBlocks` page instead of the originating per-animal `SubmissionDetailsBlock` page (added `AnimalId` route param + `BackLinkPage`). Build 0 errors; `dotnet test` 287 total, 286 passed, 1 skipped. |
| 150 | 2026-09-23 | `GitHub Copilot` | — | — | **~35 min (complexity-based estimate)** | Run #96 — `BatchBlocks.cshtml` missing 8 legacy header fields (Entered/Submitted by/area, Project, Pathologist, Species, Submission date) restored via the existing shared `BatchSummaryDisplayResolver`, in a two-column layout matching legacy's shared `Batch.ascx` control. Investigated "Copy Selected Blocks" button provenance — confirmed via real legacy source it does NOT exist on `BatchBlocks.aspx`; the real owner is `SubmissionDetailsBlock.aspx`'s "Copy To Samples", already correctly reproduced elsewhere. Concluded inadvertent carryover from the 2026-09-07 page split, flagged for user decision rather than removed. Build 0 errors; `dotnet test` 287 total, 286 passed, 1 skipped. |
| 151 | 2026-09-23 | `GitHub Copilot` | — | — | **~30 min (complexity-based estimate)** | Run #97 — `ViewSamples` Excel export column set/order fixed to match legacy exactly (added missing `HistologyRef`/`SenderRef` columns, moved `SubmittedAs` to the end) for both Block and Tissue Information modes. Ported legacy `ViewSamples.aspx.vb`'s `lblOtherFieldValue` label (DB-resolved counterpart ref) as `OtherFieldLabel`. Build 0 errors; `dotnet test` 287 total, 286 passed, 1 skipped. |
| 152 | 2026-09-23 | `GitHub Copilot` | — | — | **~25 min (complexity-based estimate)** | Run #98 — `SearchBlockRefs` pre-booked block ref quoting fixed. An existing code comment claimed legacy quotes every range/ref; reading the real `SearchBlockRefs.aspx.vb::FormatString` proved this false — only a genuine multi-ref range is quoted, single refs are not. Fixed `BlockRefRangeHelpers.FormatRange` to match and added its first-ever unit test file (6 tests). Build 0 errors; `dotnet test` 293 total, 292 passed, 1 skipped. |
| 153 | 2026-09-24 | `GitHub Copilot` | — | — | **~15 min (complexity-based estimate)** | Run #99 — `SqlException: 'Procedure or function DeleteBlock has too many arguments specified.'` fixed. Confirmed via `sys.parameters` the real SP accepts only `@ID`; `BlockRepository.DeleteAsync` was also passing `@UserID`. Removed the extra parameter; C# method signature kept unchanged for interface consistency. Build 0 errors; `dotnet test` 293 total, 292 passed, 1 skipped. |
| 154 | 2026-09-24 | `GitHub Copilot` | — | — | **~30 min (complexity-based estimate)** | Run #100 — "Assign Tissue to BatchBlocks" journey-scoped PM Date/Histology Ref editability fix. Added `SubmissionDetailsBlockModel.IsAssignTissueMode` (derived from the `Session.SampleDetailReturnPage` breadcrumb) so PM Date/Histology Ref are always editable in the Assign Tissue journey while the Create/Edit/View Submission journeys sharing the same page keep their existing locked-once-set behaviour unchanged. Fixed a Scenario-1 gap in `AddSubmissionModel` (hardcoded return path caused new Assign-Tissue samples to land in locked mode) via a new `ReturnPage`/`BackLinkPage`. Restored a previously dead histology-ref-type dropdown for Assign Tissue mode. Added `SubmissionDetailsBlockModelTests.cs` (3 new tests, first-ever coverage for this model). Build 0 errors; `dotnet test` 296 total, 295 passed, 1 skipped (up from 293/292). |
| 155 | 2026-09-25 | `GitHub Copilot` | — | — | **~120 min (complexity-based estimate, multi-part session)** | Run #101 — Multi-bug fix session, 9 root causes: (1) `EditUser`/`AddUser` SET-options (`ANSI_NULLS`/`QUOTED_IDENTIFIER`) FK/index bug fixed via a new self-contained recompile migration; (2) Application Insights exception visibility fixed by having `AppLogger<T>` call `TelemetryClient.TrackException` directly rather than relying solely on the Serilog→AI sink's `TelemetryConverter.Traces`; (3) Search Archive Location 4-part fix (missing Slide Archive `No pieces` column, Block Archive location dropdown, Block Archive `Archive comment` export, mislabelled "Slide"→"Description" column); (4) removed the Group/Area whitelist validation from `AddUser`/`EditUser` per explicit confirmation legacy has no such restriction; (5) `DevAuthBypass` hardcoded `UserDbId` root-caused as the source of an `FK_AuditLog_User` violation, made configurable and resolved via the real `IUserService`; (6)–(7) `ViewSamples` reciprocal Sender/Histology ref not appearing in its own input box, root-caused to the `asp-for` tag helper preferring stale `ModelState` over the updated model value, fixed via `ModelState.Remove`; (8) `ViewSubmissions` Species filter silent no-op, fixed to post the species ID (matching an equivalent fix already applied to `SearchSubmissions` in an earlier session); (9) button-group wrapping investigated and confirmed as intentional GOV.UK Design System behaviour, not a bug. Build 0 errors throughout; `dotnet test` 296 total, 295 passed, 1 skipped (unchanged). |

## Run #87 sub-task breakdown (2026-08-27)

| # | Duration | Area | Summary |
|---|----------|------|---------|
| 1 | **~40 min** | Docs — TSE/NON-TSE workflow redesign | Analysed legacy Block Details/Sample Blocks/Search Block Refs/View old ICC_Sub Data screens vs current Razor Pages; produced `docs/TSE-NonTSE-Submission-Workflow-Redesign.md` with pain points, GDS-aligned target journey, and Mermaid diagram |
| 2 | **~50 min** | `SubmissionDetailsBlock`, `BatchBlockSummary`, `Blocks/BlockDetails` | Consolidated block management onto `SubmissionDetailsBlock`; replaced browser `confirm()` with inline GOV.UK confirmation panels; replaced auto-submitting checkbox with explicit Apply button; added inline "check used block refs" lookup |
| 3 | **~45 min** | `HistoPageModel`, `BatchAccessDecision` (`Histo.Core.Domain`) | Phase 0 — added `CheckBatchAccessAsync` object-level access guard; extracted pure `BatchAccessDecision.IsAllowed` for unit testing without a Razor Pages harness |
| 4 | **~40 min** | `BatchBlockSummary`, `SubmissionDetailsBlock` | Phase 1 — route/query-based `BatchId`/`AnimalId` with session fallback, threaded through all links/forms between the two pages |
| 5 | **~35 min** | `BatchDetails`, `Blocks/BlockDetails`, `CopyBlocks`, `CopySamples(Summary)` | Phase 2 — extended route-based state + access guard to the rest of the submission wizard |
| 6 | **~15 min** | `BatchAccessDecisionTests.cs` | Added unit tests (Histo user bypass, area match/mismatch, batch-not-found pass-through); all pass |
| 7 | **~30 min** | `SubmissionDetailsBlock` | Added per-block tissue assignment (add/delete), pre-booked block ref dropdown + mandatory histology ref for pre-cassetted submissions, bulk "number of blocks" creation; added link to existing QC test-management page rather than guessing an unverified stored procedure for per-block test creation |
| 8 | **~25 min** | `AddSubmission`, `BatchBlockSummary` | Restored Sender Ref search/select via the existing `SearchSender` picker; redirect after adding a sample now goes to Sample Blocks (`SubmissionDetailsBlock`/`SubmissionDetails`) instead of back to the sample list; resolved/created `BatchSubmissionID` instead of silently redirecting to Home when missing |
| 9 | **~15 min** | `Cassetted.cshtml.cs` | Fixed Submission Type dropdown defaulting to a previous selection via stale `TempData` instead of "Select submission type" |
| 10 | **~15 min** | `BatchDetails.cshtml(.cs)` | Submission date changed from free-text input to native `type="date"` component |
| 11 | **~40 min** | `BatchDetails.cshtml(.cs)` | Investigated and fixed button visibility across Create/View Submission journeys (`IsViewMode`/`CanPrint`); removed redundant QC notes button and duplicate Samples task-list row/link |

---

## Run #85 Sub-task Breakdown (2026-08-19)

| Sub | Start (UTC) | End (UTC) | Duration | Description |
|-----|-------------|-----------|----------|-------------|
| 85-01 | 11:21 | 11:57 | **~36 min** | AuditLog validation messages — `AuditLogBySubmission` + `AuditLogByUser` GDS error-summary + field errors |
| 85-02 | 11:57 | 12:02 | **~5 min** | BatchBlockSummary SenderRef/HistologyRef — initial investigation, `QueryAsync<dynamic>` + `MapAnimal` approach (later reverted) |
| 85-03 | 12:02 | 12:45 | **~43 min** | Animal model `init`→`set` + `QueryAsync<Animal>` strongly-typed revert |
| 85-04 | 12:45 | 13:10 | **~25 min** | `GetBlockAnimalsByBatchAsync` — BATCH_BLOCK_ANIMAL (result-set 5 of GetBatchBlocksByID) root cause and fix |
| 85-05 | 13:10 | 13:22 | **~12 min** | ByPassSort full stack + CopyBatch data not loading + CustomerRef removal |
| 85-06 | 13:22 | 13:23 | **~1 min** | Legacy filename question (CopyBatch vs CopyBatchBlocks) |
| 85-07 | 13:23 | 13:43 | **~20 min** | CopyBatch missing legacy buttons (Change/Summary/Cancel/Finish) |
| 85-08 | 13:43 | 14:09 | **~25 min** | CopyBatch Scenario 1 & Scenario 2 (cassetted/non-cassetted) |
| 85-09 | 14:09 | 14:24 | **~15 min** | Tissue details `None` (Scenario 2) + Scenario 1 still empty |
| 85-10 | 14:24 | 14:43 | **~19 min** | Tissue column not showing at all (IsCassetted flag incorrectly hiding column) |
| 85-11 | 14:43 | 14:58 | **~15 min** | Tissue still empty — `GetBatchSubmissionDetailsByBatchID` has 3 result sets (not 9); skip index 7→1 fix |
| 85-12 | 14:58 | 15:55 | **~57 min** | Tissue empty confirmed — `GetSubmissionsByBatchAsync` skip 6→0 fix; `BatchSubmission` init→set |
| 85-13 | 15:55 | 16:26 | **~31 min** | Tissue loading moved outside `if/else` so both scenarios populate `TissueDetails` |
| 85-14 | 16:26 | 16:43 | **~17 min** | Tissues working; Change button missing + CopyBatch still showing old data |
| 85-15 | 16:43 | 16:50 | **~6 min** | CopyBatch `IsPreCassetted` → data-driven `IsCassetted` flag fix |
| 85-16 | 16:50 | 16:55 | **~5 min** | Change button visible (per-row anchor focusing NewSenderRef input) |
| 85-17 | 16:55 | 17:00 | **~5 min** | SearchSender complexity discussion (advisory only — no code) |


---

## Run #86 Sub-task Breakdown (2026-08-21)

| Sub | Start (UTC) | End (UTC) | Duration | Description |
|-----|-------------|-----------|----------|-------------|
| 86-01 | 08:34 | 09:11 | **~37 min** | BatchBlockSummary Tissue Details + EditLookupItem Area column (tables 18/19) + PickListMaintenance User Area filter |
| 86-02 | 09:11 | 09:13 | **~2 min** | Help link `target="_blank"` (GDS standard) + AuditLogDapperSetup CS8603 null-forgiving operators |
| 86-03 | 10:44 | 10:52 | **~8 min** | BatchBlockSummary tissue `@foreach`→`@for` Razor parse fix |
| 86-04 | 10:52 | 10:54 | **~3 min** | Tissue still not showing — `firstSubmId` fallback in `ResolveTissues` (mirrors CopyBatch) |
| 86-05 | 10:54 | 11:30 | **~36 min** | Remove bullet points from tissue lists; additional tissue debugging |
| 86-06 | 11:30 | 11:46 | **~16 min** | CopyBatch SearchSender picker flow — `OnPostPick` + TempData + `OnPostSelect` reusable pattern |
| 86-07 | 11:46 | 12:12 | **~26 min** | BatchBlockSummary `IsViewMode` read-only (mirrors legacy `SV_ViewSubmission`) |

---

## Duration Timer Script

Run this **before** starting an agent to capture the start time. Re-run after the agent finishes to print elapsed minutes:

```powershell
# Run BEFORE the agent
$agentStart = Get-Date
Write-Host "Timer started at: $($agentStart.ToString('HH:mm:ss'))"

# Run AFTER the agent finishes
$agentEnd = Get-Date
$elapsed  = [math]::Round(($agentEnd - $agentStart).TotalMinutes, 1)
Write-Host "Agent finished at : $($agentEnd.ToString('HH:mm:ss'))"
Write-Host "Elapsed duration  : $elapsed minutes"
```

> **Tip:** Keep a PowerShell terminal open for the duration of each agent run. `$agentStart` persists in the session until the terminal is closed.

---

## Session 2026-08-25 � Entra ID Auth + Quality Data Parity

| Item | Value |
|---|---|
| Date | 2026-08-25 |
| Start time | 10:59 |
| End time | 17:45 |
| Duration | **4h 00m** (240 min — measured from session log timestamps) |
| Agent | GitHub Copilot (Chat) |
| Build result | 0 errors, 0 warnings |

### Work completed

| Area | Change |
|---|---|
| Auth � SAML scheme | Fixed ChallengeResult("Cookies") ? ChallengeResult("saml2") in HistoPageModel.cs |
| Auth � dev config | Populated ppsettings.Development.json with real Entra ID tenant/app IDs |
| Auth � AccessDenied | Replaced inline HTML + Windows messaging with GDS layout + Entra ID email claim |
| Auth � ServiceProblem | Created GDS service-problem page; fixed Program.cs error middleware |
| Auth � Security groups | Analysis: Option A (Enterprise App assignment) recommended; portal steps provided |
| QC � BatchesForDispatch | Column names corrected to legacy labels; Completed Date + Customer Ref removed |
| QC � QualityData batch summary | 10 legacy fields added (Project, Pathologist, Entered/Submitted By/Area, dates) |
| QC � QualityData grid | On Hold column added; Failed/Passed columns separated; filters wired |
| QC � EditQualityDataTest | Not Tested radio; QC Note Ref link; JS conditionals; missing validations; Charges UI |
| QC � TC Codes full stack | TcCode model + BlockTestRepository reads result sets 7-9 + SaveTCCodesAsync delta || QC — Quick-Go bug | BatchesForDispatch Quick-Go rejected valid batches; fixed validation against dispatch list |
| QC — GDS JS cleanup | EditQualityDataTest custom JS replaced with govuk-radios/checkboxes conditional reveal |
| QC — Div structure fix | EditQualityDataTest stray div + misaligned dispatched conditional nesting fixed |
| QC — Test name display | QualityData grid Test column resolved from lookup codes to display names |
| QC — QC Note Ref | EditQualityDataTest always shows QC Note Ref (None/link) regardless of state |
| QC — GDS row colours | Row CSS tinting removed; govuk-tag in Result column is sole status indicator |
### Outstanding items

- Entra admin: add localhost Reply URL + create security group + set Assignment required
- Verify TC code SPs exist in DB (sys.procedures query)
- Fix EditUser.cshtml antiforgery form attribute (ISS-R05)
- Add dedicated 404 page (ISS-R03)
- Pagination for QualityData and EditQualityDataTest (deferred)

---

## Session 2026-08-26 — Navigation, BatchesForEditing, and QC Note Parity

| Item | Value |
|---|---|
| Date | 2026-08-26 |
| Start time | — |
| End time | — |
| Duration | **~90 min** (complexity-based estimate — sum of 5 sub-tasks below) |
| Agent | GitHub Copilot (Chat) |
| Build result | 0 errors (Histo.QualityControl); solution build had only pre-existing file-lock copy errors from a running dev process |

### Work completed

| Sub | Duration | Area | Change |
|---|---|---|---|
| 1 | **~25 min** | Nav / BatchesForEditing / EditBatch (ISS-R13) | Removed duplicate `Quality data` nav link (replaced with `Edit QC notes` → `/QC/QCNotes`); fixed `BatchesForEditingModel.OnPostSelect` to redirect to `/Batches/EditBatch` (was `/Batches/BatchDetails`, matching legacy `grdBatchesForEditing_SelectedIndexChanged`); corrected `BatchesForEditing.cshtml` grid headers/columns to match legacy `BatchesForEditing.aspx` exactly (removed `Received date`/`Customer ref`, renamed headers) |
| 2 | **~15 min** | QC — EditQCNote (ISS-R14) | Restored legacy QC Note Ref summary box (QC note ref, Submission number, Project, Species, Stain ref) and Created by/Date created footer, missing entirely from the migrated page; added `CreatedBy`/`DateCreated` to `QCNote` model and repository mapping |
| 3 | **~10 min** | QC — QCNoteRepository (ISS-R15) | Fixed regression from Sub-task 2 — `RuntimeBinderException` on dynamic access to possibly-absent SP columns silently swallowed by service catch, making the QCNotes Edit button appear non-functional; replaced with safe `IDictionary<string, object>` + `TryGetValue` mapping |
| 4 | **~25 min** | QC — QCNoteRepository (ISS-R16) | Notes missing entirely for antibody-test QC notes — added the missing `GetQCNoteAntibodiesInformation` SP call (legacy calls both SPs and combines result sets); added `BuildDefaultNoteText` to reproduce legacy's padded Sender Ref/Histo Ref/Block Ref/Test table shown when a note has no saved text yet |
| 5 | **~15 min** | QC — QCNoteRepository (ISS-R17) | Created-by date missing next to username — `DateCreated` arrives as a `dd/MM/yyyy` string but dynamic/IDictionary reads bypass Dapper's `NullableDateTimeTypeHandler`; added a `ParseDate` helper mirroring that handler's logic |

### Outstanding items

- None raised this session — all 5 issues found were resolved in-session.
---

## Session 2026-08-26 (afternoon) � Submission creation flow + Edit Submission + Pick List fixes

| Item | Value |
|---|---|
| Date | 2026-08-26 |
| Start time | 15:23 |
| End time | 16:53 |
| Duration | **90 min** (measured from session log timestamps) |
| Agent | GitHub Copilot (Chat) |
| Build result | 0 errors, 0 warnings |
| Turns | 198 |

### Work completed (13 fixes)

| Area | Fix |
|---|---|
| Cassetted / BatchDetails | Submission creation flow restored to two-step legacy pattern |
| BatchDetails create mode | Histology/Antibody/Stain checkboxes added to create form |
| BatchDetails view | "Batch not found. }" text rendering bug fixed (unbalanced @if blocks) |
| Cassetted state | Previous selections restored on back-navigation via Session + TempData |
| Cassetted SubmittedAs | Default to blank fixed (replaced asp-for with name on select) |
| SearchSubmissions | CanEditSubmission conditions were inverted � fixed to Submitted/Rejected |
| BatchesForEditing | Session.ReturnPage set in OnPostSelect |
| EditBatch | Back link / Cancel / save now context-aware via ReturnPage |
| EditLookupItem | Area column shows name not numeric code for tables 18/19 |
| EditLookupItem | Area dropdown added to Add/Edit form for tables 18/19 |
| LookupRepository | AddluContacts @ID parameter now supplied on insert |
| QualityData | Test name resolution fixed to use GetHistologyTypesAsync() |

### Outstanding items

- None raised this session � all 13 issues resolved in-session.
---

## Session 2026-09-02 — Create / Edit / View Submission review + Pick List Management integration

| Metric | Value |
|---|---|
| Date | 2026-09-02 |
| Duration | **~90 min** (complexity-based estimate — single-commit session, start time not derivable from git) |
| Agent | GitHub Copilot (Chat) |
| Mode | A (committed) — commit `91deb3f` |
| Build result | 0 errors, 4 pre-existing warnings |
| Test result | 145 passed, 0 failed |
| Files changed | 12 (6 Razor Pages + page models) |
| Subagents used | 2 × `Explore` (parallel legacy analysis) |

### Work completed

| Area | Change |
|---|---|
| BatchDetails (create mode) | Three "Manage …" pick-list buttons added beside Submitted by, Project / contract code and Pathologist |
| BatchDetails page model | `OnPostManagePickList` + `CreateDraft` save/restore so no entered data is lost on the detour |
| EditBatch | Same three buttons + `EditDraft` save/restore across all editable header, status and comment fields |
| PickListUserArea | `ReturnUrl` support, "Return to submission" button, guidance inset, hard-coded `Done` dead-end removed |
| UserMaintenance | `ReturnUrl` support, "Return to submission" button, `returnUrl` on Add user / Change links |
| AddUser / EditUser | `ReturnUrl` passthrough on form and redirects |
| Security | All return URLs validated with `Url.IsLocalUrl` (open-redirect prevention) |

### Review coverage (no change required)

Navigation Home → Cassetted → BatchDetails create → SampleSummary → AddSubmission → SubmissionDetails / SubmissionDetailsBlock; mandatory-field set vs `ValidateMandatoryFields()`; histology / antibody / stain conditional rules vs `CheckHistology()`; TSE / Non-TSE option filtering vs `HideOptions()`; Sample Summary Add / Edit / Delete / Copy / Done and Bypass sort; Wet Tissue vs block routing; `CanModifySamples` status gating.

### Outstanding items

Nine issues raised — ISS-R20 … ISS-R28 in `run-log-v2.md`:

| ID | Severity | Summary |
|---|---|---|
| ISS-R20 | High | Copy sample inverted — pre-fills Sender Ref, copies no tissues or blocks |
| ISS-R21 | Medium | Submission-date rule reversed (`max=today` vs legacy "today or later") |
| ISS-R22 | High | "At least one Block/Sample" Finish gate missing |
| ISS-R23 | High | `SubmissionDetailsBlock` has no View Submission read-only gate |
| ISS-R24 | Medium | Contacts / Projects no longer filtered by user area |
| ISS-R25 | Medium | Histology-ref format, year, duplicate and next-available validation lost |
| ISS-R26 | High | Add sample missing duplicate check, format validation, mouse-number range, file upload, TSE Daybook lookup, pre-booked block validation |
| ISS-R27 | Low | `SafeToHandle` 2-state vs 3-state; `SampleSameProjects` no UI; no Cancel confirmation; PM date unvalidated |
| ISS-R28 | High | Wet Tissue `SubmittedAs == "4"` unverified against `luSubmittedAs` |
