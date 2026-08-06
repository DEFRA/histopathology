# Performance Tuning Recommendations

> **Migration**: {{SOURCE_PLATFORM}} → {{TARGET_PLATFORM}}
> **Date**: {{DATE}}
> **Analyst**: {{ANALYST}}

---

## Executive Summary

| Metric                | Value           |
| --------------------- | --------------- |
| Total recommendations | {{TOTAL_COUNT}} |
| Critical (P1)         | {{P1_COUNT}}    |
| Before go-live (P2)   | {{P2_COUNT}}    |
| Post-migration (P3)   | {{P3_COUNT}}    |
| Backlog (P4)          | {{P4_COUNT}}    |

---

## Critical Issues — Must Fix Before Go-Live (P1)

Issues that will cause performance failures, outages, or data corruption if not addressed.

| #   | Issue     | Table/Query | Impact     | Fix     | Effort    |
| --- | --------- | ----------- | ---------- | ------- | --------- |
| 1   | {{ISSUE}} | {{TABLE}}   | {{IMPACT}} | {{FIX}} | Quick Fix |

---

## Performance Improvements — Recommended Before Go-Live (P2)

Issues that degrade performance but won't cause outages.

### Index Recommendations

| #   | Table     | Recommendation             | Reason     | DDL                                                             |
| --- | --------- | -------------------------- | ---------- | --------------------------------------------------------------- |
| 1   | {{TABLE}} | Add index on `{{COLUMNS}}` | {{REASON}} | `CREATE INDEX idx_{{table}}_{{col}} ON {{table}}({{columns}});` |

### Missing FK Indexes

Foreign key columns without indexes cause slow JOINs and slow cascaded deletes.

| #   | Table     | FK Column  | Referenced Table | DDL                                                       |
| --- | --------- | ---------- | ---------------- | --------------------------------------------------------- |
| 1   | {{TABLE}} | {{FK_COL}} | {{REF_TABLE}}    | `CREATE INDEX idx_{{table}}_{{fk}} ON {{table}}({{fk}});` |

### Redundant Indexes

Indexes that are subsets of other indexes and can be safely removed.

| #   | Table     | Redundant Index | Covered By    | DDL                       |
| --- | --------- | --------------- | ------------- | ------------------------- |
| 1   | {{TABLE}} | `{{INDEX_1}}`   | `{{INDEX_2}}` | `DROP INDEX {{INDEX_1}};` |

### Data Type Optimisations

| #   | Table     | Column     | Current Type | Recommended Type | Reason     |
| --- | --------- | ---------- | ------------ | ---------------- | ---------- |
| 1   | {{TABLE}} | {{COLUMN}} | {{CURRENT}}  | {{RECOMMENDED}}  | {{REASON}} |

---

## Partitioning Recommendations (P3)

Tables that would benefit from partitioning based on size and query patterns.

| #   | Table     | Row Count     | Partition Strategy | Key Column   | Rationale                            |
| --- | --------- | ------------- | ------------------ | ------------ | ------------------------------------ |
| 1   | {{TABLE}} | {{ROW_COUNT}} | RANGE (monthly)    | `created_at` | Time-based queries, archival pattern |

---

## Query Plan Analysis

### Slow Queries Identified

| #   | Query (summary)   | Current Time  | Issue                        | Recommendation           |
| --- | ----------------- | ------------- | ---------------------------- | ------------------------ |
| 1   | {{QUERY_SUMMARY}} | {{EXEC_TIME}} | Sequential scan on {{TABLE}} | Add index on {{COLUMNS}} |

### EXPLAIN ANALYZE Results

```
{{PASTE_EXPLAIN_OUTPUT_HERE}}
```

**Interpretation**: {{ANALYSIS}}

---

## Monitoring Setup

Set up these monitoring targets post-migration:

### Azure SQL Database

- [ ] Enable **Query Store** (`ALTER DATABASE [db] SET QUERY_STORE = ON`)
- [ ] Enable **Automatic tuning** recommendations in the Azure Portal
- [ ] Configure **Diagnostic Settings** → send `SQLInsights`, `QueryStoreRuntimeStatistics`, `QueryStoreWaitStatistics` to Log Analytics
- [ ] Set up **Metric Alerts** on DTU/vCore percentage > 80%, connection failures, and deadlocks
- [ ] Monitor `sys.query_store_runtime_stats` for top queries post-migration
- [ ] Track `sys.dm_os_wait_stats` for wait type analysis

### Amazon RDS SQL Server

- [ ] Enable **Performance Insights** on the RDS instance (1-week free retention)
- [ ] Enable **Enhanced Monitoring** (1-second granularity OS metrics)
- [ ] Configure **CloudWatch Alarms** on `CPUUtilization`, `FreeStorageSpace`, and `DatabaseConnections`
- [ ] Enable Query Store as above — applies identically to RDS SQL Server
- [ ] Set up CloudWatch dashboards for `ReadIOPS`, `WriteIOPS`, `ReadLatency`, `WriteLatency`

---

## Configuration Baseline

Recommended initial configuration for the target platform.

### Azure SQL Database

```sql
-- Enable Query Store (required for performance monitoring)
ALTER DATABASE [YourDatabase] SET QUERY_STORE = ON (
    OPERATION_MODE          = READ_WRITE,
    MAX_STORAGE_SIZE_MB     = 1024,
    INTERVAL_LENGTH_MINUTES = 60,
    QUERY_CAPTURE_MODE      = AUTO
);

-- Set compatibility level to match SQL Server 2022 source:
ALTER DATABASE [YourDatabase] SET COMPATIBILITY_LEVEL = 160;

-- Azure SQL resource allocation is managed via the portal / Bicep:
-- Service tier:  General Purpose | Business Critical | Hyperscale
-- vCore count:   2 vCores minimum for production
-- Storage:       auto-grows; enable zone-redundant backup for production
```

### Amazon RDS SQL Server

```sql
-- Query Store and compatibility level (same T-SQL, works on RDS):
ALTER DATABASE [YourDatabase] SET QUERY_STORE = ON (
    OPERATION_MODE          = READ_WRITE,
    MAX_STORAGE_SIZE_MB     = 1024,
    INTERVAL_LENGTH_MINUTES = 60,
    QUERY_CAPTURE_MODE      = AUTO
);

-- RDS instance configuration is managed via the console / CloudFormation:
-- Instance class:  db.r6i.xlarge (minimum for production workloads)
-- Storage type:    gp3 — set baseline IOPS to 3000+ for high-throughput
-- Multi-AZ:        enabled for production (automatic failover)
-- Backup window:   set to low-traffic hours; retention ≥ 7 days
-- Maintenance:     schedule for low-traffic period
```

---

## Summary

| Priority            | Count        | Action Required               |
| ------------------- | ------------ | ----------------------------- |
| P1 — Critical       | {{P1_COUNT}} | Fix immediately               |
| P2 — Before go-live | {{P2_COUNT}} | Complete before migration     |
| P3 — Post-migration | {{P3_COUNT}} | Plan into sprint backlog      |
| P4 — Backlog        | {{P4_COUNT}} | Track for future optimisation |
