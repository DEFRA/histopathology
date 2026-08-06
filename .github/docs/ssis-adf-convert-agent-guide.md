# SSIS to ADF Migration Agent — What It Does & Change Log

**File:** `.github/agents/ssis-adf-convert-agent.md`  
**Last reviewed:** 2026-07-31  
**Owner:** LAP Civica Migration Team  
**Standards alignment:** Defra SDS · Defra AI Toolkit — Deliver with AI

---

## What This Agent Does

### Overview

The **SSIS to ADF Migration Agent** converts SSIS (SQL Server Integration Services) packages to Azure Data Factory artifacts. It covers the full migration lifecycle: package analysis, artifact generation, and migration reporting. One SSIS package (`.dtsx`) is processed per execution.

---

### Modes

| Mode | Triggers | What happens |
|---|---|---|
| **Auto mode** | "migrate ssis", "convert ssis to adf", "full conversion", "generate all artifacts" | Runs all 3 phases sequentially; pauses after analysis for confirmation before generating artifacts |
| **Manual mode** | Single phase keyword | Runs one phase in isolation |

#### Manual Mode Phase Routing

| User intent | Phase file |
|---|---|
| analysis only | `01-ssis-analysis.md` |
| conversion / generate artifacts | `02-ssis-to-adf-artifact-conversion.md` |
| reporting only | `03-migration-reporting.md` |

---

### Skills / Phase Files

| Phase | File | Purpose |
|---|---|---|
| 1 | `.github/skills/ssis-adf-convert/01-ssis-analysis.md` | Analyse SSIS package: extract control flow, data flow, connections, variables, parameters |
| 2 | `.github/skills/ssis-adf-convert/02-ssis-to-adf-artifact-conversion.md` | Generate ADF JSON artifacts: pipelines, datasets, linked services, triggers |
| 3 | `.github/skills/ssis-adf-convert/03-migration-reporting.md` | Produce migration report with mapping table, gaps, and manual migration items |

---

### Inputs Required

| Input | Description |
|---|---|
| `packagePath` | Path to a single `.dtsx` file — one package per execution |
| Target ADF details | `subscription`, `resource group`, `factory name`, `region` |
| Source/sink technologies | Database types, file formats, naming conventions |
| Target resource IDs | For managed identity and RBAC planning |

> If multiple packages are provided, the agent asks the user to select one and stops — it does not process multiple packages in a single run.

---

### Critical Gate

After Phase 1 the agent always pauses and presents the analysis report. The user must explicitly confirm before Phase 2 begins.

---

### Outputs Produced

| Output | Description |
|---|---|
| SSIS analysis report | Package inventory, control flow summary, connection list, variable/parameter catalogue |
| ADF pipeline JSON | Pipeline definition for the migrated package |
| ADF dataset JSON | Source and sink dataset definitions |
| ADF linked service JSON | Connection definitions with managed identity placeholders |
| ADF trigger JSON | Schedule/event trigger definitions |
| Migration report | Full mapping table, confidence ratings (High/Medium/Low), "Manual Migration Needed" section |

---

### Confidence Rating Model

All SSIS-to-ADF mappings are tagged with confidence (High / Medium / Low). Uncertain or partial mappings are surfaced in the "Manual Migration Needed" section of the report.

---

### Token Budget Controls

| Rule | Detail |
|---|---|
| Compact Mode | Default for all phases — concise responses |
| No raw XML in chat | Full SSIS XML is never printed in the chat; written to files only |
| No full JSON in chat | Artifact JSON written to files; chat returns only: package name, status, counts, file paths |
| Per-package chat target | ≤ 250 output tokens in chat per package |
| Single package per run | Never load or analyze a package folder recursively in one run |

---

### Safety Rules

- Never include credentials or secrets in output artifacts
- Use placeholders for unresolved IDs (e.g., `principalId` before deployment)
- Mark uncertain mappings with confidence ratings
- Include a "Manual Migration Needed" section when mappings are partial

---

### Compliance Profile

| Dimension | Classification / Status |
|---|---|
| AI Toolkit risk level | MEDIUM |
| Human oversight required | Yes — confirmation gate after analysis; review all Manual Migration items |
| Defra SDS standards applied | GitHub Copilot guide, Security standards, Git branching |

---

## Change Log

### v1.0 — 2026-07-31 — Initial Guide

Initial creation of the SSIS to ADF Migration agent guide.

---

## References

- [`.github/agents/ssis-adf-convert-agent.md`](./../agents/ssis-adf-convert-agent.md) — agent definition
- [Azure Data Factory documentation](https://learn.microsoft.com/en-us/azure/data-factory/)
- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Security guidance](https://digital.defra.gov.uk/ai-toolkit/guidance/security)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)

---

*This document is maintained alongside `.github/agents/ssis-adf-convert-agent.md`. Update the change log and "Last reviewed" date whenever the agent is modified.*
