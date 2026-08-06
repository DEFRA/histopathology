# Selector Extraction Rules
 
Reference document for `e2e-baseline-selector-extractor`. Loaded by that agent only.
 
## Purpose
 
Define the classification rules, priority order, and edge case handling for
converting Web Forms control declarations into typed Playwright selector descriptors.
 
---
 
## Selector Type Priority
 
When multiple selector types could apply to the same element, use the highest
priority type that is valid:
 
| Priority | Type | When valid |
|---|---|---|
| 1 (best) | `semantic` | Element has an explicit `<label for="...">` association via `AssociatedControlID` |
| 2 | `role` | Element has a meaningful ARIA role AND visible name (button text, heading text, link text) |
| 3 | `text` | Element has unique visible text but no structural role (status messages, confirmations) |
| 4 (last resort) | `telerik` | Element is a Telerik control with no semantic equivalent |
 
**Never use CSS class selectors, ID selectors, or XPath as a selector type.**
If none of the above four types apply, record the element with `"type": "unsupported"`
and a `"reason"` field explaining why — do not fabricate a selector.
 
---
 
## Semantic Selector Rules
 
An element qualifies as `semantic` if and only if:
- There is an `<asp:Label>` with `AssociatedControlID="X"` pointing at the control, AND
- The `Text` attribute of that label contains user-visible text (not just `*` or empty), AND
- The label text, when stripped of trailing colons and trimmed, uniquely identifies the field on this page
 
The `value` stored in the registry is the **label text stripped of trailing colons and trimmed**.
Examples:
- `Text="First Name:"` → `"First Name"`
- `Text="Date of Birth"` → `"Date of Birth"`
- `Text=" * First Name"` → `"First Name"` (strip leading asterisk/space too)
 
If two fields on the same page have identical label text, append the field's purpose
to disambiguate: `"First Name (Contact)"` vs `"First Name (Emergency)"`.
 
---
 
## Role Selector Rules
 
Use `getByRole` for:
 
| Element | Role | Name source |
|---|---|---|
| `<asp:Button Text="Save">` | `button` | `Text` attribute |
| `<asp:LinkButton Text="Delete">` | `button` | `Text` attribute |
| `<asp:HyperLink Text="Back">` | `link` | `Text` attribute |
| `<asp:ValidationSummary>` | `alert` | No name — use `null` |
| Page heading (`<h1>`, `<h2>`) | `heading` | Visible text |
| Page `Title` if rendered as heading | `heading` | `Title` attribute |
| `<asp:ImageButton>` | `button` | `AlternateText` or `ToolTip` attribute |
 
The `value` (name) stored is the **exact visible text** the control renders.
For buttons, this is the `Text` attribute value. For headings, it is the literal
text content. Never store the server-side `ID`.
 
---
 
## Text Selector Rules
 
Use `getByText` for elements whose only stable identifier is their visible text
and that have no meaningful ARIA role:
 
- Status/success messages (e.g. `lblMessage.Text = "Record saved"`)
- Confirmation messages
- Empty-state messages (e.g. "No results found")
- Static informational text that serves as the expected outcome of an action
 
The `value` stored is the **exact string** from the code-behind assignment or
the markup `Text` attribute. Do not paraphrase or shorten it.
 
---
 
## Telerik Selector Rules
 
For every `<telerik:*>` control:
 
### Generated ID Pattern
 
Web Forms places server controls inside naming containers. A control with
`ID="dpDOB"` inside a `ContentPlaceHolder` with `ID="cphMain"` renders as:
`ctl00_cphMain_dpDOB`
 
For Telerik sub-elements (e.g. the input inside `RadDatePicker`), the rendered
ID has further suffixes. Use a CSS **ends-with** attribute selector to match
regardless of naming container depth:
 
| Telerik control | Rendered input element | Pattern to use |
|---|---|---|
| `RadDatePicker ID="dpDOB"` | `ctl00_cphMain_dpDOB_dateInput` | `[id$='_dpDOB_dateInput']` |
| `RadComboBox ID="cmbStatus"` | `ctl00_cphMain_cmbStatus_Input` | `[id$='_cmbStatus_Input']` |
| `RadTreeView ID="tvwSpecies"` | `ctl00_cphMain_tvwSpecies` | `[id$='_tvwSpecies']` |
| `RadGrid ID="grdResults"` | `ctl00_cphMain_grdResults` | `[id$='_grdResults']` |
 
If the Telerik control has an `AssociatedControlID`-style label, record
`"associatedLabel"` with the label text. This is used by `TelerikHelper` to
generate a human-readable description of the interaction.
 
### Telerik Sub-Element Patterns
 
For `RadGrid`: rows are identified by row index or by a cell value. Record:
- `"gridIdPattern"`: `[id$='_{gridId}']`
- `"rowSelector"`: `tr.rgRow` (standard Telerik row class)
- `"cellSelector"`: `td` within the row
 
For `RadTreeView`: nodes are matched by their text. Record:
- `"treeIdPattern"`: `[id$='_{treeId}']`
- `"nodeSelector"`: `.rtText` (Telerik tree node text class)
 
---
 
## Validation Selector Rules
 
Each `ValidationSummary` generates one selector entry under `feedback`:
- `type`: `role`
- `role`: `alert`
- `name`: `null` (role alone is sufficient; name not required)
- `group`: the `ValidationGroup` attribute value
 
Each validator's `ErrorMessage` generates one selector entry:
- `type`: `text`
- `value`: the exact `ErrorMessage` attribute value
 
These are used in required-field and format-validation scenarios to assert that
the correct error message is visible after failed submission.
 
---
 
## Status Message Extraction
 
To find success/info message selectors, read the code-behind file (`.aspx.vb` or
`.aspx.cs`) and search for assignments to labels that display messages:
 
Patterns to find (VB.NET):
```vb
lblMessage.Text = "..."
lblStatus.Text = "..."
lblInfo.Text = "..."
```
 
Patterns to find (C#):
```csharp
lblMessage.Text = "...";
lblStatus.Text = "...";
```
 
Record each unique string literal as a `text` type selector under `feedback`.
 
---
 
## Edge Cases
 
### Label with no visible text
If `AssociatedControlID` points to a control but the label `Text` is empty or
only contains `" *"`, do not create a `semantic` selector. Fall back to `role`
if the control has a meaningful ARIA role, or `unsupported` if not.
 
### Multiple submit buttons
If the page has more than one button with different `ValidationGroup` values
(multi-step or multi-section forms), record each button as a separate `role`
selector entry with distinct names.
 
### Duplicate button text
If two buttons have identical `Text` values (e.g. two "Edit" buttons in a grid),
record both but note `"duplicate": true`. The test generator will use
`GetByRole(...).Nth(0)` / `.Nth(1)` patterns via `TelerikHelper` for grid-row
scoped interactions.