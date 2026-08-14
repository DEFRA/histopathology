# User Prompts Log

A chronological record of the user's prompts/requests in this conversation session, verbatim.

---

## Prompt 1 — Full legacy-vs-current parity audit

> Please verify that all modules and functionality from the legacy application have been successfully migrated and implemented in the new .NET Razor application. Specifically: Compare the legacy application against the current .NET Razor with GDS implementation. Confirm that every module, screen, feature, workflow, and business rule has been migrated. Identify any missing modules, pages, or functionality. Validate that all CRUD (Create, Read, Update, Delete) operations available in the legacy application are present and working correctly in the .NET Razor version. Highlight any gaps, discrepancies, or incomplete migrations. Provide a detailed list of findings along with recommendations and required actions to achieve full parity with the legacy application.

---

## Prompt 2 — Prioritize completing remaining page migration

> Which agent should I run to migrate the remaining pages? Please focus on migrating all pages first . I can fix identify/report any issues or gaps later. The priority is to complete the migration of all remaining pages before addressing defects.

---

## Prompt 3 — Export prompts to a markdown file

> Can you write all the prompt that i have used in separate .md file

---

## Prompt 4 — Update prompts log with all session prompts

> Can you write all the prompt from all the session in user prompts log

---

---

## Prompt 5 — Investigate Run Log items 16–19 and update prompts log

> go throguh the Run Log in run journal.md and see the run log item 16 to 19, why its not done, aslo update this prompt in user prompt log.md

---

## Prompt 7 — Fix AddSubmission showing Sender Reference instead of TSE/Non-TSE step

> Still add Add Submission shows Sender reference when i clicke on it, can you please fix as legacy applicaitn

---

## Prompt 9 — GDS compliance review of the Search module

> Please review the Search functionality and verify that all links, components, layouts, styling, content, and user interactions comply with GDS standards and design patterns. Identify any deviations from GDS guidelines, highlight inconsistencies, and provide recommendations to bring the implementation into full compliance.

---

## Prompt 10 — Add GDS search review to run journal and prompts log

> Can you please add them into migratin run journal, prompts logs,

---

## Prompt 11 — GDS compliance review of Bookings, Archive, and Audit Log modules

> Can you please add them into migratin run journal, prompts logs, below pages and links
>
> Booking menu links
> Book Histology Reference
> Book Block Reference
> Edit Histology References
>
> Archive
> Archive Blocks
> Archive Tissues
> Batches for Archiving
>
> Audit Log
> Search by Date
> Search by User
> Search by Submission

---

## Prompt 12 — Create Functionality Traceability Matrix document

> Please create a new Markdown (.md) file that maps all existing functionality from the legacy application to the corresponding modules, pages, and features in the new .NET 10 Razor Pages application also check if the exisitng parity audit report or migratin run journal more than . Include: Legacy functionality/page name; New .NET 10 Razor Page or module; Migration status (Completed, Partial, Pending); Notes on changes or enhancements; Any identified gaps, parity issues, or missing functionality. The document should provide a clear traceability matrix to support migration tracking and parity validation.

---

## Prompt 13 — GDS compliance sweep of remaining modules (Batches, Submissions, QC, Admin, Blocks)

> Can you go for remaining ~40 pages in the Batches, Submissions, QC, Admin, and Blocks modules. etc and add the prompts and parity, journal

---

## Prompt 14 — Home page and nav label reverts

> Can you please make the changes on the below lable
> Receive Submissions / Receive batches — can revert
> Booking / Book histology refs — can revert
> Pick List Maintenance / Pick lists — can revert
> Search Outputs — Inside Search menu. Why has this been added in navigation link?
> Why were “Batches received”, “Dispatch batches”, “Fix completed dates” added to navigation when they were not home links in legacy?

---

## Prompt 15 — Search outputs label revert and Fix completed dates provenance query

> Can you update Search Outputs instead of “Search test totals” also i’m a maintenance user i dont’ see this in Fix completed dates in my legacy applicaitnion why

---

## Prompt 16 — Remove Fix completed dates from home page

> Remove it from the home page — it stays accessible at /Admin/FixCompletedDates by direct URL, exactly as legacy behaved

---

## Prompt 17 — Remove Batches received from nav and home

> remove only “Batches received” from the nav.

---

## Prompt 18 — Approve ISS-025 and proceed with Module 1 implementation (Run #54)

> Yes — approved. Please go ahead and implement Module 1 (ISS-025) as planned in the gap audit.

---

## Prompt 19 — Update Session Metrics timing for Runs 54–56

> Can you update the Start Time, End Time, and Duration columns in Session Metrics for rows 20, 21, and 22?

---

## Prompt 20 — Derive timing automatically from session file timestamps

> Can you get the timing from this session?

---

## Prompt 21 — Add session prompts to User Prompts Log

> Can add the prompt which i used in this session in user prompts

---

## Prompt 22 — Cross-module column/filter gap audit with module-by-module approval gate (Run #53)

> Can you check if there are any other pages that has missed any columns or filter and update the in issue in run journal and prompt in prompt also finally update the duration that took, implmennt them in by the module vise wait for my approval before taking to another model, why this was converted from ui implmentation agent.

---

## Prompt 24 — Detailed ViewSubmissions and cross-module dropdown/Clear Search parity analysis

> Please analyze the View Submission pages in the new application and compare them with the legacy application. Also analyze the other modules.
>
> Observations:
>
> In the legacy application, the following fields are dropdowns, but in the new application they appear as text boxes:
> Project or Contract Code
> Pathologist
> Species
> Fixation
> The Clear Search option available in the legacy application is missing in the new application.
> The Submitted Area field is not available in the legacy application but is present in the new application.
>
> Request:
>
> Perform a detailed comparison across all View Submission pages.
> Identify and document all discrepancies between the legacy and new applications.
> Add the findings as issues in the Run Journal.
> Update the prompt file with the identified issues and observations.

---

## Prompt 25 — PickListMaintenance + EditUser CRUD and pre-population gap analysis, fix ISS-031/032/033

> Can you analyze the Pick List Maintenance module in the legacy codebase and compare it with the new application? The PickListMaintenance page appears to show a raw table of stored procedure configuration instead of a user-friendly list of pick list names. Also analyze the EditUser page — the Change link appears to navigate to /Admin/EditUser/0 for every user, and the Group and Area dropdowns on the edit form are always blank.
>
> Findings and fixes requested:
> - ISS-031: PickListMaintenance shows SP config dump; pick list names blank (TableName Dapper mismatch).
> - ISS-032: All pick-list dropdowns across the entire application render blank option labels (LookupItem.Name/Active Dapper column-name mismatch against Description/IsActive returned by legacy SPs).
> - ISS-033: EditUser Change link always goes to /Admin/EditUser/0; Group and Area dropdowns blank on edit form (User.UserID/GroupCode/AreaCode Dapper mismatch against ID/UserGroup/UserArea returned by GetUsers SP).
>
> Fix all three issues and update the Open Issues table, Session Metrics, and this prompts log.

---

## Prompt 26 — User Maintenance page empty after ISS-033 fix — root cause, prevention, fix ISS-034

> Why is the User Maintenance page showing as empty? What caused this issue, and how can we prevent similar issues in the future?
>
> I originally requested a fix for the Edit functionality, but it appears that the change has also impacted the user listing feature, which is now not displaying any users.
>
> Please investigate the root cause, explain why this happened, and identify the steps needed to ensure that fixes to one feature do not unintentionally break existing functionality.
>
> Add this in run Journal and prompt and fixing time.

---

## Prompt 27 — User Maintenance still empty after ISS-034 fix — ISS-035

> Still user list is not listed in the page in following https://localhost:57879/Admin/UserMaintenance

---

## Prompt 23 — Investigate ViewSubmissions column and filter discrepancy vs legacy

> Investigate the discrepancy in the View Submission screen between the legacy application and the new application.
> Observations:
>
> The legacy application displays significantly more columns in the grid.
> The new application shows only a limited number of columns.
> The legacy application provides multiple filtering options.
> Several of these filters appear to be missing in the new application.
> Please analyze:
>
> Why the new application displays fewer grid columns compared to the legacy application.
> Whether the missing columns were intentionally removed or omitted during migration.
> Why the filtering options available in the legacy application are not present in the new application.
> Whether there are any design, performance, or technical constraints causing these differences.
> The impact of these missing columns and filters on user functionality and business processes.
> Provide recommendations to align the new application with the legacy functionality, if required.

---


---

## Prompt 28 — Booking/Archive/Quality Data/nav naming discrepancy investigation (2026-08-06)

> In the legacy application, the Booking section contains the following links:
> Book Non-PG Histology Ref
> Book Blocks
>
> However, in the new application, the corresponding links are:
> Book Histology Reference
> Book Block Reference
> Edit Histology References
>
> Could you clarify the reason for this naming discrepancy between the legacy and new applications? Has the terminology been intentionally updated, and if so, is there any business or user requirement that drove the change?
>
> In the legacy application, the Archive section contains:
> Archive Blocks
> Archive Tissues
>
> In the new application, the Archive section contains:
> Archive Blocks
> Archive Tissues
> Batches for Archiving
>
> Could you explain why "Batches for Archiving" has been added to the new application? Is this a new feature, or does it correspond to an existing function in the legacy system under a different name?
>
> In the legacy application:
> BatchesForDispatch.aspx is displayed as Enter Quality Data
> There is also Edit QC Notes
>
> In the new application:
> The navigation menu shows Quality Data
> The home page shows QC Notes
>
> Could you clarify the reasoning behind these naming differences? Are Enter Quality Data, Quality Data, Edit QC Notes, and QC Notes intended to represent the same functionality, or have there been changes to the underlying business processes?
>
> The new application also includes the following navigation links:
> Edit Batches
> Dispatch Batches
>
> Could you explain the purpose of these additional navigation items? Were they present in the legacy system under different names, or have they been introduced as new functionalities? Understanding the business need for these entries would help ensure feature parity and consistent terminology between the legacy and new applications.
>
> Need to update run journal user prompt log

**Findings:** "Book Non-PG Histology Ref" → "Book histology reference": "Non-PG" qualifier silently dropped — no documented business decision; the qualifier signals PG-numbered sender refs are excluded from a booking range. "Edit histology references": not in legacy `BookingMenu.aspx`; added in Run #21 (ISS-021) with no legacy menu precedent. "Batches for archiving" is not a new feature — it is the legacy `BatchesForArchiving.aspx` ("Archive Submission"), reorganised from home page into the Archive sub-menu and renamed. Home page labels correct vs legacy; nav bar inconsistent: "Quality data" linked to `QCNotes` while "Dispatch batches" linked to `BatchesForDispatch` (the page the home calls "Enter quality data"). "Edit batches" and "Dispatch batches" in the nav were agent-introduced names inconsistent with both the home page and legacy.

---

## Prompt 29 — Implement Booking/Archive/nav label parity fixes (2026-08-06)

> Can you implement the above the suggestion remove if something is not present in legacy

**Changes applied (Run #63):**
- `BookingMenu.cshtml`: restored "Book Non-PG histology ref" and "Book blocks"; removed "Edit histology references" (not in legacy booking menu).
- `BookHistologyRef.cshtml`: page title restored to "Book Non-PG histology ref".
- `BookBlockRef.cshtml`: page title restored to "Book blocks".
- `ArchiveMenu.cshtml`: removed "Batches for archiving" (was never in legacy `ArchiveMenu.aspx`; page remains on home page as "Archive batches").
- `_NavPartial.cshtml`: "Edit batches" → "Edit submission status"; "Dispatch batches" → "Enter quality data"; "Quality data" → "QC notes". Nav bar now consistent with home page and legacy.

Build: 0 errors, 0 warnings. Tests: 90 pass, 1 skipped, 0 fail.

---

---

## Prompt 30 — Remove stub identity; load user/group/area from database (2026-08-07)

> We are planning to integrate Entra ID in the future. Until that implementation is completed, I want the application to launch without requiring Entra ID authentication.
>
> Please review the EntraID-Implementation-plan.md document for context, but do not make any changes related to the Entra ID implementation itself. The current requirement is only to bypass the login process and allow the application to launch successfully.
>
> User validation is already handled through the existing database logic, which checks whether the user is active. Reuse this existing mechanism and retrieve the user details directly from the database.
>
> The application should display the following user information from the database:
> - User Name
> - Group
> - Area
>
> For example, the displayed values should reflect the actual database records, such as:
> User: Silambarasan Duraiswamy
> Group: Maintenance
> Area: Other VLA
>
> Please investigate where the current values are being populated from, remove any hardcoded or stubbed user information, and ensure that all displayed user, group, and area details are sourced from the database while keeping the future Entra ID integration untouched.

**Root cause:** `HttpContext.User.Identity.Name` is empty under Kestrel (no Windows Auth active). The code fell through to a hardcoded stub `User { Name = "Dev User (stub)", AreaName = "Development" }` instead of querying the database.

**Fix applied to `src/Histo.Web/Pages/HistoPageModel.cs`:**
- Removed `using Histo.Administration.Models` and `using Microsoft.Extensions.Hosting` (no longer needed).
- Removed the `IHostEnvironment env` variable and all `IsDevelopment()` guards.
- The `Environment.UserDomainName\Environment.UserName` fallback is now unconditional when `User.Identity.Name` is empty — safe because IIS always populates it in production; only empty under Kestrel.
- Removed the entire hardcoded stub `User` block.
- If `ResolveUserAsync` returns null (user not in DB or inactive) → `ForbidResult()` — mirrors the legacy redirect to `unauthorized.htm`.
- `appsettings.Development.json` (gitignored) already in place with the real local connection string, enabling the DB lookup to succeed.

---

**Fix applied to `src/Histo.Web/Pages/HistoPageModel.cs`:**
- Removed auto-NT-detection logic entirely (was unreliable on non-domain-joined machines: `Environment.UserDomainName` returns `WORKGROUP`).
- Replaced with a pure session gate: if `Session.GroupName` is empty → redirect to `/Login`.
- `AccessDenied` redirect removed from this class (no longer needed — unauthenticated users go to `/Login`).
- `Login.cshtml` and `Login.cshtml.cs` created as a pre-Entra-ID bridge (ADR-006).
- `AccessDenied.cshtml` retained for future use when Entra ID auth denies access.

*Updated on 2026-08-07.*

---

## Prompt 32 — ADR decommission tracking for Login bridge page (2026-08-07)

> Can you tell how to track that how to decommission this page when actual Entra id is integrated

**Answer:** The recommended approach is (1) create an ADR recording the bridge decision and decommission trigger; (2) add `// BRIDGE (ADR-006):` comments to all three affected source files — discoverable via `grep -r "BRIDGE (ADR-006)" src/`; (3) a GitHub Issue tagged `entra-id`, `tech-debt` linked to Phase B milestone; (4) update the run journal Phase Tracker for Phase B with a reference to merging/closing ADR-006. Decommission trigger: Phase B Step 5 — ITfoxtec SAML ACS endpoint live and calling `Session.PopulateFromUser(user)`. Files to delete: `Login.cshtml`, `Login.cshtml.cs`. Files to update: `HistoPageModel.cs` — change `RedirectToPageResult("/Login")` to SAML challenge redirect.

---

## Prompt 33 — Implement Login bridge page (ADR-006, HistoPageModel simplification) (2026-08-07)

> Can you implement the changes now?

**Changes applied (Run #66):**
- `docs/ADR/ADR-006-manual-login-page-bridge.md` — ADR created with full decision rationale, decommission trigger, files inventory, and `// BRIDGE (ADR-006):` marker strategy.
- `src/Histo.Web/Pages/HistoPageModel.cs` — Simplified to a 4-line session gate (`if (string.IsNullOrEmpty(Session.GroupName)) → RedirectToPage("/Login")`). All auto-NT-detection and DB lookup logic removed from base class.
- `src/Histo.Web/Pages/Login.cshtml` — GDS-compliant NTLogin entry form with error summary and inline error states. `Layout = null`. `// BRIDGE (ADR-006):` comment in template.
- `src/Histo.Web/Pages/Login.cshtml.cs` — `LoginModel : PageModel` (not `HistoPageModel` — prevents redirect loop). `OnGet` short-circuits if already signed in. `OnPostAsync` strips domain prefix → `UserService.ResolveUserAsync` → `Session.PopulateFromUser` → redirect to `/Index`.

Build: 0 errors, 0 warnings. Tests: 90 pass, 1 skipped, 0 fail.

---

## Prompt 34 — Help Pages investigation, GDS implementation, and Run Journal update (2026-08-07)

> Check why the Help Pages were not migrated from the legacy application during the .NET migration.
>
> Implement the Help Pages in the .NET 10 application by referring to the legacy implementation and ensure they follow GDS standards.
> Verify that all Help Page content, links, and user journeys work correctly after migration.
>
> Finally, provide a Run Journal including:
> - Tasks completed
> - Issues and resolutions
> - Start and end times
> - Total time taken to implement
> - Testing and verification summary
>
> Include the Run Journal in the final output. add this prompt in user prompt file

**Root cause (why not migrated):** The legacy help was delivered as two static `.htm` files (`HistoHelp_CustomerGroup.htm` and `HistoHelp_HistoGroup.htm`), not `.aspx` pages. The migration agent inventoried `.aspx` pages only — the `.htm` files were invisible to the migration scope. The Help link in `VLAHeader.ascx` (`lnkHelp`, Target="_blank") was dynamically set to `HistoHelp_CustomerGroup.htm#{PageName}` (Customer) or `HistoHelp_HistoGroup.htm#{PageName}` (all others) but was never ported when `VLAHeader.ascx` was replaced by `_Layout.cshtml` and `_NavPartial.cshtml`.

**Changes applied (Run #67):**
- `src/Histo.Web/Pages/Help/Index.cshtml` — Single GDS-compliant help page. Customer group: 15 sections (general, submission workflow, viewing). Histopathology User / Maintenance: 37 sections (all customer sections + receive, quality data, archive, QC notes, search, booking, admin, audit logs, imported data). All legacy VLA toolbar-icon descriptions modernised to GDS plain English. Numbered table of contents. Anchor IDs match legacy `#PageName` pattern for future context-sensitive deep-linking. GDS `govuk-heading-l/m`, `govuk-body`, `govuk-list`, `govuk-summary-list` throughout.
- `src/Histo.Web/Pages/Help/Index.cshtml.cs` — `HelpModel : HistoPageModel` (session-gated).
- `src/Histo.Web/Pages/Shared/_NavPartial.cshtml` — Help link added at the bottom of the nav bar (available to all groups).

Build: 0 errors, 0 warnings. Tests: 90 pass, 1 skipped, 0 fail.

---

## Prompt 30 — Search functionality and navigation audit: Receive Submission, Edit Submission Status, Entry Quality Data, QC Notes (2026-08-06)

> Analyze the following modules:
>
> - Receive Submission
> - Edit Submission Status
> - Entry Quality Data
> - QC Notes
>
> In the legacy application, each of these screens contains a grid that is loaded when the page is accessed. Each module also provides a search field that allows users to search using the relevant reference number, such as:
>
> - QC Note Reference Number (for QC Notes)
> - Submission Number (for Submission-related modules)
> - Any other module-specific reference number
>
> Verify that the search functionality is wired correctly. Specifically, confirm that when the user enters a valid reference number and clicks the Check or Go button, the application navigates to or loads the correct page and displays the expected data/results.
>
> Please review and validate:
>
> 1. Grid loading behavior for each module.
> 2. Search field functionality and input validation.
> 3. Check/Go button event handling.
> 4. Navigation to the correct target page.
> 5. Retrieval and display of the correct records based on the entered reference number.
> 6. Any inconsistencies, broken links, missing mappings, or functional issues compared to the legacy behavior.
>
> Needs to capture in run journal file and user prompt file.

**Legacy-to-modern module mapping (confirmed from Home.aspx):**

| User-facing label | Legacy file | Modern Razor Page |
|---|---|---|
| Receive Submission | `BatchesNotReceived.aspx` | `Batches/BatchesNotReceived.cshtml` |
| Edit Submission Status | `BatchesForEditing.aspx` | `Batches/BatchesForEditing.cshtml` |
| Enter Quality Data | `BatchesForDispatch.aspx` | `Batches/BatchesForDispatch.cshtml` |
| Edit QC Notes | `QCNotes.aspx` | `QC/QCNotes.cshtml` |

**Findings (Run #64):**

| # | Module | Area | Legacy | Modern | Verdict |
|---|--------|------|--------|--------|---------|
| 1 | All 3 batch modules | Grid loading | `clsBatch.GetBatchesWithStatus` / `GetBatchesForDispatch` on `Page_Load` | `BatchService.GetNotReceivedAsync` / `GetInProgressAsync` / `GetForDispatchAsync` on `OnGetAsync` | ✅ Grid data loaded correctly on page access |
| 2 | QC Notes | Grid loading | `clsQCNote.GetBatchQCNotes()` — all notes, no batch filter | `_qc.GetBySubmissionAsync(Session.BatchID.Value)` — batch-scoped | ⚠️ Scope changed; see ISS-045 |
| 3 | Receive Submission | Search/Go | Validates `STATUS_SUBMITTED` via `CheckBatchExists`; shows red error if not found | Sets `Session.BatchID` unconditionally; redirects; no validation | ❌ ISS-042: No status/existence check |
| 4 | Edit Submission Status | Search/Go | Validates any status (0) via `CheckBatchExists`; shows red error if not found | Sets `Session.BatchID` unconditionally; redirects; no validation | ❌ ISS-042 + ISS-044: No existence check; grid only shows in-progress |
| 5 | Entry Quality Data | Search/Go | Validates `STATUS_INPROGRESS` or `STATUS_RECEIVED`+cassetted; shows red error | Sets `Session.BatchID` unconditionally; redirects; no validation | ❌ ISS-042: No status/existence check |
| 6 | QC Notes | Quick-Go | Calls `GetBatchQCNotes(noteRef)` to verify existence; shows red error if not found | Redirects to EditQCNote unconditionally; target redirects back if null | ⚠️ No inline error; EditQCNote handles null gracefully |
| 7 | QC Notes | Edit button (grid) | `grdQCNotes.DataKeys` = `QCNoteRef`; navigates to `EditQCNote.aspx?QCNoteRef=<ref>` | `value="@n.ID"` (Submission Number) passed as `noteId` — wrong field | ❌ ISS-043: Critical bug — Edit navigates to wrong note |
| 8 | Edit Submission Status | Grid scope | `GetBatchesWithStatus(0)` — all statuses shown | `GetInProgressAsync()` — only in-progress | ❌ ISS-044: Grid shows only in-progress, not all statuses |
| 9 | QC Notes | Page entry path | Standalone; loaded from Home page "Edit QC Notes" | Batch-scoped; `Session.BatchID` required | ⚠️ ISS-045: Home nav link leads to empty page without prior batch selection |
| 10 | All 3 batch modules | Input validation | `RequiredFieldValidator` + `RegularExpressionValidator` (`^[1-9]+[0-9]*$`) client + server | `type="number" min="1"` HTML attribute only; no server-side revalidation in handler | ⚠️ Server-side validation bypassed if form manipulated; acceptable risk for internal app |
| 11 | All 3 batch modules | Navigation target | `ReceiveBatch.aspx`, `EditBatch.aspx`, `QualityData.aspx` respectively | `/Batches/ReceiveBatch`, `/Batches/EditBatch`, `/QC/QualityData` respectively | ✅ Correct target pages |

**Issues raised:** ISS-042 (High), ISS-043 (Critical), ISS-044 (High), ISS-045 (Medium). All captured in `docs/migration-run-journal.md` Open Issues table. No code changes in this run — issues require team review before fix implementation.

---

## Prompt 31 — Fix ISS-042, ISS-043, ISS-044, ISS-045 and update run journal session metrics (2026-08-06)

> Can you please fix the issues and update the session matrix in the migration runjournal?

**Scope:** ISS-042, ISS-043, ISS-044, ISS-045 — all raised in Run #64. All 4 issues fixed end-to-end in Run #65. Run journal updated with Run #65 Run Log entry, Session Metrics row #31, and all 4 issues marked Resolved in the Open Issues table.

**Changes implemented (Run #65):**

| ISS | Severity | Fix summary | Files changed |
|-----|----------|-------------|---------------|
| ISS-043 | Critical | `QCNotes.cshtml` `value="@n.ID"` → `value="@n.QCNoteRef"` | `QCNotes.cshtml` |
| ISS-042 | High | Async `OnPostGoAsync` with `GetByIdAsync()` + status check + `GoError` + `govuk-error-summary` on all 3 batch list pages | `BatchesNotReceived.cshtml.cs`, `BatchesForEditing.cshtml.cs`, `BatchesForDispatch.cshtml.cs`, `BatchesNotReceived.cshtml`, `BatchesForEditing.cshtml`, `BatchesForDispatch.cshtml` |
| ISS-044 | High | `GetAllBatchesAsync()` added to interface/repo/service; `BatchesForEditingModel` switched from `GetInProgressAsync()` | `IBatchRepository.cs`, `BatchRepository.cs`, `BatchService.cs`, `BatchesForEditing.cshtml.cs` |
| ISS-045 | Medium | `GetAllAsync()` added to interface/repo/service; `QCNotesModel` uses `IsGlobalView` bool to route between global and batch-scoped load | `IQCNoteRepository.cs`, `QCNoteRepository.cs`, `QCNoteService.cs`, `QCNotes.cshtml.cs`, `QCNotes.cshtml` |

**Build:** 0 errors, 0 warnings. **Tests:** 90 pass, 1 skipped (integration), 0 fail.
