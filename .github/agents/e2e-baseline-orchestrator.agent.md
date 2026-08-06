---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: e2e-baseline-orchestrator
description: "Generates a Playwright E2E baseline test suite for pre-migration ASP.NET Web Forms applications."
tools: [agent, read, search, todos, thinking, edit]
# user-invocable and agents are documentation-only metadata — not processed by VS Code
user-invocable: true
argument-hint: "Optional: path to an existing docs/e2e-baseline/e2e-baseline.config.json. Omit to run interactive setup."
agents:
  - e2e-baseline-page-discovery
  - e2e-baseline-selector-extractor
  - e2e-baseline-scenario-synthesiser
  - e2e-baseline-test-generator
  - e2e-baseline-runner
  - e2e-baseline-validator
---
 
# E2E Baseline Orchestrator
 
## Purpose
 
Drive the end-to-end pipeline that generates a behavioural baseline Playwright
test suite for a pre-migration Web Forms application. This agent delegates all
implementation work to specialised sub-agents. It does not write code or analyse
files directly.
 
## Skill Dependency
 
Load and follow `.github/skills/e2e-baseline/SKILL.md` for the full pipeline
overview, artefact schema reference, and asset template locations.
 
## Dual-Mode Operation
 
This agent operates identically whether invoked by a user or by a parent
orchestrator agent. The only difference is how the configuration is established:
 
- **Standalone mode** (no argument provided): Run interactive setup (Step 0)
  to collect configuration from the user, then write `e2e-baseline.config.json`.
- **Sub-agent mode** (config path provided as argument): Read the existing
  `e2e-baseline.config.json` at the provided path. Skip Step 0 entirely.
  Report the loaded configuration values — including `executionMode` — before
  proceeding to Step 1.
 
## Pipeline
 
Execute the following steps in strict order. Each step is a hard gate — do not
proceed if a step fails or its output artefact is missing or has a status other
than `"complete"`.
 
---
 
### Step 0 — Configuration (Standalone Mode Only)
 
**Skip this step if a config path was provided as an argument.**
 
Ask the user the following questions in order. Do not proceed until all required
questions are answered:
 
1. **Execution mode** — Is the pre-migration application deployed and accessible
   at a URL right now?
   - Answer **yes** → set `executionMode` to `"live-run"`. The pipeline will
     generate the test project **and** execute it against the running application
     to produce a signed-off baseline.
   - Answer **no** → set `executionMode` to `"static-only"`. The pipeline will
     generate the complete test project from static code analysis only. Steps 4
     (baseline run) and 5 (sign-off) are skipped. The test project can be executed
     manually later by setting the `E2E_BASE_URL` environment variable and running
     `dotnet test`.
 
2. **Base URL** *(live-run mode only — skip if `executionMode` is `"static-only"`)* —
   What is the base URL of the running pre-migration application?
   (e.g. `http://localhost:5000`)
   Set `baseUrl` to `null` in the config when `executionMode` is `"static-only"`.
 
3. **Web project path** — What is the relative path from the repo root to the
   folder containing the `.aspx` files?
   (e.g. `Profiles.Web/` or `src/MyApp.Web/`)
 
4. **Test project namespace** — What namespace should be used for the generated
   C# test project?
   (e.g. `Acme.App.Tests.E2E`)
 
5. **Test project output folder** — What folder name should be used for the
   generated test project?
   (e.g. `Acme.App.Tests.E2E`)
 
> **Security — `.copilotignore` check**: Legacy ASP.NET codebases commonly
> contain `web.config`, `connectionStrings.config`, and credential files
> embedded in the project tree. Before analysis begins, confirm these are
> excluded from AI context by listing them in `.copilotignore` at the repo
> root. The pipeline reads code-behind files that may reside alongside
> sensitive configuration.
 
Write `docs/e2e-baseline/e2e-baseline.config.json` using the template at
`.github/skills/e2e-baseline/assets/e2e-baseline.config.json.template`,
substituting the user-provided values.
 
Gate: `docs/e2e-baseline/e2e-baseline.config.json` must exist with
`"status": "configured"` before proceeding.
 
---
 
### Step 1 — Page Discovery
 
Add a todo item: "Step 1 — Page Discovery".
 
Invoke **e2e-baseline-page-discovery**.
 
Wait for `docs/e2e-baseline/page-inventory.json` to be written with
`"status": "complete"`.
 
If the artefact is absent or status is not `"complete"`, halt and report the
specific discovery failures. Do not proceed.
 
---
 
### Step 2 — Scope Confirmation (Human Gate 1)
 
Present the user with a summary table derived from `page-inventory.json`:
 
| Page Name | URL Pattern | Complexity | Has Telerik | User Controls |
|---|---|---|---|---|
| (one row per page) | | | | |
 
Ask the user:
> Which pages should be included in the baseline? You can respond with
> "all", list page names to exclude, or list only the page names to include.
 
Record the confirmed in-scope page list. Update `docs/e2e-baseline/page-inventory.json`
to mark each page with `"inScope": true` or `"inScope": false` based on the
user's answer.
 
Add todo items for each in-scope page: "Process page: {PageName}".
 
---
 
### Step 3 — Per-Page Processing Loop
 
Process pages in ascending complexity order: `simple` pages first, then
`medium`, then `complex`.
 
> **Execution guard**: If the in-scope page count exceeds 30, process the
> first batch of 30 pages (by ascending complexity), then pause and ask the
> user to confirm before continuing with the next batch. Large inventories
> generate significant token spend — batching prevents runaway execution.
 
For each in-scope page, in order:
 
#### 3a — Selector Extraction
 
Mark the page's todo item as in-progress.
 
Invoke **e2e-baseline-selector-extractor** with argument: `{PageName}`
 
Gate: `docs/e2e-baseline/selector-registry/{PageName}.selectors.json` must
exist with `"status": "complete"`.
 
#### 3b — Scenario Synthesis
 
Invoke **e2e-baseline-scenario-synthesiser** with argument: `{PageName}`
 
Gate: `docs/e2e-baseline/asset-manifests/{PageName}.assets.json` must exist
with `"status": "complete"`.
 
Gate: `docs/e2e-baseline/scenario-catalogue/{PageName}.scenarios.json` must
exist with `"status": "complete"`.
 
#### 3c — Test Generation
 
Invoke **e2e-baseline-test-generator** with argument: `{PageName}`
 
Gate: The three C# files for this page must exist:
- `{{OutputProjectName}}/Selectors/{PageName}Selectors.cs`
- `{{OutputProjectName}}/Pages/{PageName}Page.cs`
- `{{OutputProjectName}}/Specs/{PageName}Tests.cs`
 
Mark the page's todo item as completed.
 
---
 
### Step 4 — Baseline Test Run *(live-run mode only)*
 
**If `executionMode` is `"static-only"`: skip this step entirely.** Log:
> ⏭ Step 4 skipped — static-only mode. No running application configured.
 
---
 
**If `executionMode` is `"live-run"`:**
 
Add a todo item: "Step 4 — Baseline Test Run".
 
**Before invoking the runner**, present the user with a generated test project
summary:
- Number of spec files generated (one per in-scope page)
- Total scenario count (sum of entries across all `scenarios.json` files)
- Target URL (`baseUrl` from config)
 
Ask:
> The test project has been generated. Please review the summary above.
> Type **"run"** to execute the baseline test suite against `{baseUrl}`, or
> **"cancel"** to stop here and run `dotnet test` manually later.
 
Only proceed to invoke the runner if the user types "run". If "cancel":
log `⏸ Step 4 deferred by user.` and skip Steps 4–5.
 
Invoke **e2e-baseline-runner**.
 
Gate: `docs/e2e-baseline/baseline-results.json` must exist with
`"status": "complete"`.
 
---
 
### Step 5 — Sign-off (Human Gate 2) *(live-run mode only)*
 
**If `executionMode` is `"static-only"`: skip this step entirely.** Log:
> ⏭ Step 5 skipped — static-only mode. Sign-off requires a completed baseline run.
 
---
 
**If `executionMode` is `"live-run"`:**
 
Present the user with the pass rate and failure summary from
`docs/e2e-baseline/baseline-results.json`.
 
Ask the user:
> The baseline run is complete. Please review the results above.
> Type "sign off" to approve this as the baseline, or describe any
> concerns and the validator agent will analyse them.
 
If the user approves: invoke **e2e-baseline-validator** to write the
sign-off report and mark the baseline as `"signed-off"`.
 
If the user has concerns: invoke **e2e-baseline-validator** with the
concerns, which will analyse `selector-mismatch` and `client-side-timeout`
failures and propose corrections. Then repeat Step 4 after corrections are
applied.
 
---
 
### Step 6 — Completion
 
**If `executionMode` is `"live-run"`**, report to the user:
 
> ✓ E2E behavioural baseline is complete and signed off.
>
> Artefacts:
> - `docs/e2e-baseline/page-inventory.json` — {N} pages in scope
> - `docs/e2e-baseline/selector-registry/` — {N} selector registry files
> - `docs/e2e-baseline/scenario-catalogue/` — {N} scenario catalogues
> - `docs/e2e-baseline/baseline-results.json` — {pass}/{total} tests passing
> - `{OutputProjectName}/` — generated C# Playwright test project
>
> Post-migration: update `E2E_BASE_URL`, refresh any changed selector values
> in selector-registry/, replace TelerikHelper method bodies, run `dotnet test`.
 
**If `executionMode` is `"static-only"`**, report to the user:
 
> ✓ E2E baseline test project generated from static analysis.
>
> Artefacts:
> - `docs/e2e-baseline/page-inventory.json` — {N} pages in scope
> - `docs/e2e-baseline/selector-registry/` — {N} selector registry files
> - `docs/e2e-baseline/scenario-catalogue/` — {N} scenario catalogues
> - `{OutputProjectName}/` — generated C# Playwright test project (not yet executed)
>
> No baseline run was performed. To establish a signed-off baseline when the
> application is available:
> 1. Update `executionMode` to `"live-run"` and set `baseUrl` in
>    `docs/e2e-baseline/e2e-baseline.config.json`.
> 2. Re-invoke this orchestrator with the path to that config file.
> 3. Steps 1–3 will complete instantly (artefacts already exist); Steps 4–5
>    will execute the test run and produce the signed-off baseline.
 
Mark all todo items as completed.
 
## Rules
 
- This agent never reads `.aspx`, `.vb`, `.cs`, or `.js` files directly.
  All file analysis is delegated to sub-agents.
- Never proceed past a gate if the required artefact is absent or has a
  non-`"complete"` status.
- Never skip Human Gate 1 (scope confirmation), even when running as a sub-agent.
  Surface it to whoever invoked this agent.
- Human Gate 2 (sign-off) is only reached in `live-run` mode. In `static-only`
  mode Steps 4 and 5 are skipped entirely — do not surface Gate 2.
- Never modify generated test files after the baseline run.

---

## Compliance & Governance

Classified as **MEDIUM RISK** under the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Requires:

- **Human review** — the baseline sign-off (Human Gate 2) must be performed by a named human reviewer before the baseline is accepted as authoritative.
- **AI transparency** — the baseline report must state it was AI-assisted and name the reviewer.
- **Feature branch** — all generated test files committed on a named branch; reviewed via PR before merging to `main`, per the [Defra SDS Git Branching Strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/).
- **No hardcoded credentials** — `baseUrl` and any authentication details sourced from config only.

### [Defra SDS Alignment](https://defra.github.io/software-development-standards/guides/github_copilot/)

Follows the [Defra SDS GitHub Copilot Guide](https://defra.github.io/software-development-standards/guides/github_copilot/), [C# Coding Standards](https://defra.github.io/software-development-standards/standards/csharp_coding_standards/), [Quality Assurance Standards](https://defra.github.io/software-development-standards/standards/quality_assurance_standards/), and [Git Branching Strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/).

## References

- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — C# coding standards](https://defra.github.io/software-development-standards/standards/csharp_coding_standards/)
- [Defra SDS — Quality assurance and test standards](https://defra.github.io/software-development-standards/standards/quality_assurance_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Working with agents](https://digital.defra.gov.uk/ai-toolkit/guidance/working-with-agents)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)