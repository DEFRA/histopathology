# DevOps Pipeline Modernizer Agent — What It Does & Change Log

**File:** `.github/agents/devops-pipeline-modernizer.agent.md`  
**Last reviewed:** 2026-07-31  
**Owner:** LAP Civica Migration Team  
**Standards alignment:** Defra SDS · Defra AI Toolkit — Deliver with AI

---

## What This Agent Does

### Overview

The **DevOps Pipeline Modernizer** generates parameterized, template-driven CI/CD pipelines for a single .NET 10 repository. It reads the solution file, discovers web and database projects, and produces environment-driven deployment templates for `dev`, `test`, `pre-prod`, and `prod`. It also recommends a branching strategy and standardises environment configuration.

> **Scope:** One execution per repository. Does not design a shared pipeline across multiple repositories.

---

### Workflow Phases

| Phase | Name | What happens |
|---|---|---|
| A | Discovery & Assessment | Reads existing pipelines and solution file; identifies projects, build steps, scan tools (read-only) |
| B | Modernisation Design | Proposes reusable template structure, branching strategy, environment config, secret store references |
| C | Implementation | Generates pipeline YAML templates, deployment config, and LLD-to-Bicep IaC (on confirmation) |
| D | Validation | Verifies generated pipelines parse correctly; summarises all changes |

---

### Inputs Required

| Input | Description |
|---|---|
| Pipeline system | Azure DevOps or GitHub Actions |
| Solution file location | Path to `.sln` |
| Existing pipeline paths | `/azure-pipelines.yml`, `/.github/workflows/`, etc. |
| Deployment strategy | Slots or direct deploy, approval gates, release windows |
| Security requirements | SAST/SCA/secret scanning tools required |

Fixed environments (not configurable): `dev` → `test` → `pre-prod` → `prod`.

---

### Skills

| Skill | Purpose |
|---|---|
| `devops-pipeline-modernizer` skill files | Phase-by-phase playbooks under `.github/skills/devops-pipeline-modernizer/` |

---

### Outputs Produced

| Output | Description |
|---|---|
| Pipeline YAML templates | Reusable, parameterized pipeline templates per environment |
| Branching strategy documentation | Recommended branch model following Defra SDS Git Branching Strategy |
| Deployment config file | Repo-level config referencing environment variables and secret store keys |
| IaC templates (Bicep/YAML) | Generated from LLD if LLD document is provided |
| Modernisation report | Summary of all changes, assumptions, and recommended next steps |

---

### Guardrails Summary

**The agent will:**
- Keep secrets out of the repository — use secret stores and environment-level references
- Produce plans, diffs, and templates — prefer incremental over large one-shot changes
- Make minimal assumptions; ask targeted questions only when needed
- Work on exactly one repository per invocation

**The agent will never:**
- Hardcode credentials, API keys, or connection strings in any generated file
- Design pipelines that cross repository boundaries
- Apply changes without presenting the plan for review first

---

### Compliance Profile

| Dimension | Classification / Status |
|---|---|
| AI Toolkit risk level | MEDIUM |
| Human oversight required | Yes — review before applying pipeline changes |
| Defra SDS standards applied | GitHub Copilot guide, Security standards, Git branching |

---

## Change Log

### v1.0 — 2026-07-31 — Initial Guide

Initial creation of the DevOps Pipeline Modernizer agent guide.

---

## References

- [`.github/agents/devops-pipeline-modernizer.agent.md`](./../agents/devops-pipeline-modernizer.agent.md) — agent definition
- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)

---

*This document is maintained alongside `.github/agents/devops-pipeline-modernizer.agent.md`. Update the change log and "Last reviewed" date whenever the agent is modified.*
