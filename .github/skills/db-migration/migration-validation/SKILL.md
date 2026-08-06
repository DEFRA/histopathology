---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: migration-validation
description: >
  Validate that the migrated database matches the source in structure and data.
  Use after migration to verify row counts, data integrity, constraint
  compliance, and functional equivalence of stored procedures.
---

# Migration Validation

## When to Use

- After data migration is complete (schema deployed + data loaded)
- Requires connection details for BOTH source and target databases
- Before declaring migration successful / cutting over

## Prerequisites

- Source database: read-only access
- Target database: read access (read-write if running functional tests)
- Both databases accessible from the same network / machine
- Row counts on source should be frozen (no active writes during validation)

## Process

### Step 1 — Row Count Comparison

Run `scripts/compare-row-counts.sql` against both source and target.

For each table/collection:

| Result                   | Meaning                                                             | Action                  |
| ------------------------ | ------------------------------------------------------------------- | ----------------------- |
| Exact match              | Row counts are identical                                            | PASS                    |
| Within tolerance (±0.1%) | Minor variance — acceptable for large tables with concurrent writes | PASS with note          |
| Mismatch > tolerance     | Data loss or duplication                                            | FAIL — investigate      |
| Table missing on target  | Migration incomplete                                                | FAIL — rerun migration  |
| Extra table on target    | Unexpected object                                                   | WARNING — verify intent |

Generate a side-by-side comparison table:

```markdown
| Table     | Source Count | Target Count | Match   | Notes                  |
| --------- | ------------ | ------------ | ------- | ---------------------- |
| users     | 50,000       | 50,000       | ✅ PASS |                        |
| orders    | 1,200,000    | 1,199,998    | ⚠️ WARN | Within 0.01% tolerance |
| audit_log | 5,000,000    | 4,800,000    | ❌ FAIL | 200K rows missing      |
```

### Step 2 — Schema Structure Validation

Compare the schema structure between source and target:

| Check              | What to Compare                                             |
| ------------------ | ----------------------------------------------------------- |
| Tables exist       | Every source table has a target equivalent                  |
| Column names       | All columns present (accounting for renames)                |
| Column types       | SQL Server source types are equivalent on Azure SQL Database and RDS SQL Server |
| Column nullability | NULL/NOT NULL constraints match                             |
| Column defaults    | Default values are functionally equivalent                  |
| Column order       | Not critical but document if different                      |

### Step 3 — Constraint Validation

| Constraint Type    | Validation Method                                          |
| ------------------ | ---------------------------------------------------------- |
| Primary keys       | Verify PK exists on same column(s)                         |
| Unique constraints | Verify unique constraint/index exists                      |
| Foreign keys       | Verify FK exists with correct references and cascade rules |
| Check constraints  | Verify CHECK constraint with equivalent expression         |
| Not null           | Verify nullability matches                                 |

### Step 4 — Index Validation

| Check              | What to Compare                                            |
| ------------------ | ---------------------------------------------------------- |
| Index exists       | Every source index has a target equivalent                 |
| Index columns      | Same columns in same order                                 |
| Index type         | B-tree, hash, GIN, etc. (target equivalents)               |
| Uniqueness         | Unique flag matches                                        |
| Partial/filtered   | Filter condition is equivalent                             |
| Additional indexes | Target may have extra indexes from performance-tuning (OK) |

### Step 5 — Sequence / Identity Validation

| Check           | What to Verify                                              |
| --------------- | ----------------------------------------------------------- |
| Sequences exist | All source sequences have target equivalents                |
| Current value   | Target sequence value ≥ max(PK column) — prevents conflicts |
| Increment       | Increment values match                                      |
| Cache size      | Cache values are appropriate for target platform            |

### Step 6 — Functional Validation (Stored Procedures)

If both databases are accessible and contain stored procedures/functions:

1. Identify critical procedures from schema analysis
2. Prepare test inputs (representative data)
3. Execute on source, capture output
4. Execute equivalent on target, capture output
5. Compare outputs — must be functionally equivalent

| Procedure                   | Test Input | Source Output      | Target Output      | Match   |
| --------------------------- | ---------- | ------------------ | ------------------ | ------- |
| `calculate_total(order_id)` | `12345`    | `1,234.56`         | `1,234.56`         | ✅ PASS |

---

## Standards

This skill is loaded by the [db-migration agent](./../../../agents/db-migration.agent.md). All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/).
| `get_user_roles(user_id)`   | `42`       | `['admin','user']` | `['admin','user']` | ✅ PASS |

### Step 7 — Generate Validation Report

Use `templates/validation-checklist.md` to produce the final report.

For each check, record:

| Status  | Symbol | Meaning                                  |
| ------- | ------ | ---------------------------------------- |
| PASS    | ✅     | Validation passed                        |
| FAIL    | ❌     | Validation failed — remediation required |
| SKIP    | ⏭️     | Skipped — with documented reason         |
| WARNING | ⚠️     | Passed with caveats                      |

### Step 8 — Remediation

For any FAIL results, provide specific remediation steps:

1. Identify the root cause (missing data, wrong type, missing constraint)
2. Provide the exact SQL/command to fix
3. Re-run the specific validation check after fix
4. Update the report

## Supporting Files

| File                                                                   | Purpose                                              |
| ---------------------------------------------------------------------- | ---------------------------------------------------- |
| [scripts/compare-row-counts.sql](scripts/compare-row-counts.sql)       | Row count queries for SQL Server, Azure SQL Database, and RDS SQL Server |
| [templates/validation-checklist.md](templates/validation-checklist.md) | Output template for validation results               |
