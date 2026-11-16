---
description: Runs SIT tests for the Search and reports home-screen options (Search submissions, Search outputs, Search by PM date, Search archive locations, Search block refs, View samples, View old ICC_Sub data, Unused histology refs) in the running Histo.Web app using the integrated browser. Can run the whole file or a single suite.
tools: [open_browser_page, navigate_page, read_page, click_element, type_in_page, run_playwright_code, screenshot_page, run_in_terminal, get_terminal_output, read_file, grep_search]
---

# Search and Reports SIT Agent

Audit logs have their own agent (`audit-log.agent.md`).

## Running a subset

Run only what the user asks for, for example "run the View samples suite", "run SR-09 to SR-11", or the whole file.

| Suite | Page |
|-------|------|
| 1. Search submissions | `/Search/SearchSubmissions?ReturnPage=%2FIndex` |
| 2. Search outputs | `/Search/SearchTest?ReturnPage=%2FIndex` (heading "Search test totals") |
| 3. Search by PM date | `/Search/SearchPMDates?ReturnPage=%2FIndex` |
| 4. Search archive locations | `/Search/SearchArchiveLocation?ReturnPage=%2FIndex` |
| 5. Search block refs | `/Search/SearchBlockRefs?ReturnPage=%2FIndex` |
| 6. View samples | `/Search/ViewSamples?ReturnPage=%2FIndex` |
| 7. View old ICC_Sub data | `/Search/ViewImportedData?ReturnPage=%2FIndex` |
| 8. Unused histology refs | `/Search/SearchUnUsedHistologyRefs?ReturnPage=%2FIndex` |
| 9. Common | all pages above |

## Prerequisites (all suites)

1. Start the app if `https://localhost:57879/` is not reachable: `dotnet run --project src\Histo.Web --launch-profile Histo.Web` (async terminal).
2. Local DB `(localdb)\MSSQLLocalDB`, database `histo`, with migrations from `src/Database/Migrations` applied.
3. Dev auth signs in as a valid local test persona. Default harness account: "Local Developer" (Group: Maintenance, Area: All Areas). This is a local convenience identity, not a production entitlement. The app-valid production combinations are `Customer -> External Customer` or `TB Diagnostics`, `Histopathology User -> Histopath`, and `Maintenance -> Other VLA`. For access-control or area-scoping cases, explicitly use the matching valid combination instead of assuming Maintenance/All Areas.

## Rules (all suites)

- Read-only: these pages only search and display.
- Derive search values from data on screen (for example a submission number from the first row) instead of hard-coding them.
- Browser notes: set `<input type=date>` with `fill('yyyy-mm-dd')`; if the tab is not visible click via `evaluate(b => b.click())`; a `PageNumber` past the last page returns the last page again.
- On HTTP 500 or an unexpected "There is a problem" box, read the app terminal and include the exception.
- If the session times out (`/SessionError`), navigate to `/` and continue.

---

## Suite 1: Search submissions

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| SR-02 | Form | Open page | Fields: Submission number, Status, Project or contract code, Pathologist, Species, Fixation, Submitted area, Submitted by, Entered by, Histology ref, Sender ref, Submitted/Received date from and to; Search; "Clear search" |
| SR-03 | By number | Known submission number; Search | Only that submission |
| SR-04 | Status filter | Choose a status; Search | Every row has that status |
| SR-05 | Date range | Submitted date from/to around a known date | Rows in range |
| SR-06 | No match | `99999999` | Empty result or message; no error |
| SR-07 | Sort / page | Result headers and pagination | Order changes; pages differ; no error |
| SR-08 | Clear | "Clear search" | Filters reset |

## Suite 2: Search outputs (test totals)

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| SR-09 | Totals | Select Project; Start/End dates; tick a histology and an antibody checkbox; Search | Totals table or message; no error |
| SR-10 | Submission type and Archive | Pick Submission type; tick Archive; Search | Result renders; no error |
| SR-11 | Invalid range | End before Start | Validation or empty result; no 500 |

## Suite 3: Search by PM date

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| SR-12 | Search | PM from/to; Search | Results or "no results"; no error |
| SR-13 | Empty | Search with no dates | Validation or all results; no 500 |

## Suite 4: Search archive locations

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| SR-14 | Location and type | Choose Archive location and Archive type; Search | Results or "no results"; no error |
| SR-15 | By ref | Histology ref or Sender ref from SR-03 data | Matching rows if archived; no error |
| SR-16 | Tissue / block | Tissue code (tissue archive) or Block ref (block archive) | Results or "no results"; no error |

## Suite 5: Search block refs

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| SR-17 | Search | Sender ref or Histology ref; Search | Results or "no results"; no error |
| SR-18 | Link | Follow "View Old ICC_Sub data" | Opens View old ICC_Sub data |

## Suite 6: View samples

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| SR-19 | Tissue mode | Radio `mode-tissue`; Sender ref / Tissue / Project; Search | Tissue results; no error |
| SR-20 | Block mode | Radio `mode-block`; Search | Block results; no error |
| SR-21 | Clear all | "Clear all" | Form resets |

## Suite 7: View old ICC_Sub data

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| SR-22 | Tables | Each Table in `#SelectedTable`; Go; type a Filter; Go; Clear | Grid per table; filter narrows rows; Clear resets |

## Suite 8: Unused histology refs

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| SR-23 | Loads | Open page | Table "Histology ref", 10 rows per page, multi-page |
| SR-24 | Sort / paging | Sort by Histology ref; page 2 and last page | Order and rows change; no error |

## Suite 9: Common

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| SR-01 | Every page loads | Open each path above | HTTP 200; expected H1; Back returns to `/Index` (home) |
| SR-25 | ReturnPage handling | Open a page with `ReturnPage=https://evil.example/`; follow Back | Stays on the site, not an external URL |

---

## Report

One table per suite run:

```
| ID | Result (PASS/FAIL/SKIP) | Evidence / failure detail |
```

State which suites ran. End with a one-line summary (`n passed, n failed, n skipped`) and any app-log exceptions seen.
