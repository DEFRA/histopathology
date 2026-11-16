---
description: Runs the end-to-end SIT journey in the running Histo.Web app using the integrated browser - create a submission, receive it, assign tissues to blocks, enter quality data and dispatch (which completes it), archive its block and tissue, and verify it in View submissions and Search. Data-creating; uses a unique SIT stamp. Can run the whole flow or a single stage.
tools: [open_browser_page, navigate_page, read_page, click_element, type_in_page, run_playwright_code, screenshot_page, run_in_terminal, get_terminal_output, read_file, grep_search]
---

# End-to-End SIT Agent

One submission is followed through the whole lifecycle. Each stage depends on the previous one, so run them in order. To run a single stage, reuse a submission id from an earlier run (ask the user for it).

## Running a subset

Run only what the user asks for, for example "run stages E2E-01 to E2E-04" or the whole file.

| Stage | ID prefix | Page |
|-------|-----------|------|
| 1. Create submission | E2E-01 to E2E-03 | `/Batches/Cassetted` |
| 2. Receive submission | E2E-04 to E2E-05 | `/Batches/BatchesNotReceived` |
| 3. Assign tissues to blocks | E2E-06 to E2E-08 | `/Batches/BatchesReceived` |
| 4. Enter quality data and dispatch | E2E-09 to E2E-10 | `/Batches/BatchesForDispatch` |
| 5. Archive block and tissue | E2E-11 to E2E-12 | `/Batches/BatchesForArchiving` |
| 6. Verify in View submissions and Search | E2E-13 to E2E-15 | `/Submissions/ViewSubmissions`, `/Search/SearchSubmissions` |

## Prerequisites

1. Start the app if `https://localhost:57879/` is not reachable: `dotnet run --project src\Histo.Web --launch-profile Histo.Web` (async terminal).
2. Local DB `(localdb)\MSSQLLocalDB`, database `histo`, with migrations from `src/Database/Migrations` applied.
3. Dev auth signs in as a valid local test persona. Default harness account: "Local Developer" (Group: Maintenance, Area: All Areas). This is a local convenience identity, not a production entitlement. The app-valid production combinations are `Customer -> External Customer` or `TB Diagnostics`, `Histopathology User -> Histopath`, and `Maintenance -> Other VLA`. For access-control or area-scoping cases, explicitly use the matching valid combination instead of assuming Maintenance/All Areas.

## Rules

- Use one stamp `<yyyyMMddHHmmss>` for the whole run. Customer ref `SIT<stamp>-E2E`; every comment field `SIT<stamp> <context>`.
- Only act on the submission created in this run. Never press Receive, Save or Archive on any other submission.
- Use a Wet Tissue submission (`SubmittedAs = 1`), Non-TSE (`BatchType = 1`), so the Assign tissues stage applies. Cassetted submissions never appear in Assign tissues.
- Always set PM dates to the previous day (`yyyy-MM-dd`).
- Follow "Completing a submission" in `tests/SIT/submissions.agent.md` for sample, tissue and Finish steps.
- A submission is listed for quality data only while its status is "In progress". Dispatching its test sets the status to "Completed" and the submission then appears in `/Batches/BatchesForArchiving`; do not change the status by hand.
- Select the new submission by number only (quick-go, or the list row whose text contains the id); never click the first Select in a list, as those rows are real data.
- Quick-go on `/Batches/BatchesForDispatch` keeps Quality data read-only (fields disabled, "opened for viewing") if `/Submissions/ViewSubmissions` was visited earlier in the session; use the row's Select button on the list instead.
- If the tab is not visible, Playwright `fill`/`click`/`check` time out; use `page.evaluate` to set values, dispatch `input`/`change` events and call `.click()` on the buttons.
- Reports are out of scope: do not open or compare the Print submission or Print submission notes PDFs. At every `/Batches/PrintSubmission` page (after Finish, Receive, Assign tissues) and in View/Search submissions after Select, only record whether "Print submission notes" is offered; it is expected on every one because the sample, tissue and block carry comments.
- `run_playwright_code` is much cheaper than snapshots for filling forms; list fields with `page.$$eval('main input:not([type=hidden]), main select, main textarea, main button', ...)`.
- Browser notes: navigate to link `href`s instead of clicking if the tab is not visible; a `PageNumber` past the last page returns the last page again. Use the quick-go box (`#QuickGoId`) to find the new submission instead of paging.
- On HTTP 500 or an unexpected "There is a problem" box, read the app terminal and include the exception, then stop the flow (later stages depend on it).
- If the session times out (`/SessionError`), navigate to `/` and continue.

---

## Stage 1: Create submission

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| E2E-01 | Choose type | Open `/Batches/Cassetted`; choose Non-TSE (`#batchType-nontse`) and Wet Tissue in `#SubmittedAs`; Next | `/Batches/BatchDetails?mode=create` opens |
| E2E-02 | Create batch | Select the first real option in `#Create_OtherSubmittedBy`, `#Create_OtherSubmittedArea`, `#Create_ProjectContractCode`, `#Create_ContactName`, `#Create_Fixation`, `#Create_SpeciesId`; set `#Create_BatchDateStr` to today; tick `#Create_SafeToHandle` and at least one histology type (e.g. `#create-histology-1`); fill `#Create_Comments`; click the final submit button | `/Submissions/SampleSummary?batchId=<id>`; record `<id>`. Without a histology type: "Select at least one histology type." |
| E2E-03 | Add sample and tissue | Add sample `SIT-E2E-01`, set PM date and comment, Save details; Edit sample, add a tissue (`#TissueCode`, `#NoPieces`, `#Comment`); return to summary and click Finish | Summary shows "<n> tissue(s)"; `/Batches/PrintSubmission` shows "Submission not received" and the submission number |

## Stage 2: Receive submission

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| E2E-04 | Listed as awaiting receipt | Open `/Batches/BatchesNotReceived`; quick-go the new id | The submission is listed with a Receive button |
| E2E-05 | Receive | Click Receive; choose `#status-received`; select `#TimeReceived` = "before 11.00" (required, else "Select the time the submission was received or rejected."); Done | `/Batches/PrintSubmission` shows "Submission received"; the submission leaves the awaiting-receipt list |

## Stage 3: Assign tissues to blocks

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| E2E-06 | Listed as received | Open `/Batches/BatchesReceived`; quick-go the new id | Listed with "All tissues assigned" = No |
| E2E-07 | Assign tissue | Click View (`/Batches/BatchBlocks`, "No blocks found" is expected); click "Add sample" and pick the sample in the "Sample associated" dropdown; on Sample blocks choose `#HistologyRefType` (e.g. General Pool) and "Get next ref"; click "Add block"; on `/Blocks/BlockDetails` fill `#NewCustomerRef` and `#NewComment`, choose `#NewTissueCode` and `#NewTissueNoPieces`, Add tissue, then Done | Sample blocks lists the block (e.g. 01, "1 tissue(s)") |
| E2E-08 | All assigned | On Sample blocks click Done, then Done on the Blocks page; reopen `/Batches/BatchesReceived` | The submission shows "All tissues assigned" = Yes |

## Stage 4: Enter quality data and dispatch

After all tissues are assigned the status is already "In progress", which is what makes the submission eligible.

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| E2E-09 | Quality data | Open `/Batches/BatchesForDispatch`; Select the new submission; tick one test `select-<id>`; set `#Result` = Passed (this is what enables dispatch), `#StainRef`, `#Comment` with a `SIT<stamp>` text; click "Update Selected" | Banner "Updated 1 test(s)."; the test row shows Passed |
| E2E-10 | Dispatch with QC | Reopen the submission from the list; tick the test; set `#Result` = Passed; tick `#Dispatched`; fill `#DispatchedTo`, `#DispatchedDate` (today), `#DispatchedBy` (first real user), `#NumberOfSlides` = 1, `#QCCode` (e.g. Air bubbles), `#RemedialAction` (e.g. Re-embed), at least one charge (`#charge-1`), tick `#QCNote` and fill `#Comment`; "Update Selected" | The test row shows Passed and Dispatched = Yes; View submissions shows status "Completed" with a completed date; the submission leaves `/Batches/BatchesForDispatch` and appears in `/Batches/BatchesForArchiving` |
| E2E-10a | QC note raised | Open `/QC/QCNotes` | A new QC note (highest ref) exists for the submission and the stain ref used; Dispatch does not leave the quality data page, unlike legacy which opens QC notes |

## Stage 5: Archive block and tissue

Dispatch does not archive anything itself; archiving is a separate step for the completed submission.

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| E2E-11 | Archive block | Open `/Batches/BatchesForArchiving`; "Archive blocks" on the row for the id; tick the block; `#ArchiveLocationCode` = Discard, `#ArchivedDate` (today), `#Comment`; "Update selected" | The block row shows the archived date and "Discard" |
| E2E-12 | Archive tissue | "Archive tissues" for the same submission (`/Archive/ArchiveTissues?batchId=<id>`); tick the tissue; same fields; "Update selected" | The tissue row shows the archived date and "Discard" |

## Stage 6: Verify

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| E2E-13 | View submissions | Open `/Submissions/ViewSubmissions`; fill `#SubmissionNumber`; Search | One row with the expected project code, species, received and completed dates and status "Completed" |
| E2E-14 | Search submissions | Open `/Search/SearchSubmissions?ReturnPage=%2FIndex`; fill `#SenderRef` with the sample's sender ref; Search (there is no customer-ref filter) | The submission is returned |
| E2E-15 | Data persisted | Select the row in Search submissions (View submissions Select stays on the same page) | Sample, block, tissue and comments match what was entered; mark SKIP if no detail page is reachable |

---

## Report

One table per stage run:

```
| ID | Result (PASS/FAIL/SKIP) | Evidence / failure detail |
```

Start with the created submission id, batch type, `SubmittedAs` and stamp. State which stages ran, mark later stages SKIP if an earlier one failed, and end with a one-line summary (`n passed, n failed, n skipped`) and any app-log exceptions seen.
