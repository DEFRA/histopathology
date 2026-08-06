---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: e2e-baseline
description: >
  Use when you need to generate a Playwright E2E behavioural baseline test suite
  for any pre-migration ASP.NET Web Forms application (VB.NET or C#). Analyses
  all page assets (markup, code-behinds, JavaScript, CSS, user controls) to
  generate a self-contained C# NUnit Playwright test project with a selector
  registry, page objects, and test specs. The baseline proves pre-migration
  behaviour and can be re-run post-migration to verify functional equivalence.
  Invoke the e2e-baseline-orchestrator agent to run the full pipeline.
argument-hint: "Optional: path to existing docs/e2e-baseline/e2e-baseline.config.json"
license: OGL-UK-3.0
metadata:
  author: defra-digital
  version: "1.1"
---
 
# E2E Behavioural Baseline Skill
 
## When to Use
 
- You are migrating an ASP.NET Web Forms application and need a functional
  test baseline before any migration work begins
- You need to prove post-migration functional equivalence with a reproducible,
  signed-off test suite
- You want to generate a Playwright E2E test project from static analysis of
  `.aspx` pages and their associated assets
- The source codebase is VB.NET or C# Web Forms (the output is always C#)
 
## Quick Start
 
Invoke the orchestrator agent:
 
> Start the E2E baseline generation. The web project is at [path to .aspx folder].
 
The orchestrator will collect configuration interactively (or read an existing
config file if you provide one), then run the full pipeline automatically.
You will only be asked to:
1. Whether the pre-migration application is deployed and accessible (sets `executionMode`)
2. Confirm which pages are in scope (after discovery)
3. Sign off the baseline run results (after test execution — **live-run mode only**)
 
## Pipeline Overview
 
The pipeline runs in six ordered phases, each a hard gate:
 
| Phase | Agent | Produces | Mode |
|---|---|---|---|
| 0 — Configuration | orchestrator | `docs/e2e-baseline/e2e-baseline.config.json` | both |
| 1 — Discovery | `e2e-baseline-page-discovery` | `docs/e2e-baseline/page-inventory.json` | both |
| 2 — Scope confirmation | orchestrator (human gate) | Updated `page-inventory.json` with `inScope` flags | both |
| 3 — Per-page processing | `e2e-baseline-selector-extractor` + `e2e-baseline-scenario-synthesiser` + `e2e-baseline-test-generator` | Selector registry, scenario catalogue, C# test files | both |
| 4 — Baseline run | `e2e-baseline-runner` | `docs/e2e-baseline/baseline-results.json` | **live-run only** |
| 5 — Sign-off | `e2e-baseline-validator` + orchestrator (human gate) | `docs/e2e-baseline/baseline-report.md`, signed-off results | **live-run only** |
 
## Reference Documents
 
Loaded progressively — each agent loads only its own reference doc:
 
| Reference | Loaded by |
|---|---|
| `references/discovery-procedure.md` | `e2e-baseline-page-discovery` |
| `references/selector-extraction-rules.md` | `e2e-baseline-selector-extractor` |
| `references/asset-analysis-procedure.md` | `e2e-baseline-scenario-synthesiser` |
| `references/scenario-synthesis-rules.md` | `e2e-baseline-scenario-synthesiser` |
| `references/test-generation-patterns.md` | `e2e-baseline-test-generator` |
| `references/baseline-validation-guide.md` | `e2e-baseline-validator` |
 
## Asset Templates
 
All generated file templates are in `.github/skills/e2e-baseline/assets/`:
 
| Template | Generates |
|---|---|
| `e2e-baseline.config.json.template` | Pipeline configuration |
| `page-inventory.json.template` | Page discovery artefact schema |
| `asset-manifest.json.template` | Per-page file analysis artefact schema |
| `selectors-registry.json.template` | Per-page selector registry schema |
| `E2EProject.csproj.template` | C# test project file |
| `GlobalUsings.cs.template` | Global C# using directives |
| `Routes.cs.template` | URL route constants |
| `TestSettings.cs.template` | Environment-based test settings |
| `BasePageTest.cs.template` | NUnit base test class |
| `TelerikHelper.cs.template` | Telerik DOM interaction helpers |
| `SemanticLocatorExtensions.cs.template` | Playwright locator factory extensions |
| `PageSelectors.cs.template` | Per-page static selector class |
| `PageObject.cs.template` | Per-page Playwright page object |
| `PageSpec.cs.template` | Per-page NUnit test spec |
| `README.md.template` | Generated project README |
 
## Artefact Schema Reference
 
### `e2e-baseline.config.json`
 
```json
{
  "status": "configured",
  "executionMode": "live-run",
  "baseUrl": "http://localhost:5000",
  "webProjectPath": "MyApp.Web/",
  "outputProjectName": "MyApp.Tests.E2E",
  "namespace": "MyApp.Tests.E2E"
}
```
 
`executionMode` is `"live-run"` or `"static-only"`.  
`baseUrl` is `null` when `executionMode` is `"static-only"`.
 
### `page-inventory.json` (top-level)
 
```json
{
  "status": "complete",
  "generatedAt": "ISO-timestamp",
  "webProjectPath": "MyApp.Web/",
  "masterPage": {
    "file": "MyApp.Web/SiteTemplate.master",
    "globalJsFiles": ["MyApp.Web/Javascript/site.js"],
    "globalCssFiles": ["MyApp.Web/Css/site.css"],
    "embeddedUserControls": []
  },
  "pages": [
    {
      "name": "EditProfileTitle",
      "aspxPath": "MyApp.Web/EditProfileTitle.aspx",
      "codeBehindPath": "MyApp.Web/EditProfileTitle.aspx.vb",
      "urlPattern": "/EditProfileTitle.aspx",
      "title": "Edit Profile Title",
      "masterPageFile": "~/SiteTemplate.master",
      "contentPlaceholderId": "cphMain",
      "complexity": "medium",
      "hasUpdatePanel": false,
      "hasTelerik": false,
      "inScope": true,
      "fields": [],
      "actions": [],
      "validation": [],
      "telerikControls": [],
      "userControls": [],
      "pageSpecificJs": []
    }
  ]
}
```
 
### `selector-registry/{PageName}.selectors.json` (top-level)
 
```json
{
  "page": "EditProfileTitle",
  "status": "complete",
  "generatedBy": "e2e-baseline-selector-extractor",
  "generatedAt": "ISO-timestamp",
  "selectors": {
    "fields": {
      "profileTitle": {
        "type": "semantic",
        "locator": "getByLabel",
        "value": "Profile Title",
        "controlType": "TextBox"
      }
    },
    "actions": {
      "save": {
        "type": "role",
        "locator": "getByRole",
        "role": "button",
        "value": "Save",
        "clientConfirm": false
      }
    },
    "feedback": {
      "success": { "type": "text", "locator": "getByText", "value": "Title saved" },
      "errorSummary": { "type": "role", "locator": "getByRole", "role": "alert" }
    },
    "navigation": {
      "pageHeading": { "type": "role", "locator": "getByRole", "role": "heading", "value": "Edit Profile Title" }
    }
  }
}
```
 
### `asset-manifests/{PageName}.assets.json` (top-level)
 
```json
{
  "page": "EditProfileTitle",
  "status": "complete",
  "generatedBy": "e2e-baseline-scenario-synthesiser",
  "generatedAt": "ISO-timestamp",
  "assets": [
    {
      "file": "MyApp.Web/EditProfileTitle.aspx",
      "category": "page-markup",
      "keyBehaviours": ["Contains TinyMCE-enabled div#divProfileTitle"]
    },
    {
      "file": "MyApp.Web/EditProfileTitle.aspx.vb",
      "category": "code-behind",
      "keyBehaviours": [
        "Page_Load registers TinyMCE via RegisterStartupScript",
        "btnSave_Click sets lblMessage.Text = 'Title saved' on success",
        "btnSave_Click calls Response.Redirect('/ManageProfile.aspx?id=...')"
      ]
    }
  ]
}
```
 
### `scenario-catalogue/{PageName}.scenarios.json` (top-level)
 
```json
{
  "page": "EditProfileTitle",
  "status": "complete",
  "generatedBy": "e2e-baseline-scenario-synthesiser",
  "generatedAt": "ISO-timestamp",
  "scenarios": [
    {
      "name": "Save valid profile title",
      "category": "happy-path",
      "assetEvidence": ["MyApp.Web/EditProfileTitle.aspx.vb"],
      "preconditions": "User is logged in and on the Edit Profile Title page",
      "steps": [
        "Type a valid title into the Profile Title field",
        "Click Save"
      ],
      "expectedOutcome": "The page shows 'Title saved' and redirects to Manage Profile",
      "selectorsUsed": ["fields.profileTitle", "actions.save", "feedback.success"],
      "requiresTestData": false,
      "clientSideBehaviour": false,
      "requiresIframeHandling": false,
      "requiresRole": null
    }
  ]
}
```
 
## Evaluation Criteria
 
Use these minimum-quality checks to validate pipeline outputs at each gate:
 
| Artefact | Minimum quality check |
|---|---|
| `page-inventory.json` | Every in-scope page has at least one `fields`, `actions`, or `telerikControls` entry, or a `parseError` flag |
| `selector-registry/{Page}.selectors.json` | At least one selector classified; no `ctl00_`-prefixed IDs marked as `semantic` type |
| `scenario-catalogue/{Page}.scenarios.json` | At least one `happy-path` scenario with at least one `assetEvidence` entry |
| Generated `Specs/{Page}Tests.cs` | Each `[Test]` method includes at least one `Expect(...)` assertion |
| `baseline-results.json` | `passRate` recorded; all `selector-mismatch` failures reviewed before sign-off |
 
## Post-Migration Usage
 
When the migrated application is ready:
 
1. Update `E2E_BASE_URL` environment variable to point at the new app
2. For pages where label text changed: update the value in the relevant
   `docs/e2e-baseline/selector-registry/{PageName}.selectors.json` entry,
   then re-invoke `e2e-baseline-test-generator {PageName}` to regenerate
   the selectors class for that page
3. For Telerik controls replaced with native HTML: update the method body
   in `{OutputProjectName}/Pages/{PageName}Page.cs` to use `GetByLabel()`
   or `GetByRole()` instead of `TelerikHelper` — the spec file is unchanged
4. Run `dotnet test` — all green = functional equivalence proven
 
The `Specs/{PageName}Tests.cs` files are never modified post-migration
unless a business requirement genuinely changed.

---

## Standards

This skill is loaded by the [e2e-baseline-orchestrator agent](./../../agents/e2e-baseline-orchestrator.agent.md). All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/) and [Defra SDS — Quality assurance and test standards](https://defra.github.io/software-development-standards/standards/quality_assurance_standards/).