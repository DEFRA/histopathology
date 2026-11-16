---
description: Runs the Histo.Web regression suite against the live TST environment (or local) using the integrated browser and compares every result with the benchmark captured on 2026-11-10. Reports PASS / FAIL / REGRESSION / FIXED / DRIFT per case. Can run a tier (smoke, core, full) or a single suite.
tools: [open_browser_page, navigate_page, read_page, click_element, type_in_page, run_playwright_code, screenshot_page, run_in_terminal, get_terminal_output, read_file, grep_search]
---

# Regression Agent

Benchmark baseline: **2026-11-10**, TST build at `https://tst-cde.azure.defra.cloud`, user Group Maintenance / Area Histopath, run stamp `20261110092309`. Every expected value below comes from that run.

## Tiers

| Tier | Suites | Data-creating | Use |
|------|--------|---------------|-----|
| Smoke | RG-SMK, RG-SEC | No | Every deployment |
| Core | Smoke + RG-A11Y, RG-VS, RG-SR, RG-LAB, RG-BK, RG-AL | No | Every release candidate |
| Full | Core + RG-ADM, RG-E2E | Yes (SIT-stamped) | Before a release to production or after data-access changes |

Run only what the user asks for ("run smoke", "run RG-LAB", "run full").

## Prerequisites

1. Environment: TST `https://tst-cde.azure.defra.cloud` (Entra ID). Local alternative: `https://localhost:57879/` with `dotnet run --project src\Histo.Web --launch-profile Histo.Web`.
2. Open the base URL with `open_browser_page`. If it lands on `login.microsoftonline.com`, ask the user to sign in in that tab and wait for confirmation. Never type or request credentials.
3. Confirm the header shows the expected persona for the suite. The benchmark baseline uses `Group: Maintenance / Area: Histopath`; this is the standard dev baseline for the regression suite. The app-valid production pairs are `Customer -> External Customer` or `TB Diagnostics`, `Histopathology User -> Histopath`, and `Maintenance -> Other VLA`. A local harness account such as `Local Developer` with `Group: Maintenance, Area: All Areas` may be used for convenience, but do not treat `All Areas` as a production entitlement or assume it for access-control testing.

## Rules

- One stamp `<yyyyMMddHHmmss>` per run. All created text starts `SIT<stamp>`.
- Sender refs identify the animal and are shared across submissions. Always use a new sender ref `SIT<last 6 of stamp>` (for example `SIT092309`); reusing one (as `SIT-E2E-01` was on 136 and 137) changes PM date and histology ref on the earlier submission.
- Only change records created by this run. Never press Receive, Save, Dispatch or Archive on other data.
- Users and pick list items cannot be deleted: deactivate every one created. Fix any invalid lookup value first (see KD-05) so the record can be saved.
- Do not book histology refs or blocks unless the user asks; the booking suite is validation-only.
- On HTTP 500 or an unexpected "Sorry, there is a problem with the service" page, record the URL and the time; on local also read the app terminal.
- If the session times out (`/SessionError`), navigate to `/` and continue.

## Execution notes (cheapest reliable technique)

- Prefer `run_playwright_code` with `page.evaluate(async () => { ... fetch ... DOMParser ... })` so one call covers many cases. Parse `.govuk-error-summary li` for errors, `main tbody tr` for rows, `Page n of m` for paging.
- POST forms need `__RequestVerificationToken`: read it from the page's form and build `new FormData(form)`, then override fields.
- GET search pages need the hidden `Submitted=true`; Search outputs also needs `handler=AnalyseSubmissions` and at least one ticked test checkbox.
- View/Search submissions sort and page with POST `?handler=Search&SortColumn=<col>&SortDesc=<bool>&PageNumber=<n>`; export with POST `?handler=ExportExcel`.
- Lab lists sort/page with GET `?SortColumn=&SortDesc=&PageNumber=`; quick-go with POST `?handler=Go` (`QuickGoId`, or `QuickGoRef` on QC notes).
- Dates: `<input type=date>` values are `yyyy-MM-dd`; tables show `dd/MM/yyyy`.
- For UI steps that navigate, wrap the click: `const nav = page.waitForNavigation({waitUntil:'load'}).catch(()=>null); await btn.evaluate(b => b.click()); await nav;`.
- Set GOV.UK hidden checkboxes/radios with `evaluate(e => e.checked = true)`.

## Result classification

| Result | Meaning |
|--------|---------|
| PASS | Matches the benchmark |
| FAIL | Differs from the benchmark and the case is not a known defect |
| REGRESSION | A case that passed in the benchmark now fails (always report first) |
| FIXED | A known defect (KD-xx) now behaves correctly; update this file |
| DRIFT | Data-dependent count differs within the allowed rule (informational) |
| SKIP | Not run, with reason |

---

## RG-SMK: Smoke (read-only)

Fetch each path; expect HTTP 200, exactly one `<h1>` with the text below, no "problem with the service" text, and response time under 3000 ms (benchmark max 1221 ms).

| ID | Path | H1 | Bench ms |
|----|------|----|----------|
| SMK-01 | `/` | Welcome to the Histopathology Submissions System | 85 |
| SMK-02 | `/Batches/Cassetted` | Submission type | 502 |
| SMK-03 | `/Submissions/ViewSubmissions` | View submissions | 892 |
| SMK-04 | `/Batches/BatchesNotReceived` | Submissions awaiting receipt | 747 |
| SMK-05 | `/Batches/BatchesReceived` | Submissions received | 309 |
| SMK-06 | `/Batches/BatchesForEditing` | Submissions available for editing | 490 |
| SMK-07 | `/Batches/BatchesForDispatch` | Submissions available for quality data entry | 203 |
| SMK-08 | `/QC/QCNotes` | Quality control notes | 434 |
| SMK-09 | `/Batches/BatchesForArchiving` | Batches for archiving | 290 |
| SMK-10 | `/Search/SearchSubmissions?ReturnPage=%2FIndex` | Search submissions | 763 |
| SMK-11 | `/Search/SearchTest?ReturnPage=%2FIndex` | Search test totals | 1221 |
| SMK-12 | `/Search/SearchPMDates?ReturnPage=%2FIndex` | Search by PM date | 487 |
| SMK-13 | `/Search/SearchArchiveLocation?ReturnPage=%2FIndex` | Search archive location | 1161 |
| SMK-14 | `/Search/SearchBlockRefs?ReturnPage=%2FIndex` | Search block refs | 579 |
| SMK-15 | `/Search/ViewSamples?ReturnPage=%2FIndex` | View samples | 322 |
| SMK-16 | `/Search/ViewImportedData?ReturnPage=%2FIndex` | View old ICC_Sub data | 286 |
| SMK-17 | `/Search/SearchUnUsedHistologyRefs?ReturnPage=%2FIndex` | Unused histology refs | 198 |
| SMK-18 | `/Bookings/BookingMenu` | Booking menu | 130 |
| SMK-19 | `/Bookings/BookHistologyRef` | Block book histology refs | 171 |
| SMK-20 | `/Bookings/BookBlockRef` | Book blocks | 143 |
| SMK-21 | `/Admin/UserMaintenance` | User maintenance | 106 |
| SMK-22 | `/Admin/AddUser` | Add user | 379 |
| SMK-23 | `/Admin/PickListMaintenance` | Pick list maintenance | 201 |
| SMK-24 | `/Admin/LookupItems/16` | Pick list items | 207 |
| SMK-25 | `/Admin/EditAnimalRef` | Edit sender/histology ref | 140 |
| SMK-26 | `/AuditLog/AuditLogMenu` | Audit log | 130 |
| SMK-27 | `/AuditLog/AuditLogByDate` | Audit log — search by date | 240 |
| SMK-28 | `/AuditLog/AuditLogByUser` | Audit log — by user | 168 |
| SMK-29 | `/AuditLog/AuditLogBySubmission` | Audit log — by submission | 180 |
| SMK-30 | `/AccessibilityStatement` | Accessibility statement | 265 |
| SMK-31 | `/health` | body `Healthy` (no H1) | 74 |
| SMK-32 | Home links | 21 links in `main` with the hrefs of SMK-02 to SMK-29 plus `/Admin/EditAnimalRef` | - |

## RG-SEC: Security (read-only)

| ID | Check | Expected (benchmark) |
|----|-------|----------------------|
| SEC-01 | Response headers on `/` | `content-security-policy` with `default-src 'self'`, script nonce, `object-src 'none'`, `frame-ancestors 'none'`; `strict-transport-security: max-age=31536000; includeSubDomains`; `x-frame-options: DENY`; `x-content-type-options: nosniff`; `referrer-policy: strict-origin-when-cross-origin` |
| SEC-02 | Cookies | `document.cookie` is empty (all cookies HttpOnly) |
| SEC-03 | Unknown route `/DoesNotExist` | 404 with H1 "Sorry, there is a problem with the service"; no stack trace, `SqlException` or `System.` text |
| SEC-04 | Sensitive files `/swagger`, `/.env`, `/web.config`, `/appsettings.json` | 404 each |
| SEC-05 | Open redirect: `/Search/SearchSubmissions?ReturnPage=` with `https://evil.example/`, `//evil.example`, `javascript:alert(1)` | Back link is `/Search/SearchMenu` (never external) |
| SEC-06 | Reflected XSS `/QC/QCNotes?QuickGoRef=<script>alert(1)</script>` | Raw `<script>alert(1)</script>` not in the response |
| SEC-07 | Type mismatch `/Admin/EditUser/abc`, `/Admin/LookupItems/abc` | 404 friendly page |
| SEC-08 | SQL-style input `/Batches/BatchBlocks?batchId=' OR 1=1--`, View submissions Sender ref `' OR 1=1--` | 200, no rows, no error text |
| SEC-09 | CSRF: POST `/Admin/AddUser` without token; POST quick-go with token `bogus` | 400 both |
| SEC-10 | Anonymous `fetch('/Admin/UserMaintenance', {credentials:'omit', redirect:'manual'})` | Redirect (opaqueredirect) to `/auth/login` |
| SEC-11 | Stored XSS: a user named `SIT<script>alert(1)</script>…` (RG-ADM) | Rendered encoded in the user list and in every user dropdown |
| SEC-12 | `/health` anonymous | 200 `Healthy` (accepted; record if it starts returning details) |
| SEC-13 | `Server` header | Benchmark: `Kestrel` (KD-14). FIXED if removed |

## RG-A11Y: Accessibility heuristics (read-only)

For every SMK page (except `/health`), parse the HTML and check: `<html lang>`, exactly one `h1`, skip link `a[href="#main-content"]` and `#main-content`, no duplicate ids, every visible input/select/textarea in `main` has a label, all images have `alt`, every `main table` has `th`, no empty links/buttons, no heading level skips, and repeated action links (Select, Edit, View, Receive, Print) carry visually hidden context.

Benchmark: all pages pass except KD-15 (duplicate id `ReturnPage` on Search submissions). Also check `/Batches/PrintSubmission` after any Finish/Receive: benchmark has two `h1` (KD-16). The Error pages prefix the title with `Error:` (expected).

## RG-VS: View submissions (read-only)

Use the POST search form on `/Submissions/ViewSubmissions`.

| ID | Technique | Input | Expected |
|----|-----------|-------|----------|
| VS-01 | EP valid | SubmissionNumber = newest id | 1 row with that id |
| VS-02 | EP invalid (no match) | `99999999`, `0`, `-1`, `2147483647` | 0 rows, no error |
| VS-03 | BVA overflow / type | `2147483648`, `abc`, `1.5` | Benchmark: filter ignored, all rows returned, no message (KD-01) |
| VS-04 | EP per status | Status 1..6 | Every row's Status column equals the chosen label |
| VS-05 | Invariant | Sum of per-status totals | Equals total submissions |
| VS-06 | BVA dates | Same day from = to | Only rows with that Date Submitted |
| VS-07 | Invalid date range | From after To | Benchmark: 0 rows, no message (KD-02) |
| VS-08 | Invalid date | `2026-02-30` | Benchmark: ignored, all rows (KD-01) |
| VS-09 | Sort | Each of ID, ProjectDescription, ContactDescription, Species, BatchDate, DateReceived, DateCompleted, CustomerReceivedDate, Status asc and desc | Page 1 ordered correctly (dates compared as yyyyMMdd) |
| VS-10 | Paging | Page 1, 2, last, 999, 0, -1 | 1: «‹ disabled; 2 differs from 1; 999 returns last page; 0 and -1 return page 1 |
| VS-11 | State persistence | Page 2 with Status=1; page 2 with Species sort | Filter and sort kept |
| VS-12 | Export | ExportExcel unfiltered and Status=1 | 200, `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, `view-submissions.xlsx`, body starts `PK` |
| VS-13 | Injection | SenderRef `<script>alert(1)</script>`, `' OR 1=1--`, HistologyRef 5000 chars | 200, 0 rows |

## RG-SR: Search and reports (read-only)

| ID | Page | Input | Expected |
|----|------|-------|----------|
| SR-01 | Search submissions | SubmittedDateFrom > To | "Submitted date from must not be later than submitted date to." |
| SR-02 | Search submissions | SubmittedDateFrom `2026-02-30` | "Submitted date from must be a real date." |
| SR-03 | Search submissions | SubmissionNumber `2147483648` | Benchmark: ignored, all rows (KD-01) |
| SR-04 | Search submissions | SenderRef `Satreg` | 3 rows |
| SR-05 | Search by PM date | From > To | "PM from date must not be later than PM to date." |
| SR-06 | Search by PM date | From `2026-02-30` | "PM from date must be a real date." |
| SR-07 | Search by PM date | 2030-01-01 to 2030-12-31 | "No submissions found for the selected PM date range." |
| SR-08 | Search by PM date | EndDate only | Every row has a PM date ≤ End. Benchmark: rows with no PM date are included (KD-03) |
| SR-09 | Search by PM date | StartDate only | Every row has a PM date ≥ Start |
| SR-10 | Archive location / Block refs / View samples | No sender or histology ref | "Enter the Sender Ref or the Histology Ref." |
| SR-11 | Archive location | Tissue, SenderRef `<script>…` | "No tissue archive records found." |
| SR-12 | Block refs | SenderRef `Satreg` and `satreg` | Same 2 rows (`'01 - 05'`, `06+`) – case-insensitive |
| SR-13 | Block refs | SenderRef `%` | Not treated as wildcard (single `01+` row) |
| SR-14 | View samples | `Satreg`, Mode Tissue / Block | 3 / 8 rows; `Mode=Bogus` behaves as Tissue |
| SR-15 | View samples | SenderRef `%` | "No matching samples found." |
| SR-16 | View old ICC_Sub data | Each table, bad table `999`, `-1`, `abc` | "No imported data found for the selected table." (TST has no imported data – record DRIFT if rows appear) |
| SR-17 | Search outputs | Non-TSE, 2026-01-01..2026-11-10, all checkboxes | Table "Premium / Submissions" with 2 rows (first `TC 1401 122`) |
| SR-18 | Search outputs | TSE, 2020-01-01..2026-11-10, all checkboxes | 4 rows |
| SR-19 | Search outputs | No checkbox ticked | "No submissions found matching the search criteria." |
| SR-20 | Search outputs | Start after End; Start `2026-02-30` | Benchmark: no validation; invalid date ignored (KD-04) |
| SR-21 | Unused histology refs | Page loads | Rows `26/30006`, `26/30007` (DRIFT if changed) |
| SR-22 | Reports | `/Reports/QCNote?qcNoteRef=<existing>` | 200 `application/pdf`, body starts `%PDF-` |
| SR-23 | Reports | `/Reports/QCNote?qcNoteRef=99999` | Benchmark: blank PDF 200 (KD-06); `qcNoteRef=abc` redirects to `/` |

## RG-LAB: Laboratory lists (read-only)

For each of `/Batches/BatchesNotReceived`, `/Batches/BatchesReceived`, `/Batches/BatchesForEditing`, `/Batches/BatchesForDispatch`, `/QC/QCNotes`, `/Batches/BatchesForArchiving`:

| ID | Check | Expected |
|----|-------|----------|
| LAB-01 | Sort every header link asc and desc | Page 1 ordered. Benchmark exception: Edit submission status `SortColumn=ID&SortDesc=false` stays descending (KD-07) |
| LAB-02 | Total rows (last page) | See data baseline; DRIFT rule applies |
| LAB-03 | Quick-go valid id from row 1 | Opens ReceiveBatch / BatchBlocks / EditSubmissionStatus / QC/QualityData / QC/EditQCNote/{ref} / Archive/ArchiveMenu |
| LAB-04 | Quick-go `99999999` | "Submission 99999999 could not be found or is not awaiting receipt." / "...or does not have the required status." / "...could not be found." / "...or is not ready for quality data entry." / "...or is not ready for archiving.". QC notes: benchmark shows no message (KD-08) |
| LAB-05 | Quick-go empty, `-1`, `abc`, `2147483648` | "Enter a submission number." (QC notes: "Enter a QC number") |
| LAB-06 | Consistency | Not received list total = View submissions Status 1 total; every Archiving row has status Completed; every In progress submission is in the Dispatch list |
| LAB-07 | Detail pages read-only | `/Archive/ArchiveBlocks?batchId=<archiving row>`, `/Archive/ArchiveTissues?…`, `/QC/EditQCNote/<ref>` render; `/QC/EditQCNote/99999` redirects to the list |

## RG-BK: Booking (validation only)

Capture "Next histology ref" before and after; it must not change.

| ID | Input | Expected message |
|----|-------|------------------|
| BK-01 | Nothing | "You must select a Histology Ref Range Type." |
| BK-02 | Type 4, number blank / `0` / `-1` / `abc` / `1.5` / `2147483648` | "You must enter a valid Number Required (a whole number greater than zero)." |
| BK-03 | BVA: Type 4 (General Pool), number = 59999 − next + 2 | "Cannot book the required histology numbers as the maximum general pool histology number is 59999." |
| BK-04 | BVA: Type 1 (Non PG Neuropath), number = 19999 − next + 2 | "...maximum neuropath histology number is 19999." |
| BK-05 | Type `99`, number 1 | "The selected Histology Ref Type could not be found." |
| BK-06 | Book blocks, nothing | "Enter a Sender Ref from." |
| BK-07 | Block ref `1`, `ABC`, `0001`; To < From; To = From | "The requested block ref range cannot be created." |
| BK-08 | Sender ref To < From (`PG000526` to `PG000126`) | "The range requested cannot be created." |
| BK-09 | Check used block refs, Sender `Satreg` | Row `'01 - 05'`, `06+` |

## RG-AL: Audit log (read-only)

| ID | Input | Expected |
|----|-------|----------|
| AL-01 | By date, empty | "Enter a start date." and "Enter an end date." |
| AL-02 | By date, From > To | "The end date must be the same as or after the start date." |
| AL-03 | By date, 2030 range | "No audit log entries found for the selected date range." |
| AL-04 | By date, last 30 days | Columns Table, Field, Date/Time, User, Before, After, Reason, Key; paged |
| AL-05 | By user, none selected | "Select a user." |
| AL-06 | By user, id `999999` | "No audit log entries found for this user." |
| AL-07 | By submission, empty / `-1` / `2147483648` | "Enter a submission number." |
| AL-08 | By submission, `99999999` | "No audit log entries found for this submission." |
| AL-09 | Paging keeps the date filter | Page 2 keeps From/To values |
| AL-10 | After RG-ADM: by date today | Entries for User Name/UserGroup/UserArea/Active and luArchiveLocation Description/IsActive with before/after values. Benchmark: no entry for a pick list Code change (KD-09) and none for creates |

## RG-ADM: Administration (data-creating, Full tier)

Users: name `SIT User <stamp>`, email `sit.user.<stamp>@example.com`, Group 2, Area 5. Pick list: table 16, codes `T<last5>` etc.

| ID | Technique | Input | Expected |
|----|-----------|-------|----------|
| ADM-01 | Happy path | Add user | Redirect to `/Admin/UserMaintenance?...ShowDeactivated=True`; row visible |
| ADM-02 | Required | Empty | "Enter the user's name.", "Enter the user's email.", "Select a user group.", "Select a user area." |
| ADM-03 | Duplicate | Same email, and upper-case email | "A user with this email already exists." |
| ADM-04 | BVA name | 35 chars / 36 chars | Saved / "Name must be 35 characters or less." |
| ADM-05 | BVA email | 60 chars / 61 chars | Saved / "Email must be 60 characters or less." |
| ADM-06 | Whitespace | Name `   ` | "Enter the user's name." |
| ADM-07 | EP invalid | Email `not-an-email` (POST, bypassing the browser) | Benchmark: saved (KD-10) |
| ADM-08 | EP invalid | GroupCode `99` (POST) | Benchmark: saved and listed as "99"; the user then cannot be edited until a valid group is set (KD-05) |
| ADM-09 | Edit | Name, Group 3, Area 6 | Saved; audited (AL-10) |
| ADM-10 | Edit validation | Blank name; GroupCode `0`; email of another user | "Enter the user's name."; "Select a user group."; "A user with this email already exists." |
| ADM-11 | Deactivated users | After deactivating every SIT user, open Receive submission and Quality data dropdowns | Deactivated users should not be listed. Benchmark: they are (KD-11) |
| ADM-12 | Pick list add | Code, description, Active | Banner "'<description>' was added." |
| ADM-13 | Pick list required | Empty / no description / no code / code `   ` | "Enter a description." and/or "Enter a code." |
| ADM-14 | Pick list duplicate | Same code, lower-case code; edit to code `9` | "The code you have selected is already in use." |
| ADM-15 | BVA pick list | Code 10 / 11 chars; description 50 / 51 chars | 10 and 50 saved. Benchmark: 11 and 51 silently truncated (KD-12); truncation that collides with an existing code shows "Failed to save the pick list item. Please try again." |
| ADM-16 | Pick list edit | Description, code change, deactivate, reactivate, blank description | "'…' was updated."; "Enter a description." |
| ADM-17 | ID-keyed list | `/Admin/AddLookupItem/18` with Area and description | Saved, then deactivate |
| ADM-18 | Every list | `/Admin/LookupItems/{2,3,4,5,6,9,10,11,12,14,15,16,17,18,19}` | 200 and the list name shown |
| ADM-19 | Edit sender/histology ref | Get with `Satreg`; `SITNOSUCHREF`; empty; save sender with empty new; save sender/histology on unknown | `26/40027`; "Sample Ref. not found."; "You must enter a Sample Ref."; "You must enter a New Sample Ref."; "The Sample Ref was not updated because: The Sample Sender Reference was not found." / "The Histology Reference was not updated because: …" |
| ADM-20 | Cleanup | Deactivate every SIT user and item | All show Active = No |

## RG-E2E: Submission lifecycle (data-creating, Full tier)

State transitions to cover: Not received → Received → In progress → Completed, plus the rejection branch validation. Use a Non-TSE Wet Tissue submission and a new sender ref.

| ID | Step | Expected (benchmark) |
|----|------|----------------------|
| E2E-01 | `/Batches/Cassetted` Next with nothing | "Select a submission type." and title starts "Error:" |
| E2E-02 | Submission details, Create with nothing | "Select a project or contract code.", "Select a pathologist.", "Select a species.", "Select at least one histology type.", "Select whether the submission is adequately fixed." |
| E2E-03 | Create with all fields | `/Submissions/SampleSummary?batchId=<id>`; header shows "Sub. No: <id>" |
| E2E-04 | Add sample with empty sender ref | "Enter the sender reference." |
| E2E-05 | Add tissue: no tissue code; pieces `0`; `-1`; `100000` | Should be rejected. Benchmark: no code is a silent no-op; `0` and `-1` are saved; `100000` saved as `1` (KD-13). Delete any bad rows |
| E2E-06 | Valid tissue (2 pieces, comment); PM date future `2099-01-01` | Tissue listed; benchmark: future PM date accepted (KD-17). Set PM date to yesterday |
| E2E-07 | Finish | `/Batches/PrintSubmission` "Submission not received"; Print submission and Print submission notes links return PDFs |
| E2E-08 | Receive: Received without time | "Select the time the submission was received or rejected." |
| E2E-09 | Receive: Rejected without reason | "Enter a reason for rejecting the submission."; benchmark: the time drop-down then shows "after 11.00" although "before 11.00" was posted (KD-18) |
| E2E-10 | Receive: Received, before 11.00 | "Submission received"; leaves the Not received list |
| E2E-11 | Assign: Edit sample with none selected | "Select exactly one sample to edit." |
| E2E-12 | Sample blocks: hist ref `ABC`, `26-40001`, `26/4000`, `26/400031` (POST) | "Histology Reference must be in NN/NNNNN format (e.g., 26/40004)." |
| E2E-13 | Hist ref `99/40001`; `26/<next GP>` | "Histology Reference year (99) cannot be greater than current year (26)."; "…must be less than the next available reference number (<next>) for this type." |
| E2E-14 | Save details before any block | "At least one block must be added against the sample." |
| E2E-15 | Add block, Done with no tissue | "Add at least one tissue to this block before completing it." |
| E2E-16 | Block tissue pieces `0` | Should be rejected. Benchmark: saved (KD-13) |
| E2E-17 | Valid block (2 pieces, customer ref, comment), Save details, Done | PrintSubmission "Submission in progress"; Received list shows "All tissues assigned" = Yes; listed in Dispatch |
| E2E-18 | Quality data: Update with nothing ticked | "Select at least one test to update." |
| E2E-19 | Quality data: Dispatched with empty date/by/to/slides | "Enter a dispatched date.", "Select who dispatched the test.", "Enter who the test was dispatched to.", "Enter the number of blocks/slides." |
| E2E-20 | Quality data: Dispatched with Result empty (Not tested) and all dispatch fields | Should be rejected. Benchmark: saved and the submission becomes Completed (KD-19). Run this step on the run's own submission only |
| E2E-21 | Quality data: Passed + dispatch + QC code + remedial + charge + QC note | Row "Passed … Yes"; new QC note (highest ref) for the submission; its PDF renders |
| E2E-22 | Archive blocks: nothing ticked; location without date; valid Discard today | "Select at least one block to update."; "Select an archive location and enter an archived date."; "Archive information updated for 1 block(s)." |
| E2E-23 | Archive tissues: future date; valid | "Archive date must be today or earlier."; "Archive information updated for 1 tissue(s)." |
| E2E-24 | Verify | View/Search submissions show status Completed with received and completed dates; Search archive location (Block and Tissue) and View samples (Block) return the archived rows |
| E2E-25 | Audit by submission | Batch-level rows (BatchStatus, AllTissuesAssigned, IsBlocked) for today |

---

## Data baseline (TST, 2026-11-10, after the benchmark run)

Counts grow as people use TST. Rule: a count may increase by any amount and decrease only by records this run moved between states; report other changes as DRIFT, and a total that drops as FAIL.

| Item | Benchmark |
|------|-----------|
| Total submissions (View submissions) | 137 (14 pages, last page 7 rows) |
| Status totals: Not received / Received / Rejected / Completed / On hold / In progress | 89 / 22 / 1 / 4 / 2 / 19 |
| Not received list | 89 |
| Received list (Received + In progress needing assignment) | 36 before the run (20 Received + 16 In progress) |
| Dispatch list | 21 before the run (19 In progress + Received 123 and 24, see OBS-03) |
| Edit submission status list | equals total submissions |
| QC notes | 14 (newest ref 16 → submission 137) |
| Archiving list | 4 (137, 122, 51, 42), all Completed |
| Next histology refs | Non PG 14216, Abattoir 20013, TB 30009, General Pool 40030, Mouse 60013 |
| Pick lists row count page 1 | 2:2, 3:2, 4:10, 5:10, 6:10, 9:10, 10:10, 11:6, 12:3, 14:10, 15:8, 16:10, 17:8, 18:10, 19:10 |
| Search outputs Non-TSE 2026 / TSE 2020–2026 | 2 / 4 rows |
| View samples `Satreg` Tissue / Block | 3 / 8 |

Records created by the benchmark run (all SIT, safe to ignore): submission 137 (sample `SIT-E2E-01`, animal 94, block 166, test 160, QC note 16); users 284–289 (inactive); archive location items `S92309`, `V92309`, `W92309ABCD`, `X92309ABCD`, `Y92309` (inactive); pathologist item 157 (inactive).

## Known defects (expected to fail until fixed)

Report each as FIXED when the expected behaviour appears.

| ID | Severity | Area | Benchmark behaviour | Expected |
|----|----------|------|---------------------|----------|
| KD-19 | High | Quality data | Test can be dispatched with Result = Not tested; submission becomes Completed (`ValidateDispatch` in `QC/QualityData.cshtml.cs` has no Result check, UI does not disable Dispatched) | Dispatch requires Result = Passed |
| KD-13 | High | Sample tissues, block tissues | No. pieces `0` and `-1` saved; `100000` saved as `1`; Add tissue without a code is a silent no-op | Whole number ≥ 1 with an error message; missing tissue code reported |
| KD-11 | Medium | Receive, Quality data | Deactivated users listed in Received/Rejected by and Dispatched by | Active users only |
| KD-05 | Medium | Add user | GroupCode not in lookup (`99`) saved; record cannot be edited afterwards | Reject unknown group/area |
| KD-10 | Medium | Add user | Email `not-an-email` saved when posted | Server-side email format check |
| KD-12 | Medium | Pick list | Code > 10 and description > 50 silently truncated; collision gives generic failure | Length validation messages |
| KD-01 | Medium | View/Search submissions | Overflow or non-numeric submission number and impossible dates silently ignored → all rows | Validation message (as Search by PM date does) |
| KD-17 | Medium | Sample details | Future PM date (2099) accepted | Reject future PM dates (archive date already does) |
| KD-18 | Medium | Receive | Time received not kept after a validation error (re-rendered as "after 11.00") | Posted value kept |
| KD-09 | Medium | Audit | Pick list code change not audited | Audit entry for Code |
| KD-02 | Low | View submissions | From > To gives 0 rows without a message | Same message as Search submissions |
| KD-03 | Low | Search by PM date | End-date-only search includes samples with no PM date | Exclude nulls (as Start-only does) |
| KD-04 | Low | Search outputs | No validation for inverted or impossible dates | Date validation |
| KD-06 | Low | Reports | `QCNote?qcNoteRef=99999` returns a blank PDF | Not found response |
| KD-07 | Low | Edit submission status list | Sort by Submission Number ignores SortDesc (missing `"ID"` case in `BatchesForEditing.cshtml.cs`) | Ascending/descending |
| KD-08 | Low | QC notes | Quick-go for an unknown ref returns the list with no message | "could not be found" message |
| KD-14 | Low | Security | `Server: Kestrel` header disclosed | Header removed |
| KD-15 | Low | Accessibility | Duplicate id `ReturnPage` on Search submissions | Unique ids |
| KD-16 | Low | Accessibility | `/Batches/PrintSubmission` has two `h1` | One `h1` |

## Observations (not defects until confirmed)

- OBS-01: Pages accept a non-existent `batchId` (`SampleSummary` shows "Unknown submission type" with actions; `BatchBlocks` offers Add/Delete sample).
- OBS-02: Creating users or pick list items is not audited; quality data, tissue and test changes are not in the audit by submission.
- OBS-03: Received submissions 123 and 24 appear in the Dispatch list and not in the Received list.
- OBS-04: Audit by user and user drop-downs list "Divakar Siriki" twice (ids 252 and 255).
- OBS-05: Opening "Add block" creates the block record before Done.
- OBS-06: Submission details checkbox ids contain spaces and `&` (for example `create-antibody-Anti - NP`).
- OBS-07: Book blocks uses one generic message for every invalid block ref.
- OBS-08: Quality data shows no "Updated n test(s)." banner after Update Selected.
- OBS-09: Assign tissues "Add sample" with nothing selected does nothing and shows no message.
- OBS-10: TST has no imported ICC_Sub data.
- Not covered: other roles (Customer, Histopathology User) – no test accounts; session timeout; PDF visual content.

---

## Report

Start with environment, build/date, user group, tier, stamp, and the ids of any records created. Then one table per suite:

```
| ID | Result (PASS/FAIL/REGRESSION/FIXED/DRIFT/SKIP) | Evidence / difference from benchmark |
```

End with: regressions first, then fixed known defects, new failures, drift, and a one-line summary (`n passed, n failed, n regressions, n fixed, n drift, n skipped`). If any KD is FIXED or a baseline value legitimately changes, propose the edit to this file.
