---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: compatibility-fixing
description: >
  Identify and fix compatibility issues when migrating SQL Server 2022 on-premises
  databases to Azure SQL Database or Amazon RDS SQL Server. Scans for unsupported
  features, generates fix scripts, and produces a compatibility report.
---

# Compatibility Fixing

## When to Use

- After schema-analysis has produced a compatibility risk report
- Before performance-tuning or migration-validation
- When migrated database objects fail on the target platform
- When confirming readiness of a database for cloud deployment

## Process

### Step 1 — Confirm Target Platform

Ensure `TARGET_PLATFORM` is set to either **Azure SQL Database** or **Amazon RDS SQL Server**.  
The quirks reference and fix severity vary by target.

### Step 2 — Load Quirks Reference

Load [references/sqlserver-quirks.md](references/sqlserver-quirks.md) — this documents all known incompatibilities between SQL Server 2022 on-premises features and the two cloud targets.

### Step 3 — Scan Source Schema Against Quirks

For every entry in the quirks file:

1. Search the source schema and procedure bodies for the **Pattern** documented in the entry
2. If found and the entry applies to `TARGET_PLATFORM`, apply or document the **Fix**
3. Log: `[COMPAT] Found: {feature_name} in {object}:{detail} → Applied: {fix} (Severity: {level})`

Rate each finding:

- **Blocker** — must be resolved before migration can proceed
- **High** — will cause runtime failures if not addressed
- **Medium** — may cause unexpected behaviour; should be addressed
- **Low** — minor difference; document and monitor
- **Info** — platform change with no action required; document only

### Step 4 — Universal Compatibility Checks

Regardless of target, scan for:

| # | Check | What to Look For |
| --- | --- | --- |
| 1 | Hard-coded server names | Connection strings or OPENQUERY references inside procedure bodies |
| 2 | Hard-coded database names | `USE [DatabaseName]` or three-part identifiers `db.schema.table` |
| 3 | Windows Auth dependencies | `EXECUTE AS LOGIN`, logins mapped to Windows accounts |
| 4 | Local file system paths | BULK INSERT paths, BACKUP paths, BCP paths |
| 5 | SQL Agent job references | `msdb.dbo.sp_add_job`, `sp_add_jobstep`, `sp_add_schedule` |
| 6 | Service Broker objects | Queues, services, contracts, routes, endpoints |
| 7 | Linked server calls | `OPENQUERY`, four-part identifiers |
| 8 | Cross-database queries | `OtherDatabase.schema.table` references |
| 9 | CLR assemblies | `CREATE ASSEMBLY`, `EXTERNAL NAME` references |
| 10 | FILESTREAM columns | `FILESTREAM` attribute, FILETABLE DDL |
| 11 | Server-level objects | Server triggers, Windows group logins |
| 12 | Database Mail calls | `msdb.dbo.sp_send_dbmail` |
| 13 | Resource Governor | `CREATE RESOURCE POOL`, `CREATE WORKLOAD GROUP` |
| 14 | Extended stored procedures | `xp_cmdshell`, `xp_fileexist`, other `xp_` calls |
| 15 | Compatibility level | `COMPATIBILITY_LEVEL` below 150 |

### Step 5 — Generate Compatibility Report

Produce a report with four sections:

```markdown
## Compatibility Report — SQL Server 2022 → [TARGET_PLATFORM]

### Blockers (must fix before migration)

| # | Object | Feature | Issue | Fix |
| --- | --- | --- | --- | --- |

### High Severity (fix before go-live)

| # | Object | Feature | Fix Applied | Review Required |
| --- | --- | --- | --- | --- |

### Medium / Low Severity

| # | Object | Feature | Recommendation |
| --- | --- | --- | --- |

### Informational

---

## Standards

This skill is loaded by the [db-migration agent](./../../../agents/db-migration.agent.md). All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/).

| # | Feature | Platform Behaviour | Action |
| --- | --- | --- | --- |
```

### Step 6 — Apply Fixes

Generate `.sql` fix scripts for all auto-fixable issues. Add `-- COMPAT-FIX: {reason}` comments for traceability.

For items requiring manual refactoring (linked servers, CLR assemblies, FILESTREAM), produce a remediation plan listing affected objects and the recommended replacement architecture.

## How the Quirks File Grows

After each engagement, add newly discovered incompatibilities to `references/sqlserver-quirks.md`:

```markdown
## [N]. [Feature / Pattern Name]

- **Applies To**: Azure SQL Database | Amazon RDS SQL Server | Both
- **Pattern**: What to scan for
- **Problem**: Why it fails or behaves differently on the target
- **Fix**: The correct remediation or workaround
- **Severity**: Blocker / High / Medium / Low / Info
- **Added**: [date], [engagement reference]
```

## Reference Files

| File | Purpose |
| --- | --- |
| [references/sqlserver-quirks.md](references/sqlserver-quirks.md) | Known SQL Server 2022 → Azure SQL / RDS incompatibilities |
