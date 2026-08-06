# Requirements to Scrum Board Agent — What It Does & Change Log

**File:** `.github/agents/requirements-to-scrum-board.agent.md`  
**Last reviewed:** 2026-07-31  
**Owner:** LAP Civica Migration Team  
**Standards alignment:** Defra SDS · Defra AI Toolkit — Deliver with AI

---

## What This Agent Does

### Overview

The **Requirements to Scrum Board Agent** converts requirements documents into a CSV file ready for bulk upload to **Jira** or **Azure DevOps**. It extracts one user story per requirement, derives epics by thematic grouping, and applies the correct schema for the chosen platform. It makes **no API calls** — the output is always a CSV file.

---

### Three Governing Rules

| # | Rule | Detail |
|---|---|---|
| 1 | **CSV output only** | The deliverable is always a CSV file — no API calls, MCP calls, or direct issue creation |
| 2 | **Preview before write** | The agent always shows a full CSV preview and waits for explicit user confirmation before writing any file |
| 3 | **One story per requirement** | Every individual requirement maps to exactly one user story |

---

### Supported Input Formats

`.txt`, `.md`, `.docx`, `.pdf`, `.xlsx`, `.xls`, `.csv`, `.pptx` — and pasted plain text.

---

### Output Limits

| Limit | Value | Reason |
|---|---|---|
| Maximum epics | 20 | Keep imports manageable |
| Maximum user stories | 50 | Keep imports manageable |
| Title/summary field | 255 characters max | Truncated with warning if exceeded |

---

### Security Constraints

| Constraint | Detail |
|---|---|
| File access | Only reads files explicitly provided by the user |
| File size | Maximum 1 MB per input file |
| CSV injection prevention | Escapes `=`, `+`, `-`, `@` at the start of any CSV field |
| Path validation | Rejects paths containing `../` traversal sequences |
| No platform schemas mixed | Never mixes Jira and Azure DevOps columns in the same CSV |
| System files | Never reads system files, config files, or files outside project scope |

---

### Outputs Produced

| Output | Description |
|---|---|
| CSV file | Bulk-upload ready for Jira or Azure DevOps, following the platform's required schema |
| CSV preview | Shown to user for approval before the file is written |

---

### CSV Schema — Jira

| Column | Description |
|---|---|
| `Summary` | User story title (one-liner) |
| `Issue Type` | `Story` or `Epic` |
| `Description` | Full user story text in "As a / I want / So that" format |
| `Epic Link` / `Epic Name` | Thematic grouping derived from story analysis |
| `Priority` | Inferred from requirement language |
| `Labels` | Tags derived from requirement categories |

### CSV Schema — Azure DevOps

| Column | Description |
|---|---|
| `Title` | Work item title |
| `Work Item Type` | `User Story` or `Epic` |
| `Description` | Full user story text |
| `Area Path` | If determinable from context |
| `Priority` | Inferred from requirement language |
| `Tags` | Comma-separated tag list |

---

### Guardrails Summary

**The agent will:**
- Show a complete CSV preview before writing any file
- Validate all required columns are populated before writing
- Sanitise requirement text to remove characters that break CSV structure
- Escape formula-injection characters

**The agent will never:**
- Make API calls to Jira, Azure DevOps, or any other platform
- Write a CSV file without explicit user confirmation
- Read files not provided by the user
- Mix Jira and Azure DevOps schemas in the same CSV

---

### Compliance Profile

| Dimension | Classification / Status |
|---|---|
| AI Toolkit risk level | MEDIUM |
| Human oversight required | Yes — preview and confirmation required before every file write |
| Data protection | Requirements documents may contain personal data — validate before processing |
| Defra SDS standards applied | GitHub Copilot guide, Security standards, Common coding standards |

---

## Change Log

### v1.0 — 2026-07-31 — Initial Guide

Initial creation of the Requirements to Scrum Board agent guide.

---

## References

- [`.github/agents/requirements-to-scrum-board.agent.md`](./../agents/requirements-to-scrum-board.agent.md) — agent definition
- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Common coding standards](https://defra.github.io/software-development-standards/standards/common_coding_standards/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Keeping data safe](https://digital.defra.gov.uk/ai-toolkit/guidance/keeping-data-safe)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)

---

*This document is maintained alongside `.github/agents/requirements-to-scrum-board.agent.md`. Update the change log and "Last reviewed" date whenever the agent is modified.*
