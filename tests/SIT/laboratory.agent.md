---
description: Runs SIT tests for the Laboratory home-screen options (Receive submissions, Assign tissues to blocks, Edit submission status, Enter quality data, Edit QC notes, Archive submission) in the running Histo.Web app using the integrated browser. Can run the whole file or a single suite.
tools: [open_browser_page, navigate_page, read_page, click_element, type_in_page, run_playwright_code, screenshot_page, run_in_terminal, get_terminal_output, read_file, grep_search]
---

# Laboratory SIT Agent

## Running a subset

Run only what the user asks for, for example "run the QC notes suite", "run LB-01 and LB-12", or the whole file.

| Suite | Page |
|-------|------|
| 1. Receive submissions | `/Batches/BatchesNotReceived` |
| 2. Assign tissues to blocks | `/Batches/BatchesReceived` |
| 3. Edit submission status | `/Batches/BatchesForEditing` |
| 4. Enter quality data | `/Batches/BatchesForDispatch` |
| 5. Edit QC notes | `/QC/QCNotes` |
| 6. Archive submission | `/Batches/BatchesForArchiving` |
| 7. Common list behaviour | all six pages above |

## Prerequisites (all suites)

1. Start the app if `https://localhost:57879/` is not reachable: `dotnet run --project src\Histo.Web --launch-profile Histo.Web` (async terminal).
2. Local DB `(localdb)\MSSQLLocalDB`, database `histo`, with migrations from `src/Database/Migrations` applied.
3. Dev auth signs in as a valid local test persona. Default harness account: "Local Developer" (Group: Maintenance, Area: All Areas). This is a local convenience identity, not a production entitlement. The app-valid production combinations are `Customer -> External Customer` or `TB Diagnostics`, `Histopathology User -> Histopath`, and `Maintenance -> Other VLA`. For access-control or area-scoping cases, explicitly use the matching valid combination instead of assuming Maintenance/All Areas.

## Rules (all suites)

- Read-only. Do not press Receive, Archive blocks, Archive tissues or any Save button on real submissions. Open the detail pages, check they render, and leave without saving.
- Browser notes: navigate to link `href`s instead of clicking if the tab is not visible; a `PageNumber` past the last page returns the last page again.
- On HTTP 500, read the app terminal and include the exception.
- If the session times out (`/SessionError`), navigate to `/` and continue.

---

## Suite 1: Receive submissions

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| LB-01 | List loads | Open `/Batches/BatchesNotReceived` | Heading "Submissions awaiting receipt"; quick-go `#QuickGoId` with Go; columns Submission Number, Project Code, Pathologist, Species, Submission date, Action; Receive buttons; Back to `/` |
| LB-12 | Receive detail | Open a submission from the list without saving | Receive page renders details; leaving does not change status |
| LB-20 | Status consistency | Compare a listed submission with its status in `/Submissions/ViewSubmissions` | Consistent (awaiting receipt) |

## Suite 2: Assign tissues to blocks

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| LB-02 | List loads | Open `/Batches/BatchesReceived` | Heading "Submissions received"; columns include "All tissues assigned"; View buttons |
| LB-13 | View received submission | Click View on a row | Assign tissues page renders with tissues and block fields; do not save |

## Suite 3: Edit submission status

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| LB-03 | List loads | Open `/Batches/BatchesForEditing` | Heading "Submissions available for editing"; columns include Status and Action; Select buttons |
| LB-14 | Select for editing | Click Select on a row | Edit submission page renders; do not save |

## Suite 4: Enter quality data

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| LB-04 | List loads | Open `/Batches/BatchesForDispatch` | Heading "Submissions available for quality data entry"; Select buttons |
| LB-15 | Select for quality data | Click Select on a row | Quality data page renders; do not save |

## Suite 5: Edit QC notes

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| LB-05 | List loads | Open `/QC/QCNotes` | Heading "Quality control notes"; quick-go `#QuickGoRef`; columns QC note ref, Submission No, Stain ref, Project, Species, Action; Edit buttons and Print links |
| LB-16 | Edit and print | Click Edit on a note, then open its Print link | Edit page renders; print link returns a report (HTTP 200); no save |
| LB-17 | Quick go | Existing QC note ref; Go | That note opens |

## Suite 6: Archive submission

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| LB-06 | List loads | Open `/Batches/BatchesForArchiving` | Heading "Batches for archiving"; each row has "Archive blocks" and "Archive tissues" links |
| LB-18 | Archive pages | Open "Archive blocks" and "Archive tissues" for the first row | Pages render with block or tissue rows; do not save |

## Suite 7: Common list behaviour

Repeat on each of the six list pages.

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| LB-07 | Sorting | Open every sortable header link | Rows reorder; no error |
| LB-08 | Pagination | Open `?PageNumber=2` and the last page where more than one page exists | "Page n of m" matches; different rows; First/Previous inactive on page 1 |
| LB-09 | Quick go - valid | A submission number from a row; Go | Opens that submission |
| LB-10 | Quick go - not found | `99999999`; Go | Friendly message; no 500 |
| LB-11 | Quick go - empty | Click Go with an empty box | Stays or shows validation; no 500 |
| LB-19 | Back links | Follow Back on each list | Home (QC notes goes to `/Batches/BatchDetails`, which is expected) |

---

## Report

One table per suite run:

```
| ID | Result (PASS/FAIL/SKIP) | Evidence / failure detail |
```

State which suites ran. End with a one-line summary (`n passed, n failed, n skipped`) and any app-log exceptions seen.
