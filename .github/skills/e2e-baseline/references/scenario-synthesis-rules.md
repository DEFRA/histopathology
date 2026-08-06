# Scenario Synthesis Rules
 
Reference document for `e2e-baseline-scenario-synthesiser` (Phase B).
Loaded together with `asset-analysis-procedure.md`.
 
## Purpose
 
Define the rules for converting asset analysis findings into well-formed,
user-observable test scenarios that will remain valid as behavioural
specifications after migration.
 
---
 
## Core Principle
 
Every scenario must describe what a **user observes**, not what the code does.
 
| Correct (user-observable) | Incorrect (implementation detail) |
|---|---|
| "Click Save" | "Trigger btnSave_Click event handler" |
| "The page shows 'Record saved'" | "lblMessage.Text is set to 'Record saved'" |
| "An error message appears" | "The ValidationSummary control renders" |
| "The grid refreshes without a full page reload" | "An UpdatePanel partial postback occurs" |
| "A confirmation dialog appears" | "`app.confirmPrompt` is called" |
 
---
 
## Evidence Requirement
 
Every scenario must have at least one entry in `assetEvidence`. If you cannot
identify a specific file that evidences the scenario, do not generate it.
 
Weak evidence is acceptable: if a `RequiredFieldValidator` is in the inventory
(recorded from the `.aspx` markup), its source file is the `.aspx` file.
That is sufficient evidence.
 
Strong evidence is preferable: code-behind event handler logic, JS function
definitions, business class validation methods.
 
---
 
## Scenario Naming Convention
 
Scenario names must be unique within a page's catalogue and follow this pattern:
 
`{Verb} {subject} [qualifier]`
 
Examples:
- `Save valid profile title`
- `Submit without required First Name shows error`
- `Delete profile with confirmation`
- `Cancel delete returns to page unchanged`
- `Search results refresh without page reload`
- `Navigate away with unsaved changes shows prompt`
- `Rich text editor accepts formatted content`
- `View page as admin shows Delete button`
- `View page as reviewer hides Delete button`
 
---
 
## Step Writing Rules
 
Steps describe user actions in second person, present tense:
 
✓ `"Type 'John Smith' into the First Name field"`
✓ `"Click the Save button"`
✓ `"Leave the Email field empty"`
✓ `"Click the Delete link in the first table row"`
✓ `"Click OK in the confirmation dialog"`
✓ `"Select '2024' from the Year dropdown"`
 
✗ `"Set txtFirstName.Text to 'John Smith'"` (implementation detail)
✗ `"Trigger the Page_Load handler"` (implementation detail)
✗ `"Call btnSave_Click"` (implementation detail)
✗ `"Wait for UpdatePanel response"` (implementation detail — the test handles this)
 
---
 
## Expected Outcome Writing Rules
 
Outcomes describe what the user sees as a result:
 
✓ `"The page displays 'Profile saved successfully'"`
✓ `"An error message 'First Name is required' appears above the field"`
✓ `"The page navigates to Manage Profile"`
✓ `"The table row is removed from the list"`
✓ `"The confirmation dialog disappears and no changes were made"`
✓ `"The Delete button is not visible"`
 
---
 
## Per-Category Rules
 
### Happy Path
 
- One happy path per page (unless the page has multiple independent forms)
- Cover all required fields — use the `RequiredFieldValidator` inventory to
  identify every field that must be filled
- The expected outcome must reference a specific success message string from
  the asset analysis, or a specific URL if the page redirects
- If `Response.Redirect` was found in the code-behind, the expected outcome
  must include "navigates to [target page]"
 
### Required Field Validation
 
- One scenario per `ValidationGroup` (not one per validator, unless validators
  in the same group test logically separate concepts)
- If multiple fields share a `ValidationGroup`, the scenario can test them
  together: "Leave all required fields empty → submit → assert summary error"
- OR generate individual scenarios for each required field to maximise coverage
  (prefer individual when the page has ≤10 required fields)
- The `expectedOutcome` must reference the exact `ErrorMessage` string from
  the validator
 
### Format Validation
 
- One scenario per `RegularExpressionValidator`
- The `steps` must include an example of invalid input that the regex rejects.
  Use the `validationExpression` to derive an invalid example:
  - Email regex → use `not-an-email`
  - Numeric regex → use `abc`
  - Date regex → use `32/13/2024`
  - Domain\User format → use `nodomain`
- The `expectedOutcome` must reference the exact `ErrorMessage` string
 
### Client Confirm Dialog
 
Evidence: `onClientClick` attribute containing `confirmPrompt` or similar.
 
Generate exactly two sub-scenarios:
 
1. **Confirm path**: trigger the action → accept the dialog → assert the
   destructive/navigating action completed (deletion removed the row, redirect
   occurred, etc.)
2. **Cancel path**: trigger the action → dismiss the dialog → assert the page
   is unchanged (the row still exists, no redirect occurred)
 
Both sub-scenarios share the same `assetEvidence` files.
 
### Navigation Guard
 
Evidence: `SavePrompt.ascx` in user controls.
 
Generate two sub-scenarios:
 
1. **Trigger guard**: enter data → attempt navigation → assert unsaved changes
   prompt appears
2. **Confirm leave**: enter data → attempt navigation → when prompted, confirm
   leaving → assert navigation completes and changes are discarded
 
The navigation target in the `steps` should be the first navigation link
identified in the `NavigationLinks.ascx` markup or the page's back/cancel button.
 
### UpdatePanel Partial Render
 
Evidence: `hasUpdatePanel: true` AND a triggering action (search button,
pagination, sort column click, filter dropdown).
 
Generate one scenario per distinct trigger type (search, paginate, sort,
filter — not one per column).
 
`expectedOutcome` must specify: "The results area updates to show [content
description] without the browser page address changing."
 
`clientSideBehaviour: true` must be set.
 
### TinyMCE Rich-Text Field
 
Evidence: `pageSpecificJs` references `tinymce` OR `RegisterStartupScript`
content contains `TinyMCEHelper` OR `LongTextProfileField` control present.
 
`requiresIframeHandling: true` must be set (TinyMCE renders in an iframe).
 
Steps must include: "Click inside the rich-text editor area" (not the label
or the surrounding div).
 
### Permission Gate
 
Evidence: `User.IsInRole("X")` or equivalent in code-behind.
 
Generate two scenarios per role-gated element:
1. Authorised role: navigate as a user with the required role → assert the
   gated element or action is visible/enabled
2. Unauthorised role: navigate as a user without the role → assert the gated
   element is absent OR the page redirects to an access-denied page
 
`requiresRole`: set to the role name extracted from the code-behind.
`requiresTestData: true` if test user accounts with specific roles are needed.
 
### Empty State
 
Evidence: code-behind null/empty check that sets `Visible = True` on an
empty-state label or panel.
 
Steps: navigate to the page with no data matching any search criteria or
with an empty dataset.
 
`requiresTestData: true` — an empty dataset must be established in test setup.
 
### Redirect
 
Evidence: `Response.Redirect("...")` in a button click handler.
 
One scenario per distinct redirect target found. The scenario exercises the
action that triggers the redirect and asserts arrival at the target URL.
The URL pattern must match what was recorded in `Routes` (relative path only,
not absolute).
 
### AjaxControlToolkit Modal
 
Evidence: `ModalPopupExtender` in inventory AND `$find(...)` call in JS.
 
Steps: trigger the control that shows the modal → assert modal panel is visible
→ click the modal's OK/Cancel button → assert modal is dismissed.
 
`clientSideBehaviour: true` must be set.
 
---
 
## Scenarios to Skip
 
Do not generate a scenario for:
 
- CSS-only visual changes (colour, spacing, layout) with no state change
- JavaScript animations with no testable end state
- Browser print functionality (`window.print()`)
- External links (open in new tab — no assertion possible in same context)
- File download triggers without a testable post-download state
  (downloads have no DOM outcome to assert)
- Session timeout (handled by infrastructure, not a page behaviour test)
 
---
 
## `requiresTestData` Flag Rules
 
Set `requiresTestData: true` when:
- The scenario requires specific database records to exist
- The scenario requires a user account with a specific role
- The scenario requires a page to be in a specific state (e.g. draft version)
- The scenario requires an empty dataset
 
When `requiresTestData: true`, add a `"testDataNote"` field with a plain-English
description of exactly what data is needed.
 
---
 
## Scenario Count Targets
 
As a guide (not a hard limit):
 
| Page complexity | Expected scenario count |
|---|---|
| `simple` | 3–8 scenarios |
| `medium` | 8–18 scenarios |
| `complex` | 15–35 scenarios |
 
If the count falls significantly outside these ranges, review whether all
evidence sources were fully analysed.