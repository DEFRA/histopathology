# Discovery Procedure
 
Reference document for `e2e-baseline-page-discovery`. Loaded by that agent only.
 
## Purpose
 
Provide a deterministic, step-by-step procedure for scanning an ASP.NET Web Forms
web project and producing a complete `page-inventory.json` that accurately represents
every user-facing page and its UI elements.
 
---
 
## File Enumeration Rules
 
### Include
 
- All `.aspx` files directly under `webProjectPath` and in any subfolder
  that represents a user-facing page
 
### Exclude
 
| Pattern | Reason |
|---|---|
| Path contains `RadControls/` | Telerik internal handler pages |
| Filename is `ApplicationError.aspx` | Error handler — no user-testable interaction |
| Path contains `App_Themes/` | Theming only |
| Path contains `App_Data/` | Data files |
| Filename ends in `.master` | Master pages are read separately |
| Filename ends in `.ascx` | User controls — referenced from page inventory, not top-level pages |
 
---
 
## Master Page Reading Procedure
 
For each unique master page file referenced by any `.aspx` page:
 
1. Read the `.master` file markup
2. Extract all `<link rel="stylesheet" href="...">` — record each `href` as a
   global CSS file
3. Extract all `<script src="...">` — record each `src` as a global JS file
4. Extract all `<asp:ContentPlaceHolder ID="...">` — record each `ID`
5. Find all `<%@ Register Src="..." TagPrefix="..." TagName="..." %>` — record
   each user control registration
6. Find all user control tags used directly in the master page markup — these
   are embedded on every page that uses this master
 
Do not read master page code-behind at this stage. Code-behind analysis is
performed by `e2e-baseline-scenario-synthesiser`.
 
---
 
## Control Type Mapping
 
When recording the `controlType` for a field's associated control:
 
| ASP.NET server control tag | Record as |
|---|---|
| `<asp:TextBox>` | `TextBox` |
| `<asp:DropDownList>` | `DropDownList` |
| `<asp:CheckBox>` | `CheckBox` |
| `<asp:CheckBoxList>` | `CheckBoxList` |
| `<asp:RadioButton>` | `RadioButton` |
| `<asp:RadioButtonList>` | `RadioButtonList` |
| `<asp:ListBox>` | `ListBox` |
| `<asp:FileUpload>` | `FileUpload` |
| `<pwc:DateValidator>` or custom date control | `DateValidator` |
| `<pwc:LongTextProfileField>` | `LongTextRichText` (TinyMCE-backed) |
| `<pwc:TextProfileField>` | `TextBox` |
| `<pwc:DecimalProfileField>` | `TextBox` |
| `<pwc:BooleanProfileField>` | `CheckBox` |
| `<pwc:DropDownListProfileField>` | `DropDownList` |
| `<pwc:CheckBoxListProfileField>` | `CheckBoxList` |
| `<telerik:RadDatePicker>` | `TelerikDatePicker` |
| `<telerik:RadTreeView>` | `TelerikTreeView` |
| `<telerik:RadGrid>` | `TelerikGrid` |
| `<telerik:RadComboBox>` | `TelerikComboBox` |
 
---
 
## Complexity Classification Decision Tree
 
```
hasTelerikControls = true?
  YES → complexity = "complex"
  NO  → pageSpecificJsFiles contain "tinymce"?
          YES → complexity = "complex"
          NO  → hasUpdatePanel = true?
                  YES → complexity = "medium"
                  NO  → count(fields) > 15 OR count(userControls) > 2?
                          YES → complexity = "medium"  (if 6–15 fields or ≤2 user controls)
                          NO  → count(fields) ≤ 5?
                                  YES → complexity = "simple"
                                  NO  → complexity = "medium"
```
 
---
 
## URL Pattern Derivation
 
The URL pattern for a Web Forms page is its path relative to the web project root,
using forward slashes:
 
- File: `Profiles.Web/ManageProfile.aspx`
- Web project path: `Profiles.Web/`
- URL pattern: `/ManageProfile.aspx`
 
For pages in subdirectories:
- File: `Profiles.Web/Admin/Settings.aspx`
- URL pattern: `/Admin/Settings.aspx`
 
---
 
## Handling Parse Errors
 
If an `.aspx` file cannot be fully parsed (e.g. malformed markup, missing
referenced files):
 
1. Record the page with `"parseError": true`
2. Record `"parseErrorReason"`: a brief description of what could not be parsed
3. Record as many fields as were successfully extracted before the error
4. Continue to the next page — do not halt
 
---
 
## Output Completeness Check
 
Before setting `"status": "complete"`, verify:
 
- Every `.aspx` file in scope has an entry in `pages[]`
- Every entry has: `name`, `aspxPath`, `codeBehindPath`, `urlPattern`, `title`,
  `complexity`, `hasUpdatePanel`, `hasTelerik`
- The `masterPage` entry has `globalJsFiles`, `globalCssFiles`,
  `embeddedUserControls`
- No entry is missing `fields`, `actions`, `validation`, `telerikControls`,
  `userControls`, `pageSpecificJs` (these may be empty arrays but must be present)
 