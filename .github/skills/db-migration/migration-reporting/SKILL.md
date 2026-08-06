---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: migration-reporting
description: >
  Generate a comprehensive migration playbook and final report. Use as the last
  phase to produce the deliverable document covering the entire migration:
  assessment findings, compatibility fixes applied, performance recommendations,
  validation results, and a step-by-step runbook.
---

# Migration Reporting

Generate the final migration playbook that aggregates outputs from all previous
phases into a single deliverable document suitable for technical leadership and
client stakeholders.

## When to Use

- After completing all migration phases (analysis → conversion → compatibility → tuning → validation)
- When the client or delivery lead requests a migration report or playbook
- When preparing handover documentation for the migration
- Trigger phrases: "generate migration report", "create migration playbook", "produce final report", "write up the migration"

## Instructions

### Step 1 — Gather Phase Outputs

Collect the outputs from each preceding phase. Every section below maps to a
phase:

| Report Section              | Source Phase         | What to Include                                              |
| --------------------------- | -------------------- | ------------------------------------------------------------ |
| Assessment Findings         | schema-analysis      | Object inventory, compatibility risks, cloud-readiness score |
| Compatibility Fixes Applied | compatibility-fixing | All unsupported features found and fixes applied             |
| Performance Recommendations | performance-tuning   | Tuning items with impact and effort ratings                  |
| Validation Results          | migration-validation | Pass/fail summary, outstanding issues                        |

If a phase was skipped, note "Not applicable — skipped" in that section.

### Step 2 — Populate the Report Template

Use the template at `templates/migration-report.md`. Fill every section with
real data from the phase outputs. Do not leave placeholder text in the final
report.

### Step 3 — Write the Executive Summary

Summarise the migration in 3-5 paragraphs:

- Source and target platforms, versions
- Total objects migrated (tables, views, procedures, functions, triggers, indexes)
- Overall complexity rating: Low / Medium / High / Very High
- Key risks and how they were mitigated
- Validation outcome (all passed, or list outstanding items)

### Step 4 — Write the Migration Runbook

The runbook is the most operationally critical section. It must be executable
by an engineer who was not involved in the migration design. Include:

1. **Pre-migration checklist** — backups verified, maintenance window scheduled, stakeholders notified, rollback plan reviewed
2. **Backup commands** — exact commands for source database backup (platform-specific)
3. **Schema deployment order** — respecting FK dependencies (sequences → tables → constraints → indexes → views → functions → procedures → triggers)
5. **Data migration commands** — tool-specific (SqlPackage.exe for BACPAC, Azure Database Migration Service, AWS DMS, native SQL Server backup/restore to Azure Blob Storage or Amazon S3)
5. **Post-migration validation** — run the migration-validation skill checks
6. **Rollback procedure** — step-by-step instructions to revert if critical issues found
7. **Go-live cutover** — DNS/connection string switch, application config update, cache invalidation

### Step 5 — Complete Supporting Sections

- **Estimated Downtime** — based on data volume, migration method (online vs offline), and network throughput. Provide best-case, expected, and worst-case estimates.
- **Risk Register** — table with columns: Risk ID, Description, Likelihood (Low/Medium/High), Impact (Low/Medium/High), Mitigation, Owner, Status
- **Sign-off Checklist** — items requiring human approval before go-live:
  - [ ] Schema validation passed
  - [ ] Row count validation passed (within tolerance)
  - [ ] Application smoke tests passed on target
  - [ ] Performance benchmarks met
  - [ ] Rollback procedure tested
  - [ ] Stakeholder sign-off received

### Step 6 — Format and Deliver

- Include a mermaid diagram showing the migration flow (phases, decision points, rollback path)
- Use tables for structured data (object inventory, validation results, risk register)
- Format as professional Markdown suitable for rendering in GitHub, Azure DevOps, or Confluence
- The document should be self-contained — a reader should not need to reference other files

## Output

Generate the report using `templates/migration-report.md` as the structure.
The completed report should be saved to the project's output directory or
delivered inline as the agent's response.

## Template

- `templates/migration-report.md` — Full report template with all sections

---

## Standards

This skill is loaded by the [db-migration agent](./../../../agents/db-migration.agent.md). All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/).
