# GDS UI Compliance Agent — What It Does & Change Log

**File:** `.github/agents/gds-ui.agent.md`  
**Last reviewed:** 2026-07-31  
**Owner:** LAP Civica Migration Team  
**Standards alignment:** Defra SDS · Defra AI Toolkit — Deliver with AI

---

## What This Agent Does

### Overview

The **GDS UI Compliance Agent** is a GOV.UK Design System specialist for the four Civica applications: **BSE, Histo, D2R2, and PTLIMS**. It reviews and fixes pages, forms, and components to meet the GOV.UK Design System, accessibility (WCAG 2.2 AA), content standards, and GOV.UK Service Standard.

It targets **govuk-frontend v6.x**.

---

### Skills

| Skill | File | When it runs |
|---|---|---|
| `fix-gds-layout` | `.github/skills/fix-gds-layout.skill.md` | **Always** — Step 1 of every GDS compliance pass; also handles all explicit layout, asset, header, or footer issues |

---

### Hard Requirements (Blocks Work That Fails These)

| # | Requirement | Detail |
|---|---|---|
| 1 | Component source | Use `govuk-frontend` components and documented markup only — no hand-rolled CSS clones |
| 2 | Accessibility | WCAG 2.2 AA — interactive elements keyboard-reachable, visible focus, correct ARIA per component docs |
| 3 | Semantic HTML | One `<h1>` per page, no skipped heading levels, native elements over `div`/`span` with handlers |
| 4 | One thing per page | Forms follow the question-pages pattern; every input has a `<label>` |
| 5 | Error handling | `govuk-error-summary` at top (focused on load), inline error messages, error text in `<title>` |
| 6 | Page template | GOV.UK template: skip link, crown header, phase banner, footer with required links, `lang="en"` |
| 7 | Content style | Plain English, sentence case, no Latin abbreviations, "Continue" not "Submit" |
| 8 | No colour-only meaning | Use official GDS palette tokens |

---

### Approval Checkpoint (Required)

The `fix-gds-layout` skill includes a mandatory **Step 2a approval checkpoint** before any files are written:

1. Agent presents audit findings (asset status + layout issues)
2. Agent lists every planned write action
3. User must explicitly confirm before any file is modified or fetched

No files are modified without explicit user approval.

---

### Component Quick Reference

| Need | Correct component | Do not use |
|---|---|---|
| Primary action | `govuk-button` | Styled `<a>` or custom button |
| Text input | `govuk-input` + label | Bare `<input>` |
| Error messages | `govuk-error-message` + `govuk-error-summary` | Custom styling |
| Navigation | `govuk-service-navigation` | Custom nav |

---

### Outputs Produced

| Output | Description |
|---|---|
| Updated layout file | `Pages/Shared/_Layout.cshtml` with correct govuk-frontend v6 structure |
| Fetched asset files | CSS, JS, fonts, SVG assets from unpkg CDN where missing or stubbed |
| Audit report | Summary of all issues found and fixes applied |

---

### Guardrails Summary

**The agent will:**
- Run `fix-gds-layout` as Step 1 of every GDS compliance pass
- Present a full audit and planned actions before writing any file
- Use govuk-frontend v6.x components and markup only

**The agent will never:**
- Write any files without explicit user approval (Step 2a checkpoint)
- Invent bespoke UI when an official GOV.UK component exists
- Add `govuk-frontend` assets without first checking if they are present and valid

---

### Compliance Profile

| Dimension | Classification / Status |
|---|---|
| AI Toolkit risk level | MEDIUM |
| Human oversight required | Yes — mandatory approval checkpoint before all writes |
| Accessibility | WCAG 2.2 AA required |
| Defra SDS standards applied | GitHub Copilot guide, Common coding standards, Security standards, Git branching |

---

## Change Log

### v1.0 — 2026-07-31 — Initial Guide

Initial creation of the GDS UI Compliance agent guide.

---

## References

- [`.github/agents/gds-ui.agent.md`](./../agents/gds-ui.agent.md) — agent definition
- [GOV.UK Design System](https://design-system.service.gov.uk/)
- [WCAG 2.2](https://www.w3.org/TR/WCAG22/)
- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — Common coding standards](https://defra.github.io/software-development-standards/standards/common_coding_standards/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)

---

*This document is maintained alongside `.github/agents/gds-ui.agent.md`. Update the change log and "Last reviewed" date whenever the agent is modified.*
