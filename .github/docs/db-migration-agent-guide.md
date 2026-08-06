# Database Migration Agent — What It Does & Change Log

**File:** `.github/agents/db-migration.agent.md`  
**Last reviewed:** 2026-07-31  
**Owner:** LAP Civica Migration Team  
**Standards alignment:** Defra SDS · Defra AI Toolkit — Deliver with AI

---

## What This Agent Does

### Overview

The **Database Migration Agent — SQL Server** is a specialist in migrating on-premises SQL Server 2022 databases to **Azure SQL Database** or **Amazon RDS SQL Server**. It covers the full migration lifecycle: schema analysis, compatibility fixing, performance tuning, validation, and reporting.

> **Scope:** SQL Server 2022 (on-premises) to Azure SQL Database or Amazon RDS SQL Server only. Other database engines are out of scope — use the generic Database Migration Agent for those paths.

---

### Modes

| Mode | Triggers | What happens |
|---|---|---|
| **Auto mode** | "migrate", "move", "run full migration", "full workflow" | Runs all 5 phases sequentially; pauses after Phase 1 for user confirmation before proceeding |
| **Manual mode** | Single action word (see below) | Runs one skill in isolation |

#### Manual Mode Skill Routing

| User intent | Skill invoked |
|---|---|
| analyse, assess, inventory, scan | `schema-analysis` |
| fix, compatibility, unsupported | `compatibility-fixing` |
| tune, optimise, performance, indexes | `performance-tuning` |
| validate, verify, compare, check counts | `migration-validation` |
| report, playbook, summary | `migration-reporting` |

---

### Skills

| Order | Skill | Purpose |
|---|---|---|
| 1 | `schema-analysis` | Introspect tables, columns, constraints, indexes, views, procedures, row counts |
| 2 | `compatibility-fixing` | Catch and fix dialect quirks, reserved words, deprecated features |
| 3 | `performance-tuning` | Review indexes, query plans, partitioning, DB-specific configuration |
| 4 | `migration-validation` | Verify row counts, schema structure, constraints, functional equivalence |
| 5 | `migration-reporting` | Generate comprehensive migration playbook and final report |

Skills are located under `.github/skills/db-migration/`.

---

### Critical Gate

After Phase 1 (schema analysis) the agent **always pauses** and presents the assessment before proceeding. The user must explicitly confirm before Phase 2 begins.

---

### Inputs Required

| Input | Description |
|---|---|
| Source database | SQL Server 2022 connection details or DDL scripts |
| Target platform | `Azure SQL Database` or `Amazon RDS SQL Server` |
| Intended mode | Auto or single-phase |

---

### Outputs Produced

| Output | Description |
|---|---|
| Schema analysis report | Compatibility assessment with risk matrix |
| Compatibility fix scripts | T-SQL fixes for dialect differences and deprecated features |
| Performance tuning recommendations | Index, query plan, and configuration recommendations |
| Validation report | Row count comparison, schema diff, constraint verification |
| Migration playbook | Full migration runbook and final report |

---

### Guardrails Summary

**The agent will:**
- Pause after schema analysis for user confirmation before any changes are made
- Mark uncertain mappings with confidence ratings (High / Medium / Low)
- Include a "Manual Migration Needed" section when automated conversion is not possible
- Never include credentials or secrets in output artefacts

**The agent will never:**
- Handle database engines other than SQL Server
- Proceed past Phase 1 without explicit user confirmation
- Silently skip unresolvable issues — all blockers are surfaced in the report

---

### Compliance Profile

| Dimension | Classification / Status |
|---|---|
| AI Toolkit risk level | MEDIUM |
| Human oversight required | Yes — gate confirmation after Phase 1 |
| Data protection | Do not run against live production data |
| Defra SDS standards applied | GitHub Copilot guide, Security standards, Git branching |

---

## Change Log

### v1.0 — 2026-07-31 — Initial Guide

Initial creation of the Database Migration Agent guide covering SQL Server 2022 migration to Azure SQL Database / Amazon RDS SQL Server.

---

## References

- [`.github/agents/db-migration.agent.md`](./../agents/db-migration.agent.md) — agent definition
- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)

---

*This document is maintained alongside `.github/agents/db-migration.agent.md`. Update the change log and "Last reviewed" date whenever the agent is modified.*
