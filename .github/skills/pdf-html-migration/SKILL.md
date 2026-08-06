---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: pdf-html-migration
description: >
  Use when replacing a programmatic PDF generation library (TallPDF, iText,
  FastReport, PdfSharp, Syncfusion, Crystal Reports, Aspose) with an HTML-first
  approach using ASP.NET Core Razor views rendered to PDF via Playwright headless
  Chromium. Covers discovery of all report classes, reference baseline generation,
  infrastructure setup, per-report Razor conversion, chart library replacement,
  visual regression validation, and old library decommission. Generic across
  .NET codebases. Invoke the pdf-migration-orchestrator agent to run the pipeline.
argument-hint: "Name of the PDF library being replaced (e.g. TallPDF, iText)"
---
 
# PDF Library → HTML-First Migration Skill
 
## When to Use
 
- You are replacing a Windows-only or legacy PDF library with a cross-platform
  HTML-first approach
- You need to migrate to Linux containers and the existing PDF library cannot run
  on Linux
- You want to produce PDF reports from Razor views using Playwright headless Chromium
- The codebase is .NET (VB.NET or C#), targeting ASP.NET Core
 
## Pipeline Overview
 
The migration runs in five ordered phases, each a hard gate:
 
| Phase | Agent | Produces |
|---|---|---|
| 1 — Discovery | `pdf-discovery` | `report-inventory.json` |
| 2 — Scope confirmation | orchestrator | confirmed report list |
| 3 — Infrastructure | `pdf-infrastructure` | `IPlaywrightPdfService`, `_PdfLayout.cshtml`, smoke test |
| 4 — Conversion + Static Review | `pdf-report-converter` + `pdf-validation` | Razor view, view model, xUnit tests, static review report |
| 5 — Wrap-Up | orchestrator | ADR, manual checklist printed |
 
## Quick Start
 
Invoke the orchestrator agent:
 
> Start the PDF library migration. The library being replaced is [TallPDF / iText / etc.].
 
The orchestrator will guide you through each phase. The only inputs required
from you are:
- Confirming the scope of reports to migrate (Phase 2)
- Reviewing any static review failures and confirming fixes (Phase 4)
- Performing manual visual sign-off of rendered reports before go-live (post-migration)
 
## Reference Documents
 
Loaded progressively by agents as each phase becomes active:
 
| Document | Loaded by | Purpose |
|---|---|---|
| [discovery-procedure.md](./references/discovery-procedure.md) | pdf-discovery | Library fingerprinting, report class scanning, complexity scoring |
| [obtaining-test-profile-ids.md](./references/obtaining-test-profile-ids.md) | Not loaded by any agent — developer reference only | Placeholder for application-specific SQL queries to obtain test entity IDs for manual visual sign-off. Must be populated by the team before post-migration sign-off. |
| [playwright-pdf-pipeline.md](./references/playwright-pdf-pipeline.md) | pdf-infrastructure | `IPlaywrightPdfService` design, concurrency, Docker |
| [razor-layout-guide.md](./references/razor-layout-guide.md) | pdf-infrastructure | CSS print rules, watermark, TOC, portrait/landscape |
| [report-conversion-patterns.md](./references/report-conversion-patterns.md) | pdf-report-converter | Four archetypes, TallPDF → Razor translation table |
 
## Asset Templates
 
Used by agents to create implementation files:
 
| Template | Used by | Output |
|---|---|---|
| [PlaywrightPdfService.cs.template](./assets/PlaywrightPdfService.cs.template) | pdf-infrastructure | `Services/Pdf/PlaywrightPdfService.cs` |
| [_PdfLayout.cshtml.template](./assets/_PdfLayout.cshtml.template) | pdf-infrastructure | `Views/Shared/_PdfLayout.cshtml` |
| [report-inventory.json.template](./assets/report-inventory.json.template) | pdf-discovery | `report-inventory.json` |
 
## Key Design Decisions
 
**One report per converter invocation** — keeps each PR small and independently
reversible, matching the incremental-refactoring guardrails.
 
**Validation is blocking** — the orchestrator cannot proceed to the next report
until the current one passes all static code review checks.
 
**Reference baseline is committed first** — before any code change, the old
library's output is in version control and auditable.

**Use the smallest model appropriate to each phase** — per [Defra AI Toolkit — Sustainability](https://digital.defra.gov.uk/ai-toolkit/guidance/sustainability), use the smallest model that meets each phase's needs. Discovery (Step 1) and static review validation (Step 4) are pattern-matching and file-inspection tasks that do not require a frontier model. Infrastructure setup (Step 3) and report conversion (Step 4) involve generating new C# and Razor code and benefit from a larger model.

**Generic by design** — the discovery agent uses heuristic patterns, not
library-specific knowledge. The conversion patterns describe constructs by
function, not by TallPDF class name. The same skill works for iText, FastReport,
and Crystal Reports codebases.

---

## Standards

This skill is loaded by the [pdf-migration-orchestrator agent](./../../agents/pdf-migration-orchestrator.agent.md). All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/).