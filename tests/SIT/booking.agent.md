---
description: Runs SIT tests for the Booking home-screen option (booking menu, book histology refs, book blocks) in the running Histo.Web app using the integrated browser. Can run the whole file or a single suite.
tools: [open_browser_page, navigate_page, read_page, click_element, type_in_page, run_playwright_code, screenshot_page, run_in_terminal, get_terminal_output, read_file, grep_search]
---

# Booking SIT Agent

## Running a subset

Run only what the user asks for, for example "run the Book blocks suite", "run BK-03 to BK-08", or the whole file.

| Suite | Page |
|-------|------|
| 1. Booking menu | `/Bookings/BookingMenu` |
| 2. Book histology refs | `/Bookings/BookHistologyRef` ("Block book histology refs") |
| 3. Book blocks | `/Bookings/BookBlockRef` ("Book blocks") |

## Prerequisites (all suites)

1. Start the app if `https://localhost:57879/` is not reachable: `dotnet run --project src\Histo.Web --launch-profile Histo.Web` (async terminal).
2. Local DB `(localdb)\MSSQLLocalDB`, database `histo`, with migrations from `src/Database/Migrations` applied.
3. Dev auth signs in as a valid local test persona. Default harness account: "Local Developer" (Group: Maintenance, Area: All Areas). This is a local convenience identity, not a production entitlement. The app-valid production combinations are `Customer -> External Customer` or `TB Diagnostics`, `Histopathology User -> Histopath`, and `Maintenance -> Other VLA`. For access-control or area-scoping cases, explicitly use the matching valid combination instead of assuming Maintenance/All Areas.

## Rules (all suites)

- Booking consumes real histology ref numbers and blocks that cannot be given back. Run the data-changing cases (BK-09, BK-14) only when the user has asked for a data-changing run, and then book the smallest amount (1). Everything else is read-only or validation-only.
- Before any booking, capture the "Next histology ref" values (BK-04) so changes can be verified.
- Browser notes: if the tab is not visible, click via `evaluate(b => b.click())`; navigate to link `href`s instead of clicking them.
- On HTTP 500 or an unexpected error box, read the app terminal and include the exception.
- If the session times out (`/SessionError`), navigate to `/` and continue.

---

## Suite 1: Booking menu

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| BK-01 | Menu loads | Open `/Bookings/BookingMenu` | Heading "Booking menu"; links "Book a histology ref to a sample" and "Book a pre-booked block to a sample" |
| BK-02 | Menu links | Follow each link | `BookHistologyRef` and `BookBlockRef` open; Back goes to the menu |
| BK-17 | Back links | Follow Back on each page | Menu, then home |

## Suite 2: Book histology refs

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| BK-03 | Page loads | Open `/Bookings/BookHistologyRef` | Select `#HistologyType`; number `#NumberToBook`; Book; table Description / Next histology ref with rows; "Check used block refs" link |
| BK-04 | Next refs table | Read the table | Each range type has a Description and a well-formed next ref |
| BK-05 | Range type options | Read `#HistologyType` options | One option per table row |
| BK-06 | Nothing entered | Click Book with nothing selected | Validation message; nothing booked; refs unchanged |
| BK-07 | Zero / negative | Choose a type; Number `0` and `-1`; Book | Validation message; nothing booked |
| BK-08 | Non-numeric / too large | `abc` (blocked by input) and `1000000` | Validation or limit message; no 500; nothing booked |
| BK-09 | Happy path (data-changing, only on request) | Choose a type; Number `1`; Book | Success message; that type's Next histology ref advanced by 1 |
| BK-10 | Check used block refs | Follow "Check used block refs" | Page opens; no error |

## Suite 3: Book blocks

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| BK-11 | Page loads | Open `/Bookings/BookBlockRef` | Inputs Sender Ref from/to, Block Ref from/to; Book; "Check used block refs"; help text about PG/MC numbers and 2-3 digit block refs |
| BK-12 | Empty | Click Book with nothing entered | Validation message; nothing booked |
| BK-13 | Bad block ref | Block Ref from `1` or `ABCD`; Book | Validation about two or three digits; nothing booked |
| BK-14 | Happy path (data-changing, only on request) | Unused PG/MC number, block ref `01` to `01`; Book | Success; booking appears in Check used block refs |
| BK-15 | Range rule | PG number to a non-PG/MC ref | Message that only the first ref is booked; validation only unless on a data-changing run |
| BK-16 | Check used block refs | Enter a Sender Ref range; "Check used block refs" | Used refs or "none"; no error |

---

## Report

One table per suite run:

```
| ID | Result (PASS/FAIL/SKIP) | Evidence / failure detail |
```

State which suites ran and whether the run was read-only. If data was booked, list the histology type, count, sender refs and block refs. End with a one-line summary (`n passed, n failed, n skipped`) and any app-log exceptions seen.
