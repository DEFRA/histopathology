---
description: Runs SIT tests for the Audit logs home-screen option (menu, search by date, by user, by submission) in the running Histo.Web app using the integrated browser. Can run the whole file or a single suite.
tools: [open_browser_page, navigate_page, read_page, click_element, type_in_page, run_playwright_code, screenshot_page, run_in_terminal, get_terminal_output, read_file, grep_search]
---

# Audit Log SIT Agent

## Running a subset

Run only what the user asks for, for example "run the By user suite", "run AL-12 to AL-15", or the whole file.

| Suite | Page |
|-------|------|
| 1. Audit log menu | `/AuditLog/AuditLogMenu` |
| 2. Search by date | `/AuditLog/AuditLogByDate` |
| 3. Search by user | `/AuditLog/AuditLogByUser` |
| 4. Search by submission | `/AuditLog/AuditLogBySubmission` |
| 5. Audit trail written by edits | cross-feature |

## Prerequisites (all suites)

1. Start the app if `https://localhost:57879/` is not reachable: `dotnet run --project src\Histo.Web --launch-profile Histo.Web` (async terminal).
2. Local DB `(localdb)\MSSQLLocalDB`, database `histo`, with migrations from `src/Database/Migrations` applied.
3. Dev auth signs in as a valid local test persona. Default harness account: "Local Developer" (Group: Maintenance, Area: All Areas). This is a local convenience identity, not a production entitlement. The app-valid production combinations are `Customer -> External Customer` or `TB Diagnostics`, `Histopathology User -> Histopath`, and `Maintenance -> Other VLA`. For access-control or area-scoping cases, explicitly use the matching valid combination instead of assuming Maintenance/All Areas.
4. Audit entries are written by edits. If a search finds nothing, make a harmless change first (for example run the Administration agent's User maintenance suite) and retry.

## Rules (all suites)

- Read-only, except Suite 5 which makes one edit to a `SIT` record.
- Set `<input type=date>` with `fill('yyyy-mm-dd')`; "today" is the date of the run.
- On HTTP 500 or an unexpected error box, read the app terminal and include the exception.
- If the session times out (`/SessionError`), navigate to `/` and continue.

---

## Suite 1: Audit log menu

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| AL-01 | Menu loads | Open `/AuditLog/AuditLogMenu` | Heading "Audit log"; links "Search by date", "Search by user", "Search by submission" |
| AL-02 | Menu links | Follow each link | "Audit log — search by date" / "— by user" / "— by submission"; Back returns to the menu |
| AL-18 | Access | Open all four pages as Maintenance | HTTP 200 on each |

## Suite 2: Search by date

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| AL-03 | Form | Open page | Inputs `#StartDate`, `#EndDate`; Search |
| AL-04 | Range with entries | From 30 days ago, To today; Search | Audit rows with who/when/what; no error |
| AL-05 | Future range | From and To in the future | No results message |
| AL-06 | Invalid range | From later than To | Validation or no results; no 500 |
| AL-07 | Empty | Search with no dates | Validation or results; no 500 |
| AL-17 | Sort and paging | If results span pages, open page 2 and sort | Rows change; no error |

## Suite 3: Search by user

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| AL-08 | Form | Open page | Select `#UserID`, From, To, Search |
| AL-09 | User list | Read `#UserID` options | Contains users (for example "Local Developer" after an edit) |
| AL-10 | Search | A user with edits; a range; Search | Only that user's entries |
| AL-11 | No entries | A user without edits | No results message |

## Suite 4: Search by submission

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| AL-12 | Form | Open page | Input `#SubmissionID` (number), From, To, Search |
| AL-13 | Valid | Submission number with audit rows; Search | Results for that submission only |
| AL-14 | Unknown | `99999999` | No results message |
| AL-15 | Empty / negative | Empty box, then `-1` | Validation or no results; no 500 |

## Suite 5: Audit trail written by edits

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| AL-16 | Edit is audited | Edit a `SIT` record (user or pick list item), then search by date for today | New entry showing the changed field and old/new values |

---

## Report

One table per suite run:

```
| ID | Result (PASS/FAIL/SKIP) | Evidence / failure detail |
```

State which suites ran. End with a one-line summary (`n passed, n failed, n skipped`) and any app-log exceptions seen.
