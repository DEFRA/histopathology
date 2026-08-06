---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: performance-tuning
description: >
  Analyse and optimise performance after migrating from SQL Server 2022 on-premises
  to Azure SQL Database or Amazon RDS SQL Server. Reviews index strategy, query plans,
  data types, partitioning, and cloud-specific service tier configuration.
---

# Performance Tuning

## When to Use

- After compatibility-fixing — optimise before validating
- When query performance on the cloud target is worse than on-premises
- When reviewing index strategy post-migration
- When selecting the correct Azure SQL / RDS service tier

## Process

### Step 1 — Identify Critical Queries

Gather the most performance-sensitive queries from:
- Application code (ORM queries, raw SQL)
- Stored procedures and functions
- Reporting queries
- Batch processing jobs

Prioritise by: frequency, latency sensitivity, and data volume.

### Step 2 — Run Query Plan Analysis

Run `scripts/explain-analyse.sql` against each critical query on the target platform.

Metrics to investigate:

| Metric | Concern Threshold |
| --- | --- |
| Table scan on large table (>100K rows) | Should have index |
| Nested loop join on large tables | Consider hash/merge join or missing index |
| Sort with large spill to tempdb | Add index or adjust sort memory |
| High logical reads vs low rows returned | Index selectivity issue |
| Key lookups / RID lookups | Consider covering index with INCLUDE |

### Step 3 — Review Index Strategy

| # | Check | Common Issue |
| --- | --- | --- |
| 1 | FK columns indexed | SQL Server creates PK indexes but not FK indexes automatically |
| 2 | Redundant indexes | Indexes that are subsets of wider indexes |
| 3 | Index column order | Leftmost prefix rule for composite indexes |
| 4 | Covering indexes | Use INCLUDE columns to avoid key lookups |
| 5 | Filtered indexes | For skewed data distributions (e.g., Status = 'Active') |
| 6 | Columnstore indexes | For analytical/reporting workloads |
| 7 | Unused indexes from source | Remove indexes with no read benefit |

### Step 4 — Review Data Types

| Check | Issue | Fix |
| --- | --- | --- |
| NVARCHAR where VARCHAR suffices | Double storage for ASCII data | Use VARCHAR if data is not Unicode |
| VARCHAR(MAX) for short values | Optimiser issues, no length validation | Right-size to actual max length |
| FLOAT for financial data | Precision loss | Use DECIMAL(p, s) |
| DATETIME for date-only values | Unnecessary time component | Use DATE |
| UNIQUEIDENTIFIER as clustered PK | Random GUIDs cause fragmentation | Use NEWSEQUENTIALID() or BIGINT identity |

### Step 5 — Partitioning Opportunities

Consider partitioning when:
- Table exceeds 10 million rows
- Queries consistently filter by date range
- Data has an archival pattern (older data rarely accessed)
- Bulk deletes of old data are frequent

Both Azure SQL Database and Amazon RDS SQL Server support table partitioning (RANGE). Verify partition function and scheme syntax migrates correctly.

### Step 6 — Cloud-Specific Configuration

#### Azure SQL Database

| Consideration | Guidance |
| --- | --- |
| Service tier selection | vCore model recommended for predictable workloads; DTU for simple/small |
| Compute tier | Provisioned for consistent load; Serverless for intermittent workloads |
| Elastic Pool | Use when multiple databases have complementary usage patterns |
| Query Store | Enabled by default; use for plan regression detection and forced plans |
| Automatic Tuning | Enable `AUTO_CREATE_INDEX` and `FORCE_LAST_GOOD_PLAN` for managed index tuning |
| MAXDOP | Managed by service tier; verify MAXDOP hints in procedures are appropriate |
| tempdb | Managed; monitor contention for workloads with heavy temp table use |
| Connection pooling | Use application-level pooling; Azure SQL has a 30,000 connection limit per pool |
| Read scale-out | Available on Business Critical tier; route reporting queries to secondary replica |

#### Amazon RDS SQL Server

| Consideration | Guidance |
| --- | --- |
| Instance class | `db.m6i` for general workloads; `db.r6i` for memory-intensive workloads |
| Multi-AZ | Enable for production HA; read replicas available for read scaling |
| Storage type | `io1`/`io2` for IOPS-sensitive workloads; `gp3` for general purpose |
| Parameter group | Tune max server memory (leave 10% for OS), cost threshold for parallelism, MAXDOP |
| Query Store | Available; enable for plan stability and regression analysis |
| Enhanced Monitoring | Enable for OS-level metrics at sub-minute intervals |
| Performance Insights | Enable for wait-type analysis and top SQL identification |
| Maintenance window | Schedule during low-traffic periods; RDS applies patches in the maintenance window |

### Step 7 — Generate Recommendations

Use `templates/tuning-recommendations.md` to produce the report.

Rate each recommendation:

| Impact | Effort | Priority |
| --- | --- | --- |
| High | Quick Fix | P1 — do immediately |
| High | Moderate | P2 — do before go-live |
| Medium | Quick Fix | P2 — do before go-live |
| High | Major Change | P3 — plan for post-migration |
| Medium | Moderate | P3 — plan for post-migration |

---

## Standards

This skill is loaded by the [db-migration agent](./../../../agents/db-migration.agent.md). All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/).
| Low | Any | P4 — backlog |

## Supporting Files

| File | Purpose |
| --- | --- |
| [scripts/explain-analyse.sql](scripts/explain-analyse.sql) | Query plan analysis for SQL Server, Azure SQL, and RDS |
| [templates/tuning-recommendations.md](templates/tuning-recommendations.md) | Output template for performance recommendations |



| Metric                                        | Concern Threshold                  |
| --------------------------------------------- | ---------------------------------- |
| Sequential scan on large table (>100K rows)   | Should have index                  |
| Nested loop join on large tables              | Consider hash join / missing index |
| Sort operation on unsorted data               | Add index or increase `work_mem`   |
| High buffer reads vs low rows returned        | Index selectivity issue            |
| Bitmap heap scan with many recheck conditions | Partial index opportunity          |

### Step 3 — Review Index Strategy

Check the converted schema for:

| #   | Check                      | Common Issue                                                                        |
| --- | -------------------------- | ----------------------------------------------------------------------------------- |
| 1   | FK columns indexed         | Most migrations miss indexes on FK columns — PG doesn't auto-create them            |
| 2   | Redundant indexes          | Two indexes covering similar columns; one is a subset of the other                  |
| 3   | Index column order         | Leftmost prefix rule — reorder compound indexes to match query patterns             |
| 4   | Covering indexes           | High-frequency queries can use INCLUDE columns to avoid table lookups               |
| 5   | Partial indexes            | For columns with skewed data (e.g., `WHERE status = 'active'` on 90% of rows)       |
| 6   | Expression indexes         | For queries filtering on `LOWER(email)`, `DATE_TRUNC('day', ts)`, etc.              |
| 7   | Unused indexes from source | Source DB may have indexes that are no longer needed on target due to query changes |
| 8   | Over-indexing              | Every index adds write overhead; remove indexes with no read benefit                |

### Step 4 — Review Data Types

| Check                                         | Issue                                      | Fix                                          |
| --------------------------------------------- | ------------------------------------------ | -------------------------------------------- |
| `VARCHAR(4000)` where `VARCHAR(255)` suffices | Wasted memory estimation, poor query plans | Right-size based on actual data max length   |
| `NUMERIC` where `INTEGER` works               | Slower arithmetic, more storage            | Use INTEGER for whole numbers within range   |
| `TEXT` for short constrained values           | No length validation                       | Use `VARCHAR(n)` with appropriate constraint |
| `TIMESTAMPTZ` where `DATE` suffices           | Unnecessary precision                      | Use DATE if time component is never used     |

### Step 5 — Review Partitioning Opportunities

Consider partitioning when:

- Table exceeds **10 million rows**
- Queries consistently filter by a date/time range
- Data has a natural archival pattern (older data is rarely accessed)
- Bulk deletes of old data are frequent (DROP PARTITION vs DELETE)

Partitioning strategies by platform:

| Platform   | Feature                                          | Minimum Version           |
| ---------- | ------------------------------------------------ | ------------------------- |
| PostgreSQL | Declarative partitioning (RANGE, LIST, HASH)     | v10+                      |
| MySQL      | Native partitioning (RANGE, LIST, HASH, KEY)     | v5.1+                     |
| SQL Server | Table partitioning (partition function + scheme) | Enterprise edition        |
| MongoDB    | Sharding (range, hashed, zone)                   | v3.6+ (recommended v4.4+) |

### Step 6 — Platform-Specific Configuration

#### PostgreSQL Targets

| Setting                   | Recommended Baseline   | Notes                            |
| ------------------------- | ---------------------- | -------------------------------- |
| `shared_buffers`          | 25% of system RAM      | Primary cache                    |
| `work_mem`                | 64MB–256MB per query   | For sorts and hash joins         |
| `maintenance_work_mem`    | 512MB–1GB              | For VACUUM, CREATE INDEX         |
| `effective_cache_size`    | 75% of system RAM      | Query planner hint               |
| `random_page_cost`        | 1.1 (SSD) or 4.0 (HDD) | Affects index vs seq scan choice |
| `pg_stat_statements`      | ENABLED                | Query performance tracking       |
| `ANALYZE` after bulk load | REQUIRED               | Statistics must be fresh         |

#### MongoDB Targets

| Setting              | Recommended Baseline                          | Notes                          |
| -------------------- | --------------------------------------------- | ------------------------------ |
| WiredTiger cache     | 50% of RAM minus 1GB                          | Default is usually correct     |
| Shard key selection  | High cardinality, query-aligned               | Cannot change after sharding   |
| Read concern         | `local` for speed, `majority` for consistency | Per-operation setting          |
| Write concern        | `w: 1` minimum, `w: majority` for critical    | Durability vs speed            |
| Connection pool size | Match application thread count                | Over-sizing wastes connections |

### Step 7 — Generate Recommendations

Use `templates/tuning-recommendations.md` to produce the report.

Rate each recommendation:

| Impact | Effort       | Priority                     |
| ------ | ------------ | ---------------------------- |
| High   | Quick Fix    | P1 — do immediately          |
| High   | Moderate     | P2 — do before go-live       |
| Medium | Quick Fix    | P2 — do before go-live       |
| High   | Major Change | P3 — plan for post-migration |
| Medium | Moderate     | P3 — plan for post-migration |
| Low    | Any          | P4 — backlog                 |

## Supporting Files

| File                                                                       | Purpose                                                   |
| -------------------------------------------------------------------------- | --------------------------------------------------------- |
| [scripts/explain-analyse.sql](scripts/explain-analyse.sql)                 | EXPLAIN ANALYZE syntax for PG, MySQL, SQL Server, MongoDB |
| [templates/tuning-recommendations.md](templates/tuning-recommendations.md) | Output template for performance recommendations           |
