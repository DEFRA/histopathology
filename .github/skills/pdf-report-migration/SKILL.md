---
name: pdf-report-migration
description: Modernises legacy report files (Crystal Reports, TallPDF, etc.) to Azure-safe PDF output for .NET ASP.NET applications. Extracts report metadata, generates HTML templates, and produces renderers. Supports pre-migration and post-migration split workflow.
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
---
# PDF Report Modernizer – Skills Specification
 
> **Last reviewed: 2026-07-29** — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
 
This document defines the core skills required by the agent.
 
## Skill List
- RptDefinitionGenerationSkill
- HtmlTemplateGenerationSkill
- HtmlToPdfConversionSkill
 
## Batch Execution Config
- Default batch size: 4–6 reports per run.
- Process one skill across the whole batch before moving to the next skill.
- Each skill is idempotent: skip files that already have a matching output (definition, template, renderer).
- Log summary only (filename + pass/fail); write full errors to `output/logs/failures.json`.
- Commit after every skill stage as a checkpoint.
 
## Skill Flow (with consumes → outputs)

Paths use the agent input parameters `rptSourceFolder` (default `reports/`) and `reportOutputFolder` (default `output/`). For `modern` paradigm Stage 3, the renderer is placed inside `srcFolder`.

```
RptDefinitionGenerationSkill
  consumes: {rptSourceFolder}/{ReportName}.rpt
  outputs:  {reportOutputFolder}/definition/{ReportName}.ReportDefinition.json

HtmlTemplateGenerationSkill
  consumes: {reportOutputFolder}/definition/{ReportName}.ReportDefinition.json
  outputs:  {reportOutputFolder}/templates/{ReportName}.html
           (+ copy to {srcFolder}/wwwroot/reports/{ReportName}.html  — modern paradigm only)

HtmlToPdfConversionSkill
  consumes: {reportOutputFolder}/templates/{ReportName}.html
  outputs:  {reportOutputFolder}/renderer/{ReportName}Renderer.vb   (classic paradigm)
         OR {srcFolder}/Reports/{ReportName}Renderer.cs              (modern paradigm)
```

> **Stage skip rule:** Before invoking any skill, the agent checks whether the output already exists.
> - Stage 1 skip: `{reportOutputFolder}/definition/{ReportName}.ReportDefinition.json` exists → reuse.
> - Stage 2 skip: `{reportOutputFolder}/templates/{ReportName}.html` exists → reuse.
> - Stage 3 skip (`modern`): `{srcFolder}/Reports/{ReportName}Renderer.cs` exists → skip.
> - Stage 3 **no skip** (`modern`): only a `.vb` renderer exists → regenerate Stage 3 as C# renderer.

---

## Standards

This skill is loaded by the pdf-report-migration agents. All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/).
