---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: schema-analysis
description: 'Introspect and analyse a SQL Server 2022 source database for migration to Azure SQL Database or Amazon RDS SQL Server. Extracts all objects, identifies unsupported features on the target platform, assesses compatibility risks, and produces a migration readiness report.'
---

# Schema Analysis

Introspect a SQL Server 2022 on-premises source database to produce a comprehensive inventory of all database objects, their cloud-compatibility risks, and a migration readiness assessment for the chosen target (Azure SQL Database or Amazon RDS SQL Server).

## When to Use

- First phase of every migration workflow
- User asks to "assess", "analyse", "inventory", "introspect", or "scan" the database
- Before deciding on a migration strategy or target tier

## Steps

### 1. Confirm Target Platform

Before running introspection, confirm with the agent context or user:
- **Azure SQL Database** — managed PaaS; many server-level features are unavailable
- **Amazon RDS SQL Server** — managed IaaS-like; most SQL Server features available but some restrictions apply

The target affects how risks are classified.

### 2. Run Introspection Script

**Execute** [scripts/introspect-sqlserver.sql](scripts/introspect-sqlserver.sql) directly using the terminal tool. Do NOT print the command and ask the user to run it — use the terminal tool to run it:

```bash
# Windows Authentication (domain account or LocalDB)
sqlcmd -S <server> -d <database> -E -i .github/skills/db-migration/schema-analysis/scripts/introspect-sqlserver.sql -o analysis-raw.txt -W

# SQL Server Authentication
sqlcmd -S <server> -d <database> -U <username> -P <password> -i .github/skills/db-migration/schema-analysis/scripts/introspect-sqlserver.sql -o analysis-raw.txt -W
```

Common server name formats:
- Named instance: `HOSTNAME\INSTANCENAME`
- LocalDB: `(localdb)\MSSQLLocalDB`
- Azure SQL: `<server>.database.windows.net`
- Default instance: `HOSTNAME` or `HOSTNAME,1433`

After execution, read `analysis-raw.txt` using the read tool and proceed to Step 3.

### 3. Classify Database Objects

Parse the introspection output and classify every object by:

- **Type**: table, view, stored procedure, function, trigger, sequence, index, constraint, synonym, linked server, CLR object, SQL Agent job, Service Broker queue/service, FILESTREAM column
- **Complexity**:
  - **Simple** — No dependencies, basic data types, no procedural logic
  - **Medium** — FK references, simple procedural logic, standard data types
  - **Complex** — Cross-database references, dynamic SQL, cursors, proprietary features, partitioned tables
- **Row count** per table
- **FK dependency graph**

### 4. Identify Cloud Compatibility Risks

Flag every object or feature that is unsupported or restricted on the target platform:

#### Azure SQL Database — Unsupported / Restricted Features

| Feature | Severity | Detail |
| --- | --- | --- |
| Windows Authentication / Kerberos | **Blocker** | Must migrate to Azure AD auth or SQL auth |
| Linked servers | **Blocker** | Not available; replace with Elastic Query or ADF |
| Cross-database queries (`USE OtherDb`) | **Blocker** | Single-database scope only |
| CLR integration (custom assemblies) | **Blocker** | Blocked in Azure SQL Database |
| FILESTREAM / FILETABLE | **Blocker** | Not supported; migrate files to Azure Blob Storage |
| SQL Server Agent jobs | **High** | Not available; use Elastic Jobs or Azure Automation |
| Service Broker (cross-db/server) | **High** | Replace with Azure Service Bus |
| Database Mail | **High** | Not available; use Azure Communication Services |
| Resource Governor | **High** | Not available on Azure SQL Database |
| Server-level DDL triggers | **High** | Not supported at server scope |
| BULK INSERT with local file paths | **High** | Must use Azure Blob Storage URL |
| Extended stored procedures (xp_) | **Medium** | Most xp_ procedures unavailable |
| PolyBase external tables | **Medium** | Not available; use Elastic Query or ADF |
| Always On AG / mirroring config | **Info** | Platform-managed HA; AG objects must be removed |
| TDE configuration | **Info** | Platform-managed by default |

#### Amazon RDS SQL Server — Unsupported / Restricted Features

| Feature | Severity | Detail |
| --- | --- | --- |
| FILESTREAM / FILETABLE | **Blocker** | Not supported on RDS SQL Server |
| Windows Authentication | **High** | Requires AWS Managed Microsoft AD |
| BULK INSERT with local paths | **High** | Must use S3 paths |
| Backup/Restore to local disk | **High** | Must use S3 with RDS procedures |
| CLR integration | **Medium** | Supported but requires option group enablement |
| SQL Server Agent | **Medium** | Available but some system jobs restricted |
| Linked servers to on-premises | **Medium** | Limited; requires testing |
| Service Broker (cross-server) | **Medium** | Not available cross-server |
| Database Mail | **Medium** | Available via RDS option group |
| xp_cmdshell | **Blocker** | Disabled and cannot be enabled |
| Custom startup stored procedures | **Medium** | Not supported on RDS |
| Dedicated Admin Connection (DAC) | **Low** | Not available on RDS |

### 5. Identify Additional Migration Risks

---

## Standards

This skill is loaded by the [db-migration agent](./../../../agents/db-migration.agent.md). All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/).

Also flag:
- Temporal tables (system-versioned) — verify syntax compatibility on target
- Computed / persisted columns — verify expressions
- Always Encrypted columns — key management migration required
- Spatial data types — verify function support on target
- LOB columns (VARBINARY(MAX), VARCHAR(MAX), NVARCHAR(MAX)) — check max row size
- Partitioned tables — verify partition scheme migration
- Full-text indexes — check service availability on target
- Dynamic SQL with hard-coded server/database names

### 6. Generate Output

Use the template at [templates/analysis-report.md](templates/analysis-report.md) to produce the analysis report.

### 7. Summary Metrics

Include at the top of the report:

- Total object count by type
- Compatibility blockers count per target platform
- Estimated migration complexity: **Low** / **Medium** / **High** / **Very High**
- Recommended migration strategy: lift-and-shift to RDS vs refactor for Azure SQL Database
- Key risks and blockers grouped by target platform

## Complexity Rating Guide

| Total Objects | Complex Objects | Blocker Features | Rating    |
| ------------- | --------------- | ---------------- | --------- |
| < 50          | < 5             | 0                | Low       |
| 50–200        | 5–20            | 1–3              | Medium    |
| 200–500       | 20–50           | 3–10             | High      |
| > 500         | > 50            | > 10             | Very High |
