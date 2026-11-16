---
description: Runs SIT tests for the Administration home-screen options (User maintenance, Pick list maintenance, Edit Sender/Histology Ref) in the running Histo.Web app using the integrated browser. Can run the whole file or a single suite.
tools: [open_browser_page, navigate_page, read_page, click_element, type_in_page, run_playwright_code, screenshot_page, run_in_terminal, get_terminal_output, read_file, grep_search]
---

# Administration SIT Agent

## Running a subset

The tests are grouped into suites. Run only what the user asks for, for example "run the User maintenance suite", "run PL-08 to PL-14", or "run the Administration agent" (all suites).

| Suite | ID prefix | Pages |
|-------|-----------|-------|
| 1. User maintenance | UM | `/Admin/UserMaintenance`, `/Admin/AddUser`, `/Admin/EditUser/{id}` |
| 2. Pick list maintenance | PL | `/Admin/PickListMaintenance`, `/Admin/LookupItems/{tableId}`, `/Admin/AddLookupItem/{tableId}`, `/Admin/EditLookupItem/{tableId}?itemCode={code}` |
| 3. Edit Sender/Histology Ref | ER | `/Admin/EditAnimalRef` |

## Prerequisites (all suites)

1. Start the app if `https://localhost:57879/` is not reachable: `dotnet run --project src\Histo.Web --launch-profile Histo.Web` (async terminal).
2. Local DB is `(localdb)\MSSQLLocalDB`, database `histo`. Migrations in `src/Database/Migrations` must be applied (see `src/Database/Deploy/Deploy.sql` for order). Missing migrations show up as SQL errors in the app log, for example "Cannot insert the value NULL into column 'NTLogin'" means `V20260914_01_User_Email_NotNull_NTLogin_Nullable.sql` and `V20260928_01_Fix_Empty_String_NTLogin_To_Null.sql` were not applied.
3. Dev auth signs in as a valid local test persona. Default harness account: "Local Developer" (Group: Maintenance, Area: All Areas). This is a local convenience identity, not a production entitlement. The app-valid production combinations are `Customer -> External Customer` or `TB Diagnostics`, `Histopathology User -> Histopath`, and `Maintenance -> Other VLA`. For access-control or area-scoping cases, explicitly use the matching valid combination instead of assuming Maintenance/All Areas.

## Rules (all suites)

- Use a unique stamp `<yyyyMMddHHmmss>` per run in test data.
- Only touch records created by the current run (names or descriptions starting with `SIT`). Never edit pre-existing records.
- Users and pick list items cannot be deleted; deactivate every record created by the run at the end of the suite.
- On a failed save, read the terminal running the app and include the logged exception in the report.
- If the session times out (`/SessionError`), navigate to `/` and continue.
- Browser notes (GOV.UK components): the Active checkbox is visually hidden, so set it with `locator('#Active').evaluate((e, v) => e.checked = v, value)`; submit buttons may need `evaluate(b => b.click())` when the tab is not visible; navigate to the Cancel/Back link's `href` instead of clicking it; `PageNumber` past the last page returns the last page again, so stop at "Page n of m" when collecting rows across pages.
- Use `run_playwright_code` with `page.selectOption` by label for select lists; batch cases of a suite in a single script and return one result line per case.

---

## Suite 1: User maintenance (UM)

Data: name `SIT User <stamp>`, email `sit.user.<stamp>@example.com`. Cleanup: deactivate every `SIT User` created (find IDs with `sqlcmd -S "(localdb)\MSSQLLocalDB" -d histo -E -Q "SELECT ID, Name, Email, Active FROM [User] WHERE Name LIKE 'SIT User%'"`).

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| UM-01 | List loads | Open `/Admin/UserMaintenance` | Heading "User maintenance"; table with Name, Email, Group, Area, Active, Actions; "Add user" link present |
| UM-02 | Sort | Open each `?SortColumn=<col>&SortDesc=false` | Rows reorder; no error |
| UM-03 | Show deactivated filter | Compare `?ShowDeactivated=True` and `False` | Inactive rows (Active = No) only appear with True (list is paginated, so compare on a page that contains inactive rows) |
| UM-04 | Add user - happy path | Name, email, Group "Histopathology User", Area "Histopath", Active ticked | Redirect to list; new row visible with entered values and Active = Yes |
| UM-05 | Add user - required fields | Submit empty form | Stays on Add user; "There is a problem" summary; nothing saved |
| UM-06 | Add user - duplicate email | Add a second user with the UM-04 email | Not saved; "A user with this email already exists."; one row with that email |
| UM-07 | Add user - blank NTLogin twice | Add two users with different emails (NTLogin is never entered) | Both save (NTLogin stored as NULL, no unique-index collision); optionally confirm `SELECT NTLogin FROM [User] WHERE Name LIKE 'SIT User <stamp>%'` returns NULLs |
| UM-08 | Add user - cancel | Fill form, follow Cancel | Returns to list; no new row |
| UM-09 | Edit user | Change Name, Group and Area of the UM-04 user; save | Redirect to list; reload shows new values; Email unchanged |
| UM-10 | Edit user - deactivate | Untick Active; save | Active = No; hidden when the filter is unticked |
| UM-11 | Edit user - reactivate | Tick Active; save | Active = Yes |
| UM-12 | Edit user - invalid email | Email `not-an-email`; save | Not saved (DB email unchanged); validation error or browser validation |
| UM-13 | Edit user - audit | After UM-09 check the audit log by date | Entry with old and new values (SKIP with a note if it cannot be found) |
| UM-14 | Edit non-existent user | Open `/Admin/EditUser/999999` | Redirect to list or friendly error; no 500 |

---

## Suite 2: Pick list maintenance (PL)

Test table: **Archive Location** (`tableId` 16, code-keyed: Code, Description, Active). Test items: code `T<5 digits>`, `U<5 digits>`, `V<5 digits>` (respect any `maxlength`), description `SIT Item <stamp>`. Cleanup: deactivate every item created.

Known pick lists (name -> tableId): Archive Location 16, Fixatives 10, Non-TSE Antibodies 5, Pathologists 18, Post-Fixation 12, Projects 19, QC Code 14, Remedial Action 15, Special Stain 6, Submission Priority 2, Submitted As 11, TC Codes 17, Time Received 3, Tissue Type 9, TSE Antibodies 4. Pathologists (18) and Projects (19) are ID-keyed and area-scoped (Area field, no Code). Species, if present, has its own pages (`/Admin/SpeciesItems`).

### 2a. Browse (read-only)

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| PL-01 | List loads | Open `/Admin/PickListMaintenance` | Heading "Pick list maintenance"; columns "Pick list name" and Actions; each row has "Edit items" |
| PL-02 | Pagination | Open `?PageNumber=2` | "Page 2 of 2"; rows differ from page 1; First/Previous go to page 1 |
| PL-03 | Sort | `?SortColumn=TableName&SortDesc=true` | Rows reorder descending |
| PL-04 | Items page | Open `/Admin/LookupItems/16` | Heading "Pick list items", sub-heading "Archive Location"; columns Code, Description, Active, Actions; "Add item" link; Back to `/Admin/PickListMaintenance` |
| PL-05 | Every list's items page | Open `/Admin/LookupItems/{id}` for each known tableId | HTTP 200, no error, sub-heading matches name |
| PL-06 | Items sort | `?SortColumn=Code|Description|Active` | Rows reorder; no error |
| PL-07 | Show deactivated filter | Toggle on `/Admin/LookupItems/16` (after PL-13 so an inactive row exists) | Inactive rows hidden when unticked, shown when ticked |

### 2b. Add

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| PL-08 | Happy path | `/Admin/AddLookupItem/16`: code, description, Active; Add item | Redirect to items page; "'<description>' was added."; row shows Active = Yes |
| PL-09 | Required fields | Submit empty | "Enter a code." and "Enter a description."; nothing saved |
| PL-10 | Description missing | Code only | "Enter a description."; nothing saved |
| PL-11 | Code missing | Description only | "Enter a code."; nothing saved |
| PL-12 | Duplicate code | Reuse the PL-08 code, then the same code in lowercase | "The code you have selected is already in use." both times; one row only |
| PL-13 | Inactive item | Add with Active unticked | Saved; Active = No; visible only with deactivated shown |
| PL-14 | Cancel | Fill form; follow Cancel | Back to items page; no new row |
| PL-22 | ID-keyed list (Pathologists) | `/Admin/AddLookupItem/18`: description and Area; Add item | Saved (no "too many arguments" SQL error); deactivate afterwards |

### 2c. Edit

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| PL-15 | Edit description | Change description of the PL-08 item; save | "'<description>' was updated."; row shows new description; code unchanged |
| PL-16 | Change code | Change to a new unused code | Row shows new code; old code gone |
| PL-17 | Duplicate code | Change to an existing code such as `1` | "The code you have selected is already in use."; not saved |
| PL-18 | Re-save unchanged code | Change only the description | Saved (own code not a duplicate) |
| PL-19 | Deactivate / reactivate | Untick Active, save; tick, save | Active = No then Yes |
| PL-20 | Blank description | Clear description; save | "Enter a description."; not saved |
| PL-21 | Unknown item | `/Admin/EditLookupItem/16?itemCode=ZZZZZ` | Redirects to `/Admin/LookupItems/16`; no exception in log |

### 2d. Other

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| PL-23 | Invalid table | `/Admin/LookupItems/999999` | Empty "No items found." page or friendly error; no HTTP 500 |
| PL-24 | Audit | After PL-15, check the audit log by date | Entry exists (SKIP with a note if it cannot be found) |

---

## Suite 3: Edit Sender/Histology Ref (ER)

Page `/Admin/EditAnimalRef`: Sender ref (`#OriginalSenderRef`), Original histology ref (`#CurrentHistologyRef`, disabled until "Get histology ref"), New sender ref (`#NewSenderRef`) with "Save sender ref", New histology ref (`#NewHistologyRef`) with "Save histology ref", Done link. Changing a ref changes **every** submission using it.

Rules specific to this suite:
- The Save buttons modify real sample references. Do not use them on existing data. Data-changing cases (ER-10 to ER-14) run only when the user has asked for a data-changing run, and only against a sample created for the test (sender ref prefixed `SIT`), restored at the end. If no safe sample exists, mark them SKIP.
- Record original sender and histology refs before any save so they can be restored.

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| ER-01 | Page loads | Open `/Admin/EditAnimalRef` | Heading "Edit sender/histology ref"; warning paragraph; four inputs and three buttons; Back and Done go to `/` |
| ER-02 | Initial state | Inspect fields | Original histology ref disabled and empty; New fields empty |
| ER-03 | Get histology ref - known sample | Sender ref of an existing sample (from `/Search/ViewSamples`); "Get histology ref" | Original histology ref shows its value (or blank if none) |
| ER-04 | Get histology ref - unknown | `SITNOSUCHREF` | "Not found" style message; no 500 |
| ER-05 | Get histology ref - empty | Click with empty Sender ref | Validation message; no 500 |
| ER-06 | Save sender ref - empty new value | Existing sender ref, empty new value; Save | Validation message; nothing changed |
| ER-07 | Save sender ref - unknown sample | Unknown sender ref and new value; Save | "Not found" message; nothing changed |
| ER-08 | Save sender ref - duplicate | New ref equal to another sample's ref | Rejected; nothing changed (only submit on a data-changing run with a `SIT` sample) |
| ER-09 | Save histology ref - unknown sample | Unknown sender ref; Save histology ref | "Not found" message; nothing changed |
| ER-10 | Rename sender ref (data-changing) | `SIT` sample: new ref `SIT<stamp>b`; Save sender ref | Success; View samples finds it under the new ref only |
| ER-11 | Rename back (data-changing) | Reverse ER-10 | Original ref restored |
| ER-12 | Change histology ref (data-changing) | `SIT` sample: new unused histology ref; Save | Success; View samples shows it |
| ER-13 | Remove histology ref (data-changing) | Leave blank; Save histology ref | Histology ref removed; restore afterwards |
| ER-14 | Audit (data-changing) | After ER-10/12 check the audit log by date | Entries exist |
| ER-15 | Done / Back | Follow Done and Back | Both return to home |

---

## Report

One table per suite run:

```
| ID | Result (PASS/FAIL/SKIP) | Evidence / failure detail |
```

State which suites ran and whether the run was read-only. List records left deactivated (and any sample touched in Suite 3, with original and final refs). End with a one-line summary (`n passed, n failed, n skipped`) and any app-log exceptions seen.
