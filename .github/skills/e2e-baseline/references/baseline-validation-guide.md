# Baseline Validation Guide
 
Reference document for `e2e-baseline-validator`. Loaded by that agent only.
 
## Purpose
 
Define the decision tree for classifying test failures, the correction rules
for each failure type, and the sign-off report format.
 
---
 
## Failure Classification Decision Tree
 
```
Test failed?
├── Exception type contains "TimeoutException"?
│   ├── Error message contains "Locator" or "selector" or "waiting for"?
│   │   └── Classification: selector-mismatch
│   │       → the element was not found; the selector is wrong
│   └── Exception occurred on WaitForResponseAsync or WaitForFunctionAsync?
│       └── Classification: client-side-timeout
│           → JS interaction did not complete within timeout
├── Exception type is "AssertionException" or "PlaywrightException"?
│   ├── Error message contains "Expected ... to be visible" AND locator found?
│   │   └── Classification: assertion-failure
│   │       → element found but assertion did not hold
│   └── Error message contains "Expected text" or "Expected value"?
│       └── Classification: assertion-failure
└── Any other exception?
    └── Classification: test-error
        → infrastructure or unexpected error; investigate separately
```
 
---
 
## Selector Mismatch Correction Rules
 
### How to identify the wrong selector
 
1. Read the test failure message — it will contain the locator expression
   (e.g. `GetByLabel("First Name")` or `GetByRole(AriaRole.Button, "Save")`)
2. Find which selector registry entry maps to this locator by reading
   `Pages/{PageName}Page.cs` — trace the ILocator property back to the
   selector constant it uses in `Selectors/{PageName}Selectors.cs`
3. Find the selector registry entry in
   `docs/e2e-baseline/selector-registry/{PageName}.selectors.json`
 
### Proposing a correction
 
For `semantic` (getByLabel) mismatches:
- The label text in the registry does not match the rendered HTML `<label>` text
- Common cause: trailing colon included (e.g. registered `"First Name:"` but
  rendered without colon), or case difference
- Propose the exact text as it appears in the rendered HTML
 
For `role` (getByRole) mismatches on buttons:
- The `Text` attribute value in `.aspx` differs from the rendered button text
- Common cause: resource file substitution, dynamic text from code-behind,
  or web.config substitution
- Propose the exact text as it appears in the rendered HTML
 
For `text` (getByText) mismatches on feedback messages:
- The success/error message string in the code-behind differs from what renders
- Propose the exact string as it appears in the rendered page
 
### Correction format in the report
 
```markdown
| Test | Current Selector | Proposed Correction | Reason |
|---|---|---|---|
| `Save_valid_record` | `getByLabel("First Name")` | `getByLabel("Forename")` | Label text in rendered HTML is "Forename" not "First Name" |
```
 
### Applying corrections (after human approval)
 
When the orchestrator signals human approval of the proposed corrections:
1. Update the `value` field in `docs/e2e-baseline/selector-registry/{PageName}.selectors.json`
2. Re-invoke `e2e-baseline-test-generator {PageName}` to regenerate
   `Selectors/{PageName}Selectors.cs` with the corrected values
3. Do NOT regenerate `Pages/{PageName}Page.cs` or `Specs/{PageName}Tests.cs`
   unless the correction changes the locator type (not just the value)
 
---
 
## Client-Side Timeout Correction Rules
 
### Diagnosis procedure
 
1. Find the test failure — identify which page object method timed out
2. Read `docs/e2e-baseline/asset-manifests/{PageName}.assets.json`
3. Find the JavaScript files listed for this page
4. Read the relevant JS file to identify the async pattern:
 
| JS pattern found | Diagnosis | Proposed fix |
|---|---|---|
| `__doPostBack(...)` called after click | UpdatePanel postback — `WaitForResponseAsync` cannot detect this | Replace `WaitForResponseAsync` with `Expect(element).ToBeVisibleAsync()` with extended timeout |
| `$find(clientId).show()` | ModalPopupExtender show — rendered via CSS class change, not network | Use `Expect(modal).ToBeVisibleAsync()` |
| `$.ajax({url: "..."})` or `fetch(...)` | XHR/Fetch call — `WaitForResponseAsync` CAN detect this | Pass the URL pattern to `WaitForResponseAsync` |
| `TinyMCEHelper().inlineEditorInit(...)` | TinyMCE iframe load — timing sensitive | Add `await page.WaitForFunctionAsync("() => typeof tinymce !== 'undefined' && tinymce.activeEditor !== null")` before iframe interaction |
 
### Applying JS timing fixes
 
Fixes to page object method bodies in `Pages/{PageName}Page.cs` may be
applied directly without waiting for human approval — these are infrastructure
fixes that do not change the scenario's intent or expected outcome.
 
After applying: record the fix in the report as "auto-applied".
 
---
 
## Assertion Failure Escalation Rules
 
Assertion failures MUST be escalated to the human reviewer. Never auto-correct.
 
### Escalation message format
 
```markdown
### ⚠ Assertion Failure — Human Review Required
 
**Test**: `Submit_without_FirstName_shows_error`
**Page**: EditProfileTitle
**Failure**: Expected text "First Name is required" to be visible, but it was not present.
 
**What this means**: The page loaded and rendered correctly, but the expected
error message was not shown after submitting the form with an empty First Name
field. This could mean:
- The validation behaviour changed (business logic difference)
- The error message text is different from what was recorded in the inventory
- The form submission did not trigger server-side validation
 
**Action required**: Please navigate to the page manually and reproduce the
scenario. If the error message text has simply changed, this is a selector
correction (not an assertion failure) — update the selector registry instead.
If the validation behaviour is genuinely different, this represents a
functional difference that must be investigated before the baseline can be
signed off.
```
 
---
 
## Test Error Handling
 
For `test-error` classified failures:
 
1. Read the full stack trace from the `.trx` results file
2. Determine whether the error is:
   - **Infrastructure**: Playwright browser launch failure, network
     connectivity, missing environment variable → record as infrastructure
     issue; propose fix in report but do not block sign-off
   - **Test code**: NullReferenceException in page object, missing page
     object method → propose code fix in the report; apply it to the
     affected `.cs` file directly (this is a code defect, not a
     selector or behaviour issue)
 
---
 
## Sign-Off Report Structure
 
The `docs/e2e-baseline/baseline-report.md` must contain these sections
in this order:
 
```markdown
# E2E Baseline Run Report
 
Generated: {ISO timestamp}
Base URL: {baseUrl}
Run mode: pre-migration baseline
 
## Summary
 
| Metric | Value |
|---|---|
| Total tests | {n} |
| Passed | {n} ({n}%) |
| Skipped | {n} |
| Failed | {n} |
| — Selector mismatches | {n} |
| — Assertion failures | {n} |
| — Client-side timeouts | {n} |
| — Test errors | {n} |
 
## Status
 
{One of:}
✓ Ready for sign-off — all failures are technical (selector/timing) with
  proposed corrections. No assertion failures.
 
⚠ Requires investigation — {n} assertion failure(s) require human review
  before sign-off.
 
## Selector Corrections Proposed
 
{Table of corrections — see format above}
{If none: "None — all selectors resolved correctly."}
 
## JS Timing Fixes Applied
 
{List of auto-applied fixes}
{If none: "None required."}
 
## Assertion Failures Requiring Human Review
 
{Escalation blocks — see format above}
{If none: "None — all failures are technical."}
 
## Skipped Tests (Require Test Data)
 
| Test | Page | Reason |
|---|---|---|
| `Reviewer_cannot_see_Delete_button` | ManageProfile | Requires user account with Reviewer role |
 
## Next Steps
 
{One of:}
→ Apply the {n} proposed selector corrections above, then re-run the baseline.
→ Investigate the {n} assertion failures above before proceeding.
→ Sign off approved: type "sign off" to the orchestrator to finalise the baseline.
```
 
---
 
## Sign-Off Procedure
 
The orchestrator calls this agent a second time after human approval to
finalise the sign-off. At that point:
 
1. Update `docs/e2e-baseline/baseline-results.json`:
   - Set `"status": "signed-off"`
   - Set `"signedOffAt"`: current ISO timestamp
   - Set `"signedOffPassRate"`: current pass percentage
 
2. Append to `docs/e2e-baseline/baseline-report.md`:
 
```markdown
---
 
## Sign-Off Record
 
Signed off: {ISO timestamp}
Final pass rate: {n}%
Signed-off test count: {passed}/{total - skipped}
 
This baseline is the agreed pre-migration behavioural specification.
All post-migration test runs must achieve the same pass rate against
the same scenarios to prove functional equivalence.
```
 
3. Do not modify any test files after sign-off.