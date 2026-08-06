# Asset Analysis Procedure
 
Reference document for `e2e-baseline-scenario-synthesiser` (Phase A).
Loaded together with `scenario-synthesis-rules.md`.
 
## Purpose
 
Define the complete procedure for resolving and reading every file associated
with a single page before scenario generation begins. The asset manifest is
the evidence base that justifies every generated scenario.
 
---
 
## Asset Resolution Order
 
Read files in this exact order. Each category may reveal additional files
to add to the set (e.g. code-behind `Imports` statements reveal business
class files). Always complete one category before starting the next.
 
### 1. Page Markup (`.aspx`)
 
Already inventoried. Confirm by re-reading:
- Inline `<script>` blocks within the page's `ContentPlaceHolder` content
  (not master page scripts)
- `OnClientClick` attribute values on every button/link — record the full
  string (e.g. `"app.confirmPrompt('Are you sure you want to delete?', this); return false;"`)
- `onkeypress`, `onblur`, `oninput`, `onchange` attribute values on inputs
- `<a href="javascript:...">` inline JS calls
 
### 2. Code-Behind (`.aspx.vb` or `.aspx.cs`)
 
Read the full code-behind file. Extract:
 
**Event handlers** — every `Sub`/`void` method bound to a server control event:
- `Page_Load` — what is initialised; any `RegisterStartupScript` calls
- Button click handlers — what action is performed; success message; redirect target
- Grid command handlers (`RowCommand`, `RowDeleting`, etc.) — command names handled
- Dropdown `SelectedIndexChanged` — what is reloaded/updated
- UpdatePanel triggers
 
**Authorization checks** — any pattern matching:
```vb
' VB.NET
If Not User.IsInRole("RoleName") Then ...
If Not ProfilesPrincipal.IsInRole("RoleName") Then ...
Response.Redirect("/Unauthorised.aspx")
```
```csharp
// C#
if (!User.IsInRole("RoleName")) ...
if (!context.User.IsInRole("RoleName")) ...
```
Record each role name found.
 
**`Response.Redirect` calls** — record the target URL string.
 
**ScriptManager registration calls** — any of:
```vb
ScriptManager.RegisterStartupScript(...)
Page.ClientScript.RegisterStartupScript(...)
Page.ClientScript.RegisterClientScriptBlock(...)
```
Record the script content (or key name if content is a variable).
 
**`Imports`/`using` namespaces** that point to service or business classes in
this solution. Add those class files to the asset set (see Step 6).
 
**Empty/null state handling** — any check on a collection before binding:
```vb
If results Is Nothing OrElse results.Count = 0 Then
    pnlEmpty.Visible = True
```
Record the condition and the visible element affected.
 
### 3. Designer File (`.aspx.designer.vb` or `.aspx.designer.cs`)
 
Read for control field declarations. This confirms all server control IDs
and their declared types. Cross-reference with the inventory to catch any
controls the markup scan missed.
 
Extract the complete list of `Protected WithEvents`/`protected` control
declarations as a validation check only. Do not use designer file information
for scenario generation — use code-behind and markup instead.
 
### 4. User Controls (`.ascx` files)
 
For each `.ascx` file listed in the page inventory's `userControls`:
 
Read the `.ascx` markup:
- All `<asp:*>` controls with their attributes
- Inline `<script>` blocks
- `<script src="...">` references — add each to the asset set
- `OnClientClick`, `onkeypress`, etc. attributes
 
Read the `.ascx.vb` or `.ascx.cs` code-behind:
- All `ScriptManager.RegisterStartupScript` calls — record the JS key and content
- Events exposed to parent page
- Any `Response.Redirect` calls
 
Notable user controls and what to look for:
 
| User Control | Behaviour to extract |
|---|---|
| `SavePrompt.ascx` | Presence confirms unsaved-changes navigation guard scenario |
| `Paginator.ascx` | Presence confirms pagination scenario (if parent has grid) |
| `PaginatorPageSize.ascx` | Presence confirms page-size-change scenario |
| `SpeciesSelector.ascx` | Presence + TreeView confirms tree selection scenario |
| `ConfirmDialog.ascx` | Used by master; confirms Bootstrap modal is available |
| `NavigationLinks.ascx` | Read for navigation link text (used in navigation scenarios) |
 
### 5. Master Page Code-Behind (`.master.vb` or `.master.cs`)
 
Read the master page code-behind for:
- All `RegisterStartupScript` or `ClientScript.Register*` calls
- Any method called `RegisterTinyMce` or similar — confirms TinyMCE is loaded
  globally on every page (not page-specifically)
- Session timeout handling (e.g. `Session_End` or timer-based redirect)
 
The global JS files loaded by the master page are already in the inventory.
This step confirms which are always active vs. conditionally loaded.
 
### 6. Page-Specific JavaScript Files
 
For each file in the page inventory's `pageSpecificJs` list:
 
Read the full JavaScript file. Extract:
 
**Function/class definitions** — record the names of all exported functions,
classes, and objects (e.g. `app.searchPage`, `TreeViewController`).
 
**`OnClientClick` target functions** — confirm the functions called from
markup `OnClientClick` attributes exist in this file and understand their behaviour:
- Does the function show a modal? → modal scenario
- Does the function submit the form? → form submission scenario
- Does the function intercept navigation? → navigation guard scenario
 
**`$(document).ready(...)` blocks** — what is initialised on page load.
 
**`__doPostBack(...)` calls** — which server control is triggered and with what
argument (identifies UpdatePanel trigger targets).
 
**`$find(...).show()/.hide()`** — `ModalPopupExtender` control patterns.
 
**`fetch(...)` or `$.ajax(...)` calls** — async data loading patterns; record
the URL pattern (identifies WebService dependencies).
 
**`ScriptManager.registerAsyncPostBackControl`** — records which controls
trigger partial postbacks.
 
### 7. User Control JavaScript Files
 
For each `.ascx` file processed in Step 4, if its markup contained
`<script src="...">` tags, read those JavaScript files using the same
extraction procedure as Step 6.
 
### 8. Global JavaScript Files (Selective)
 
From the master page's `globalJsFiles` list, read only the files that are
referenced by `OnClientClick` or `RegisterStartupScript` content found in
previous steps. Do not read all global JS files — only the ones that are
evidenced as being called from this specific page.
 
Common global files and what they provide:
 
| File | Provides |
|---|---|
| `modal.js` | `app.confirmPrompt(message, element)` — Bootstrap confirm modal |
| `profiles-template.js` | `HideShowBehaviour`, navigation menu behaviour |
| `asp-to-gds-styling.js` | `aspRadioButtonListGdsStyling()`, `aspCheckboxListGdsStyling()` |
| `profiles.js` | `app.profiles.*` general helpers |
| `save-prompt.js` | Navigation interception when `SavePrompt.ascx` is present |
 
### 9. Referenced WebService Files
 
If any JavaScript from Steps 6 or 7 contains `$.ajax({url: "..."})` or
`fetch("...")` calls pointing to `.asmx` or `/api/` endpoints in this project:
 
Read the WebService or API handler file to understand:
- What parameters it accepts
- What it returns (success/error responses)
- Whether it validates input server-side
 
This informs async validation scenarios.
 
### 10. Business and Service Classes
 
From the code-behind `Imports`/`using` statements (Step 2), identify any
service or business class files within this solution (not framework assemblies).
 
For each referenced service class, read:
- Method signatures called from the code-behind
- Any exception types thrown on invalid input
- Any string constants used for error messages or status messages
 
Limit this to **direct dependencies only** (depth 1). Do not follow transitive
dependencies.
 
---
 
## Asset Manifest Completeness Check
 
Before writing the manifest with `"status": "complete"`, verify:
 
- At minimum: page markup, code-behind, and at least one JavaScript file (global
  or page-specific) have been read
- Every file in the manifest has `"keyBehaviours"` populated (not an empty array)
  — if a file contained nothing relevant, record `"keyBehaviours": ["No behaviours relevant to test scenarios found"]`
- The `"category"` field uses one of the defined values:
  `page-markup`, `code-behind`, `designer`, `user-control`, `user-control-code-behind`,
  `master-code-behind`, `page-js`, `user-control-js`, `global-js`,
  `webservice`, `business-class`
 
---
 
## C# Output Rule
 
This agent reads VB.NET source files (`.vb`) and C# source files (`.cs`) equally.
When recording `keyBehaviours`, always describe the behaviour in plain English
regardless of the source language. Never include VB or C# syntax in the
`keyBehaviours` strings — these are scenario inputs, not code.