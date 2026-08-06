# Documentation LLD Agent — What It Does & Change Log

**File:** `.github/agents/documentation-lld.agent.md`  
**Last reviewed:** 2026-07-31  
**Owner:** LAP Civica Migration Team  
**Standards alignment:** Defra SDS · Defra AI Toolkit — Deliver with AI

---

## What This Agent Does

### Overview

The **Documentation LLD Agent** reads HLSA and HLD source documents and fills every placeholder, table, and figure caption in an LLD template, saving the result as a new `.docx` file. It is specific to the four Civica applications: **BSE, Histo, D2R2, and PTLIMS**.

The agent never invents facts — it uses only information from the HLSA and HLD source files. All content is cited to the source slide or section. If information is missing, it writes `[NEEDS INPUT: <detail>]` rather than guessing.

---

### Inputs Required

| # | File | Format | Role |
|---|---|---|---|
| 1 | HLSA | `.pptx` or `.docx` | Architecture overview source |
| 2 | HLD | `.pptx` or `.docx` | High-level design source — actual extension checked at runtime |
| 3 | LLD Template | `.docx` | Template with `<placeholder>` tokens and empty tables |
| 4 | Instructions file | `.md` | Project-specific naming rules and overrides |

---

### Processing Steps

| Step | Action |
|---|---|
| 1 | **Read sources** — extract all text from HLSA, HLD, and template before writing anything |
| 2 | **Inventory template** — list every `<placeholder>` token, every table (by index), and every `Figure N:` caption |
| 3 | **Apply instructions** — use the `.md` rules file; mark manual-only sections `[MANUAL REVIEW REQUIRED]` |
| 4 | **Write content** — replace placeholders and fill cells from source text only; insert Mermaid diagrams under every `Figure N:` caption |
| 5 | **Save output** — write `docs/LLD-{App}-Populated.docx` (never overwrite the blank template) |
| 6 | **Write summary** — write `docs/LLD-{App}-Summary.md` companion file |

**Three-pass population strategy:**
- **Pass 1 (short tokens < 200 chars):** Word Find & Replace
- **Pass 2 (long text ≥ 200 chars):** Locate token and overwrite directly — never use the replacement field for long content (Word silently truncates at 255 chars)
- **Pass 3 (tables):** Populate cells using the table index map from Step 2

---

### Outputs Produced

| Output | Path | Description |
|---|---|---|
| Populated LLD | `docs/LLD-{App}-Populated.docx` | Fully filled LLD document — blank template never overwritten |
| Summary companion | `docs/LLD-{App}-Summary.md` | Auto-filled sections, manual review flags, needs-input list, assumptions, confidence table |

---

### Summary Companion File Structure (`LLD-{App}-Summary.md`)

- `# LLD Generation Summary` + `Generation date: <ISO 8601>`
- `## Migration Details`
- `## Document Generation Summary`
  - `### Sections Auto-Filled`
  - `### Sections Requiring Manual Review`
  - `### Needs Input`
  - `### Assumptions`
- `## Confidence by Section` — table with Section, Confidence (High/Medium/Low), Notes

---

### Sections That Always Require Human Review

- **Security Design** — marked `[MANUAL REVIEW REQUIRED]`
- **Risks and Open Questions** — marked `[MANUAL REVIEW REQUIRED]`

---

### Guardrails Summary

**The agent will:**
- Use only information from HLSA and HLD source files — no invented facts
- Cite every claim to the source slide number or HLD section
- Check the actual file extension of the HLD before parsing (may be `.pptx` or `.docx`)
- Write script files with UTF-8 encoding before running them (inline scripts with special characters cause parse errors)
- Skip TOC and List of Figures entries when searching for body section headings

**The agent will never:**
- Overwrite the blank LLD template
- Leave any placeholder token, table cell, or figure caption unfilled (uses `[NEEDS INPUT]` if data is missing)
- Add a Document Generation Summary section to the DOCX body (summary goes in the companion `.md` file only)

---

### Compliance Profile

| Dimension | Classification / Status |
|---|---|
| AI Toolkit risk level | MEDIUM |
| Human oversight required | Yes — Security Design and Risks sections always require manual review |
| Data protection | Do not process documents containing personal data without a confirmed Defra data handling agreement |
| Defra SDS standards applied | GitHub Copilot guide, Security standards, Git branching |

---

## Change Log

### v1.0 — 2026-07-31 — Initial Guide

Initial creation of the Documentation LLD agent guide.

---

## References

- [`.github/agents/documentation-lld.agent.md`](./../agents/documentation-lld.agent.md) — agent definition
- [`.github/instructions/lld-agent.instructions.md`](./../instructions/lld-agent.instructions.md) — detailed LLD generation rules
- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Keeping data safe](https://digital.defra.gov.uk/ai-toolkit/guidance/keeping-data-safe)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)

---

*This document is maintained alongside `.github/agents/documentation-lld.agent.md`. Update the change log and "Last reviewed" date whenever the agent is modified.*
