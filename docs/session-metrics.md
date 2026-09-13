# Session Metrics

> Extracted from `migration-run-journal.md` on 2026-08-21.
> Durations from 2026-08-19 onwards are derived from VS Code session store timestamps.
> All previously blank `—` duration cells filled 2026-08-21 with journal-stated or complexity-based estimates.
> Rows 53–70 added 2026-08-21 retrospectively — agent runs missing from original capture; durations are complexity-based estimates.

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
| 82 | 2026-09-04 | `GitHub Copilot` | — | — | **~30 min (complexity-based estimate)** | Run #21 — ISS-R28 resolved. User restated the Block Types vs Wet Tissue navigation rule as a spec; while verifying it against legacy source read directly (`clsBatch.vb`, `Common.vb`, `Cassetted.aspx.vb`), found `Cassetted.aspx.vb::btnYes_Click` decides Wet Tissue by comparing the resolved lookup *description* text, never a hardcoded numeric code — the migrated app's `submittedAsCode == "4"` guess (flagged unverified in ISS-R28) had no legacy or DB confirmation. Fixed by adding `ValidationHelpers.IsWetTissueDescription` and resolving the code via `ILookupService.GetLookupDataAsync(11)` before comparing, in both `SampleSummaryModel` and `AddSubmissionModel` (newly injected `ILookupService`). Added 7 unit tests. Build 0 errors, 4 pre-existing warnings; `dotnet test` 164 total, 163 passed, 1 skipped. |
| 83 | 2026-09-04 | `GitHub Copilot` | — | — | **~20 min (complexity-based estimate)** | Run #22 — QualityData.cshtml sorting/pagination (ISS-R06 groundwork). Converted `QualityDataModel` from `HistoPageModel` to `GridPageModel`; added `PagedEntries` (sorts the already-filtered Tests list across 7 columns) and `PopulateGridViewData`; view converted to `_SortableHeader`/`_Pagination` partials matching `UserMaintenance.cshtml`. `EditQualityDataTest.cshtml` confirmed to have no grid — not applicable. Build 0 errors; `dotnet test` 171 total, 170 passed, 1 skipped. |
| 84 | 2026-09-04 | `GitHub Copilot` | — | — | **~45 min (complexity-based estimate)** | Run #23 — Create submission auto-populate + grid sorting/pagination rollout. `BatchDetails.cshtml.cs` Create mode now defaults Submitted by/area to the logged-in user (still fully editable). Audited `BatchesNotReceived`/`BatchesForDispatch`/`BatchesReceived` — already had sorting/pagination, no changes needed. Converted `QCNotes.cshtml(.cs)` to `GridPageModel`. `ViewSubmissions.cshtml(.cs)` is POST-driven, so built two new POST-safe shared partials (`_SortableHeaderPost`, `_PaginationPost`) that submit the existing hidden carrier form instead of navigating via GET, preserving all search criteria; added `SortColumn`/`SortDesc`/`PageNumber`/`PagedResults`. Fixed an `RZ1010` Razor parse error found along the way. Build 0 errors; `dotnet test` 171 total, 170 passed, 1 skipped. |
| 85 | 2026-09-04 | `GitHub Copilot` | — | — | **~25 min (complexity-based estimate)** | Run #24 — ViewSubmissions sort/pagination colour mismatch, then a real non-functional-sort bug, both found via user retest. Fixed `.app-link-button` in `Styles.css` to state GOV.UK link colours directly instead of relying on `govuk-link`'s `:link`/`:visited` pseudo-classes, which never match a `<button>`. User retested and reported the sort icon still never appeared — root-caused as a real bug: hidden `SortColumn`/`SortDesc`/`PageNumber` fields in the carrier form always won over the sort button's query-string override (ASP.NET Core checks Form before Query), so sorting never actually changed server-side. Removed the hidden fields; carried sort/page state on the Select button's own `formaction` instead. Build 0 errors; `dotnet test` 171 total, 170 passed, 1 skipped. |
| 86 | 2026-09-04 | `GitHub Copilot` | — | — | **~40 min (complexity-based estimate)** | Run #25 — Three-part submission-workflow fix. `AddSubmission` Sender Ref search/select passthrough restored via the existing `SearchSender` picker; `SearchSender.cshtml` picker mode now auto-searches on load instead of requiring a second blank search; `SampleSummary.cshtml`'s submission-type caption (`SubmittedAsDescription`) fixed from a 2-way ternary collapsing 4 distinct submission types into one generic label to the correct per-type text. Build 0 errors; `dotnet test` 171 total, 170 passed, 1 skipped. |
| 87 | 2026-09-04 | `GitHub Copilot` | — | — | **~15 min (complexity-based estimate)** | Run #26 — Access Denied regression fix. `HistoPageModel.CheckBatchAccessAsync` had silently regressed to `Session.IsHistoUser` only, dropping the `|| Session.IsMaintenance` unrestricted-access condition added earlier in the session — restored, so Maintenance-role dev-bypass users regained batch access. Build 0 errors. |
| 88 | 2026-09-04 | `GitHub Copilot` | — | — | **~90 min (complexity-based estimate)** | Run #27 — Block management page un-merge. User reported the Run #88 consolidation of `Blocks/BlockDetails.cshtml` into `SubmissionDetailsBlock.cshtml` made the page cluttered; re-read legacy `SubmissionDetailsBlock.aspx(.vb)`/`BlockDetails.aspx(.vb)` directly and restored the two-page split — `SubmissionDetailsBlock` trimmed back to a pure grid (Block ref/Tissue details/Archive/EO/H&E/TSE-or-NonTSE columns/Special stain/Select, multi-select delete/copy, single-select edit redirect) and `Blocks/BlockDetails` recreated as the dedicated Add/Edit page (block ref, tissues, test checkboxes). Added `Block.Archived`. Build 0 errors; `dotnet test` 171 total, 170 passed, 1 skipped. |
| 89 | 2026-09-04 | `GitHub Copilot` | — | — | **~120 min (complexity-based estimate)** | Run #28 — SubmissionDetailsBlock/BlockDetails legacy-parity investigation. Verified the "Or Pick" histology-ref picker against legacy (confirmed correct, no change). Root-caused "No blocks found" via live `sqlcmd` against LocalDB: `BlockRepository.GetByBatchAsync`/`GetPreBookedByAnimalAsync` called nonexistent SPs, fixed to the confirmed real `GetBatchBlockDetails`/`GetAnimalPreBookedBlocks`. Fixed the grid's Archive/IHC Prp/IHC Other/Special Stain columns (were reading the wrong test-type table) and Tissue Details (raw code → resolved name). Discovered and fixed the same nonexistent-SP bug independently breaking tissue loading everywhere (`GetTissuesByBlockAsync`/`GetTissuesBySubmissionAsync`), affecting `BlockDetails`, `SubmissionDetailsBlock`, `SubmissionDetails`, `CopyBlocks`, `CopySamples`, `CopyBatch` — fixed via confirmed `GetBatchBlockTissues`/`GetBatchTissues`. Implemented full `BlockDetails.aspx` parity: Block Ref always free text with a legacy-matching default, missing Comments field, Additional Request enable/disable by `luSubmittedAs` code, Histology test-selection validation rules, "Use these tests for the next block?"/Next Block workflow, "Use the entire tissue list?" toggle. Build 0 errors; `dotnet test` 171 total, 170 passed, 1 skipped. |
| 90 | 2026-09-04 | `GitHub Copilot` | — | — | **~150 min (complexity-based estimate, multi-round)** | Run #29 — SubmissionDetails.cshtml full CRUD parity. PM Date fixed to native `type="date"` via new shared `DateFormatHelpers` (also reused for `SubmissionDetailsBlock`'s identical field). Built the previously-unwired Edit tissue feature. Root-caused "Add tissue not working" to a bogus `@UserID` param on `AddTissueAsync`. Multi-round "tissue still not loading" investigation with user-supplied network-log evidence each round: first patch attempt (borrow `BatchSubmissionID` from `GetAnimalsByBatchAsync`) was itself wrong — DB-verified that source ALSO never returns `BatchSubmissionID`; correct fix resolved it via existing `GetSubmissionsByBatchAsync` (batch-scoped) plus a missing `BatchSubmission.AnimalID` model property. Final round fixed Edit/Delete button style inconsistency, Add-tissue form being pre-filled with Edit values (split into dedicated `EditTissueCode`/`EditNoPieces`/`EditComment`), and "Save tissue not working" (missing `@BatchSubmissionID`/`@BlockID` keyfield param on `UpdateTissueAsync`). User raised a token-consumption concern mid-session and briefly disabled terminal/edit tools; re-enabled after discussion. Build 0 errors; `dotnet test` 171 total, 170 passed, 1 skipped throughout. |
| 91 | 2026-09-07 | `GitHub Copilot` | — | — | **~90 min (complexity-based estimate)** | Run #30 — Edit Submission field accessibility + Submissions On Hold filtering + Edit Submission vs Edit Submission Status review. Added Entered By/Entered Area/Submitted As (entirely missing before) as read-only fields on `EditBatch.cshtml` and made Submitted area (external) read-only, confirmed against `Batch.ascx.vb`/legacy field rules. Root-caused `SubmissionsOnHold.cshtml` via `SubmissionsOnHold.aspx.vb`: legacy shows every sample of the current batch with a per-sample On Hold checkbox, not a system-wide on-hold report — rewrote the page accordingly, reusing `SampleSummary`'s `MergeAnimals` pattern. Confirmed via `EditBatch.aspx.vb` that legacy's `EditBatch.aspx` genuinely is "Edit Submission Status" (a thin screen whose "Edit Submission" button redirects to `BatchDetails.aspx`, legacy's real full-edit screen) — assessed a full two-page split as out of scope for this pass and added clear GDS section separation on the single page instead, flagging the full split as a follow-up option. Build 0 errors; `dotnet test` 171 total, 170 passed, 1 skipped. |
| 92 | 2026-09-07 | `GitHub Copilot` | — | — | **~35 min (complexity-based estimate)** | Run #31 — GDS pagination ellipsis truncation + out-of-range page clamping. Fetched the official GOV.UK Design System pagination docs to confirm the exact algorithm/worked examples. Created `PaginationHelpers.BuildPageItems` (`Histo.Core.Domain`) and rewrote both `_Pagination.cshtml` (GET) and `_PaginationPost.cshtml` (POST) to use it instead of a naive `@for` loop rendering every page number. Also clamped `GridPageModel.PopulateGridViewData`'s `PageNumber` to `[1, totalPages]` (the "other pagination issue"), fixing a silent-empty-grid bug for stale/out-of-range page links. Added `PaginationHelperTests.cs` (12 new tests matching the GDS worked examples), which caught and fixed a bug in the helper's own neighbour calculation (used unclamped `currentPage`). Build 0 errors; `dotnet test` 183 total, 182 passed, 1 skipped (up from 171/170). |
| 93 | 2026-09-07 | `GitHub Copilot` | — | — | **~40 min (complexity-based estimate)** | Run #32 — Wet Tissue Create Submission Tissue Details root cause. Validated the Create Submission journey (`SubmissionDetails.cshtml`/Add Sample) against the already-fixed Edit journey; markup was identical/shared (no UI gap). Root-caused via legacy `clsBatchSubmission.vb::NewRecord` overload (confirms the real `AddBatchSubmission` SP accepts a per-animal `AnimalID`): `AddSubmissionModel.OnPostAsync` reused one shared submission (hardcoded `AnimalID=0`) for every new animal instead of creating a dedicated linked submission per animal, so `SubmissionDetailsModel.LoadAnimalAsync`'s AnimalID-matching resolution (Run #29) never found a fresh animal's real submission, leaving `BatchSubmissionID` at 0 and silently breaking Tissue Details load/Add/Edit/Delete for every newly-created Wet Tissue sample. Fixed `SubmissionRepository.AddSubmissionAsync` (was hardcoding `AnimalID=0`) and `AddSubmissionModel.OnPostAsync` (now creates a dedicated submission per new animal). Flagged `CopyBatch`'s `CopyAnimalAsync` as a likely same-class follow-up, not yet fixed. Build 0 errors; `dotnet test` 183 total, 182 passed, 1 skipped (unchanged). |
| 94 | 2026-09-07 | `GitHub Copilot` | — | — | **~15 min (complexity-based estimate)** | Run #33 — CopyBatch Wet Tissue flow follow-up fix. Same root cause as Run #32: `CopyBatchModel.OnPostAsync` created the destination submission before copying its animal, so `AnimalID` could never be linked. Added an optional `animalId` param to `CopySubmissionAsync` (interface + service) and reordered `OnPostAsync` to copy the animal first, then create the linked destination submission, then copy tissues. Applies to both cassetted and non-cassetted copy paths; harmless for cassetted (Block-owned tissue). Build 0 errors; `dotnet test` 183 total, 182 passed, 1 skipped (unchanged). |
| 95 | 2026-09-07 | `GitHub Copilot` | — | — | **~35 min (complexity-based estimate)** | Run #34 — ViewSubmissions/Edit Submission 4-issue fix. (1) Made "Customer Received Date" sortable on `ViewSubmissions.cshtml` (confirmed sortable in legacy) via `_SortableHeaderPost` + a new sort-switch case. (2) Disabled the Submission category (TSE/Non-TSE) radios on `EditBatch.cshtml` — legacy's Cassetted.aspx type step is never re-shown on Edit — plus backend hardening to always persist the original `BatchType`. (3) Root-caused Project/Pathologist dropdowns showing wrong values on Edit to `GetLookupDataAsync`'s default `includeInactive: false` silently dropping a since-deactivated saved value from the `<select>`; fixed with `includeInactive: true`. (4) Added a `.app-field-with-button` flex utility class so each dropdown + "Manage …" button pair aligns on one row. Build 0 errors; `dotnet test` 183 total, 182 passed, 1 skipped (unchanged). |
| 96 | 2026-09-07 | `GitHub Copilot` | — | — | **~50 min (complexity-based estimate)** | Run #35 — SubmissionDetailsBlock/BlockDetails full CRUD review. Made PM date/Histology reference read-only on `SubmissionDetailsBlock.cshtml` (explicit UX decision), removed the now-redundant "Save details" action, and made "Get next ref" persist the Histology reference immediately. Root-caused the empty Archive/EO/H&E/etc grid columns to a Dapper dynamic-row DBNull cast bug in `BlockTestRepository.Map` (`is not null` is true for `DBNull.Value`, so `(int)row.QCNote` threw whenever QCNote/Dispatched was NULL — a common state — silently wiping every indicator column for the whole batch); rewrote to use DBNull-safe `IDictionary<string, object>` reads. Added Edit tissue functionality and a read-only Histology reference row to `BlockDetails.cshtml`; confirmed "Or pick/Get next ref" and "Number of blocks" are both legacy-correct and were not missing. Build 0 errors; `dotnet test` 183 total, 182 passed, 1 skipped (unchanged). |
| 97 | 2026-09-07 | `GitHub Copilot` | — | — | **~30 min (complexity-based estimate)** | Run #36 — Context-sensitive Help navigation. Added `HelpSectionMap` (`Histo.Core.Domain`) mapping each current Razor Page path to its `Help/Index.cshtml` section anchor, cross-referenced against `docs/Functionality-Traceability-Matrix.md` for legacy-page renames/consolidations. `_NavPartial.cshtml`'s Help link now resolves the current page through the map and appends `#anchor`; falls back to the page top when unmapped. Added a focus-management script to `Help/Index.cshtml` (`tabindex="-1"` + `.focus()` on the fragment target, on load and `hashchange`) so the target section is genuinely focused for keyboard/screen-reader users, not just scrolled into view. Build 0 errors; `dotnet test` 191 total, 190 passed, 1 skipped (up from 183/182, 8 new tests). |
| 98 | 2026-09-07 | `GitHub Copilot` | — | — | **~20 min (complexity-based estimate)** | Run #37 — PR review comment fix. Fixed unencoded `SortColumn` in `ViewSubmissions.cshtml`'s Select button `formaction` (query-injection risk) by moving it to a `Uri.EscapeDataString`-encoded `SelectFormAction` PageModel property. Also fixed two unrelated pre-existing build-breaking regressions found along the way: a Razor RZ1010 parse error from an inline `@{ }` after `</form>` inside a nested `@if`, and a dangling `IsNeuropath` reference in `AddSubmissionModel.OnPostAsync` after the field was removed from the form — replaced with `Session.UserArea == "Neuropath"`, matching legacy's actual derivation. Build 0 errors; `dotnet test` 191 total, 190 passed, 1 skipped (unchanged). |
| 99 | 2026-09-07 | `GitHub Copilot` | — | — | **~55 min (complexity-based estimate)** | Run #38 — "Assign Tissues to Blocks" journey analysis + fixes. Compared legacy `BatchesReceived.aspx → BatchBlocks.aspx → FinalPrintBatch.aspx` against the current app, confirmed `SubmissionDetailsBlock.cshtml` (batch-wide mode) is the real equivalent of `BatchBlocks.aspx`. Fixed 3 gaps: missing Histology Ref/Tissue Details/test-indicator grid columns, missing "Add sample" button, and a completely missing "Done" action (IsBlocked/status/AllTissuesAssigned transition) — added `IBatchService.CompleteBlockAssignmentAsync`. `FinalPrintBatch.aspx` confirmed genuinely unmigrated (Reporting phase); "Done" redirects to Batches received instead. Documented the legacy session-deferred-commit vs current immediate-write architecture difference as an accepted, unchanged deviation. Build 0 errors; `dotnet test` 191 total, 190 passed, 1 skipped (unchanged). |
| 100 | 2026-09-07 | `GitHub Copilot` | — | — | **~70 min (complexity-based estimate)** | Run #39 — `Batches/BatchBlocks` page split. Split the batch-wide branch out of `SubmissionDetailsBlock.cshtml(.cs)` (now always requires `AnimalId`) into a genuinely separate `Pages/Batches/BatchBlocks.cshtml(.cs)`, mirroring legacy's real page boundary (`BatchBlockSummary.aspx` vs `BatchBlocks.aspx`, previously conflated onto one page). Repointed `BatchDetails`'s "Assign blocks" button and all `CopyBlocks`/`CopySamples`/`CopySamplesSummary` cancel/done redirects to the new page. Renamed "Copy samples" → "Copy from a previous submission" to disambiguate from `SampleSummary`'s "Copy sample" (confirmed via legacy `SV_CopySample` grep that it's dead code in legacy too). Build 0 errors; `dotnet test` 190 passed, 1 skipped, 0 failed. |
| 101 | 2026-09-07 | `GitHub Copilot` | — | — | **~30 min (complexity-based estimate)** | Run #40 — BatchesReceived grid columns + BatchBlocks action-button legacy parity. Trimmed `BatchesReceived.cshtml`'s grid to legacy's exact 6 columns. Rebuilt `BatchBlocks.cshtml(.cs)`'s action row to match legacy's single shared button group exactly (Add/Edit/Delete sample, Copy from a previous submission, Cancel, Done), replacing per-row links; "Delete sample" now removes the whole sample via `DeleteAnimalAsync`, matching legacy. Cancel/back-link retargeted to `/Batches/BatchDetails`. Build 0 errors; `dotnet test` 190 passed, 1 skipped, 0 failed. |
| 102 | 2026-09-07 | `GitHub Copilot` | — | — | **~45 min (complexity-based estimate)** | Run #41 — BatchesReceived wrong stored procedure + BatchBlocks CRUD review. Grepped the legacy `.aspx.vb` call site directly and confirmed the real SP is `GetBatchesToBeBlocked`, not `GetReceivedBatches` (dead code in legacy) — fixed, also expected to resolve the reported Species-as-ID bug (not verified live). Fixed default sort order and Go/Select validation + redirect target to match legacy. Found and fixed 2 more bugs in the BatchBlocks CRUD review: an incomplete-animal-list bug (same class as a previously-fixed `SampleSummaryModel` bug) and a missing antiforgery token on 3 bare `<form method="post">` tags (flagged, not mass-fixed, that the same pattern recurs in 32 files app-wide). Build 0 errors; `dotnet test` 190 passed, 1 skipped, 0 failed. |
| 103 | 2026-09-07 | `GitHub Copilot` | — | — | **~20 min (complexity-based estimate)** | Run #42 — BlockDetails Edit block Tissue type dropdown empty, root-caused and fixed. `GetBlockAnimalsByBatchAsync`'s result set doesn't carry `BatchSubmissionID`, starving the tissue-dropdown filter for animals resolved via that path (Edit block mode). Patched `Animal.BatchSubmissionID` from the plain animal list. Build 0 errors; `dotnet test` 190 passed, 1 skipped, 0 failed. |
| 104 | 2026-09-07 | `GitHub Copilot` | — | — | **~50 min (complexity-based estimate)** | Run #43 — root-cause correction of Run #42 + BlockDetails "Number of blocks" gap. The `BatchSubmissionID` patch from Run #42 was itself wrong (verified via `BlockDetails.aspx.vb`: the real filter is BatchID+AnimalID, SP `GetBatchSampleTissues`) — fixed by cross-referencing this animal's block IDs against `GetTissuesByBatchAsync` instead. Re-investigated "Archive/EO/H&E/IHC Other/Special Stain not populated" on `SubmissionDetailsBlock.cshtml`: a user-approved live sqlcmd check plus a throwaway xUnit test calling the repositories directly against LocalDB (bypassing the web app/Entra ID auth) proved the data pipeline was fully correct. Real root cause: govuk-frontend renders `.govuk-checkboxes__input{opacity:0}` and draws the tick via the adjacent label — these 7 indicator checkboxes had no label/wrapper, so were invisible regardless of checked state. Fixed in `SubmissionDetailsBlock.cshtml`/`BatchBlocks.cshtml`. Also fixed "Number of blocks" wrongly hidden for pre-cassetted Add mode on `BlockDetails.cshtml` (legacy always shows it), with bulk-creation now sequencing through `PreBookedBlockRefs` for pre-cassetted submissions. Confirmed the Histology Ref Type "Or pick"/"Get Next Ref" workflow (Run #28) remains correct, no change needed. Build 0 errors; `dotnet test` 190 passed, 1 skipped, 0 failed. |
| 106 | 2026-09-08 | `GitHub Copilot` | — | — | **~120 min (complexity-based estimate)** | Run #45 — Search module legacy-parity review + GDS remediation across 7 screens, then implementation over four turns. Legacy source is not in this repo; the user supplied the `Histopathology-2026-07-15\HistopathologySystem.2018` path mid-session, after which four parallel `Explore` subagents did the screen-by-screen comparison and the highest-severity findings were re-verified by hand before any edit. Restored 5 lookup dropdowns on `SearchSubmissions` (had been downgraded to free-text); removed `SearchTest`'s dead date filter after reading `Support\1\GetTestRows.sql` and confirming the SP takes no date parameters; restored legacy `IsDateRangeValid` guards and the submission-number rule; added a shared linked `_ErrorSummary` partial plus field-level GDS error markup; added sorting/paging to 3 grids (`SearchSubmissions` defaulting to legacy ID DESC) and CSV export to 4 pages; converted `SearchBlockRefs` to a GET search, restoring deep-linking and fixing a latent bug where sorting or paging silently dropped the criteria; replaced all native date inputs with the GOV.UK three-field component (`DateParts` + `_DateInput`); deleted the redundant `SearchSample` page. Build 0 errors; `dotnet test` 224 total, 217 passed, 1 skipped, 6 failed — the 6 failures confirmed pre-existing by stashing the tree and re-running (199 passed / 6 failed baseline). |
| 107 | 2026-09-08 | `GitHub Copilot` | — | — | **~25 min (complexity-based estimate)** | Run #46 — QualityData grid showing an extra "Special Stain" row vs legacy. Root-caused via legacy `clsBatchSummary.vb::CreateTestSummaryData`'s `BLOCK_HISTOLOGY` loop: only codes 1/2/5/7 get a worklist row, codes 3/4/6 are gating flags only (real rows live in `BLOCK_ANTIBODIES`/`BLOCK_STAIN`). Confirmed live for submission 29399 (legacy math 0+0+3=3, current app 1+0+3=4). Fixed `BlockTestRepository.GetByBatchAsync` to filter histology rows to codes 1/2/5/7 before mapping. Build 0 errors (`Histo.Histology` project). |
| 108 | 2026-09-08 | `GitHub Copilot` | — | — | **~20 min (complexity-based estimate)** | Run #47 — `BlockTestRepository.UpdateAsync` missing `@Code`/`@EnteredBy`/`@PremiumCharge` params. User hit `SqlException: 'EditBlockStain' expects parameter '@Code'` on save. Confirmed via `sys.parameters` all 3 Edit SPs need these params; added `EnteredBy`/`PremiumCharge` to the `BlockTest` model (mapped from the already-returned SP columns) and wired all 3 params into `UpdateAsync` (`EnteredBy` always the current user per legacy; `PremiumCharge` round-tripped unchanged). Build 0 errors. |
| 109 | 2026-09-08 | `GitHub Copilot` | — | — | **~20 min (complexity-based estimate)** | Run #48 — `EditQualityDataTest` error summary not clickable. Replaced the plain single-string `Error` with a field-keyed `Errors` dictionary rendered via the existing shared `_ErrorSummary.cshtml` partial (already used on the Search module), added field-level GDS error markup to all 7 validated fields, and switched validation to accumulate all errors in one pass. Added a separate non-linked `ConcurrencyError` for the optimistic-concurrency case. 5 new tests. Build 0 errors. |
| 110 | 2026-09-08 | `GitHub Copilot` | — | — | **~40 min (complexity-based estimate)** | Run #49 — Pick List Maintenance Add/Edit GDS page split. `EditLookupItem.cshtml` combined item list + Add form + Edit form on one page, violating GDS "one thing per page". Confirmed via `sys.parameters` on every editable lookup table's Add SP that only 2 field-shape variants exist (Code-keyed vs Area-scoped/ID-keyed). Split into `LookupItems.cshtml` (list), `AddLookupItem.cshtml` (new, Add-only) and `EditLookupItem.cshtml` (trimmed to Edit-only), with a shared `LookupTableSchema` helper deriving field shape identically across all 3. 12 new tests. Build 0 errors; `dotnet test` 267 passed, 4 pre-existing unrelated failures, 1 skipped. |
| 111 | 2026-09-09 | `GitHub Copilot` | — | — | **~30 min (complexity-based estimate)** | Run #50 — `LookupItemsModel.OnGetAsync` regression fix (grid silently empty) + 4 pre-existing `BatchesReceivedModelTests` failures fixed (missing `await`, stale redirect assertion) + `BatchesForEditing` default-sort fix + `BatchesForArchiving` back-link infinite-loop fix. Build 0 errors; `dotnet test` 274 passed, 1 skipped, 0 failed. |
| 112 | 2026-09-09 | `GitHub Copilot` | — | — | **~35 min (complexity-based estimate)** | Run #51 — `BatchDetails` Project/Contact `includeInactive` fix; `ArchiveBlocks`/`ArchiveTissues` archive-location-name, sortable columns, and 7 summary fields added via new shared `BatchSummaryDisplayResolver`; app-wide `BatchStatus` "Not started"→"Not received" label correction (verified against live `luStatus` table). Build 0 errors. |
| 113 | 2026-09-09 | `GitHub Copilot` | — | — | **~30 min (complexity-based estimate)** | Run #52 — `EditBatch` button-group alignment; `ISessionService.ReturnPageQuery` added so Cancel/Back/post-save redirects can restore full list sort/page context without breaking `RedirectToPage`'s bare-page-name requirement; `BatchesForEditing` captures the query on select; `EditSubmissionStatus` gained missing Submitted by/area/date summary rows via `BatchSummaryDisplayResolver`. Build 0 errors. |
| 114 | 2026-09-09 | `GitHub Copilot` | — | — | **~40 min (complexity-based estimate)** | Run #53 — ViewSubmissions area-restriction false positive fully reverted after user disproved the hypothesis with a direct legacy observation (cross-VLA record appearing under a 2-row Rejected filter). Cross-checked `migration-run-journal.md` confirming this exact fact was already established once before (Run #57/ISS-030). Added 2 permanent regression-guard tests. |
| 115 | 2026-09-09 | `GitHub Copilot` | — | — | **~35 min (complexity-based estimate)** | Run #54 — Deep-read legacy `AddSubmission.aspx.vb`; repositioned the "Check historical data" button out of an illegally-nested `<form>` into the main form via HTML5 `formmethod`/`formaction`; closed `Parity-Audit-Report.md` D-3/D-4 and added D-8 (all Neuropath/Mouse-Bioassay-only features being decommissioned). |
| 116 | 2026-09-10 | `GitHub Copilot` | — | — | **~20 min (complexity-based estimate)** | Run #55 — Footer GOV.UK crown replaced with the APHA departmental logo; corrected an initial aspect-ratio-distorted `60×60` sizing to the image's true ratio at `120×136`, moved out of `govuk-footer__crown` into a dedicated `govuk-footer__logo` row per GOV.UK Design System guidance and WCAG 1.4.5 legibility. |
| 117 | 2026-09-10 | `GitHub Copilot` | — | — | **~10 min (complexity-based estimate)** | Run #56 — `ViewSubmissions` "Print submission notes" button gated on `HasNotes` (mirrors existing `PrintSubmissionModel.HasNotes` precedent), so it's only enabled when the submission has recorded comments/status comments. |

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

---

## Session 2026-09-08 — Search module legacy parity + GDS review

**Run:** #45. **Duration:** ~120 min across four turns (review, then three implementation passes).

### Approach

Legacy `.aspx` source does not exist in this repository. After confirming this (workspace, sibling `bse` repo, and the whole `source\repos` tree), the user supplied the legacy path. Four `Explore` subagents then ran the screen-by-screen comparison in parallel; the two highest-severity findings were re-verified by hand against the actual files before any code was changed, and the `SearchTest` date defect was confirmed by reading the real stored procedure script rather than inferring it.

### Work completed

| # | Severity | Area | Fix |
|---|---|---|---|
| 1 | High | `SearchSubmissions` | 5 lookup dropdowns (Project, Pathologist, Species, Fixation, Submitted area) had been downgraded to free-text boxes — any typo silently returned zero rows. Restored via `ILookupService`, following the `ViewSubmissions` ISS-030 pattern. Also fixed `SubmittedArea` bypassing `NullIfEmpty()`. |
| 2 | High | `SearchTest` | Date inputs were bound and rendered but never reached the query. `GetTestRows` declares only `@ProjectContractDesc`/`@BatchType` (verified in `Support\1\GetTestRows.sql`), so the controls were removed rather than left as a dead filter. |
| 3 | High | `SearchPMDates`, `SearchSubmissions` | Restored legacy `IsDateRangeValid` from ≤ to guards (3 ranges) and the `revSubmissionNumber` `^[1-9]+[0-9]*$` rule. |
| 4 | GDS | Module-wide | Shared `_ErrorSummary.cshtml` with clickable `<a href="#FieldId">` links; field-level `govuk-form-group--error` / `govuk-error-message` / `aria-describedby`. Only 3 of 11 pages had any summary before, and none linked. |
| 5 | Medium | 3 grids | Sorting + pagination on `SearchSubmissions` (default ID DESC per legacy), `SearchPMDates`, `SearchUnUsedHistologyRefs`. |
| 6 | Medium | 4 pages | CSV export via the existing `CsvExportHelper`, replacing legacy `hlbExcel`/`hlExcelExport`. |
| 7 | Medium | `SearchBlockRefs` | Converted to a GET search — restores legacy query-string deep-linking **and** fixes a latent bug where the page's GET sort/paging partials silently dropped its POST-bound criteria. ICC_Sub link re-added. |
| 8 | GDS | `SearchPMDates`, `SearchSubmissions` | Native `<input type="date">` replaced with the GOV.UK three-field component (`DateParts` + `_DateInput`). PM dates now blank-on-load and mandatory, matching legacy. |
| 9 | — | Rationalisation | `SearchSample` deleted (duplicate of `SearchSender`, no callers, no tests); `SearchSender` kept — `CopyBatch`'s picker depends on it; `SearchMenu` gained the missing "View samples" and "View old ICC_Sub data" links. |

**Build:** 0 errors. **Tests:** 224 total, 217 passed, 1 skipped, 6 failed — all 6 pre-existing, confirmed by stashing the working tree and re-running (199 passed / 6 failed baseline). 18 new tests in `SearchValidationTests.cs`.

### Outstanding items

| ID | Severity | Summary |
|---|---|---|
| ISS-R29 | High | `GetTestRows.sql` selects `PROJECTCONTRACTCODE`/`SUBMITTEDAREA` but the code maps to `TestItemRow{Description,Count}` — if the live SP matches that script, `SearchTest` renders blank descriptions and zero counts. Needs DB verification. |
| ISS-R30 | Medium | Empty-string vs NULL filter semantics — legacy passed `""`, migrated code passes `NULL`. Needs a DB-side check that each search SP tests `IS NOT NULL` rather than `<> ''`. |
| ISS-R31 | Medium | Non-clickable error summaries outside the Search module (~30 files). Not swept because error models differ per page (`List<string>` vs single string vs dictionary) and several are page-level errors GDS does not require to link. |
| ISS-R32 | Low | `SearchTest` legacy premium-charge cross-tab and test-type checkbox filtering remain unported (accepted simplification, Parity Audit §2 row 54). |
