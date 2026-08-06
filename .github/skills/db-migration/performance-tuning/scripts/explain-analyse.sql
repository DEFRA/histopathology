-- =============================================================================
-- SQL Server Execution Plan & Statistics Analysis
-- =============================================================================
-- Source:  SQL Server 2022 (on-premises)
-- Targets: Azure SQL Database | Amazon RDS SQL Server
-- Run against the source to identify expensive queries before migration.
-- The same T-SQL commands work against Azure SQL Database and RDS SQL Server.
-- Replace <YOUR_QUERY> with the actual query to analyse.
-- =============================================================================


-- =============================================================================
-- SQL SERVER
-- =============================================================================
-- SQL Server uses SET options and graphical execution plans.
-- Run in SSMS or sqlcmd.

-- Enable I/O and time statistics (text output):
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
<YOUR_QUERY>;
SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;

-- Include actual execution plan (XML, viewable in SSMS):
SET STATISTICS XML ON;
<YOUR_QUERY>;
SET STATISTICS XML OFF;

-- Estimated plan only (does not execute):
SET SHOWPLAN_XML ON;
GO
<YOUR_QUERY>;
GO
SET SHOWPLAN_XML OFF;
GO

-- Interpreting SQL Server execution plan:
-- -----------------------------------------
-- Table Scan        → Full table scan. Needs clustered index or covering index.
-- Clustered Index Scan → Scanning entire clustered index. May need non-clustered
--                     index for the filter columns.
-- Index Seek        → Good. Using index efficiently.
-- Key Lookup        → Index found the row but needs to go back to clustered
--                     index for additional columns. Consider INCLUDE columns.
-- Nested Loops      → Good for small inner input.
-- Hash Match        → For large unsorted inputs. Check memory grant warnings.
-- Sort              → Explicit sort. If 'warning: sort spilled to tempdb',
--                     increase memory or add index.
-- Logical reads     → Pages read from buffer cache. Lower is better.
-- Physical reads    → Pages read from disk. Should be 0 on warm cache.
-- CPU time          → Processing time (excludes I/O waits).
-- Elapsed time      → Wall clock time (includes I/O waits).


-- =============================================================================
-- QUERY STORE — TOP RESOURCE-CONSUMING QUERIES
-- =============================================================================
-- Query Store is supported on SQL Server 2016+, Azure SQL Database, and
-- RDS SQL Server. Enable on the target after migration to capture baselines.

-- Enable Query Store on the target database:
ALTER DATABASE [YourDatabase] SET QUERY_STORE = ON (
    OPERATION_MODE     = READ_WRITE,
    MAX_STORAGE_SIZE_MB = 1024,
    INTERVAL_LENGTH_MINUTES = 60,
    QUERY_CAPTURE_MODE = AUTO
);

-- Top 20 queries by average CPU time:
SELECT TOP 20
    qt.query_sql_text,
    rs.avg_cpu_time,
    rs.avg_logical_io_reads,
    rs.avg_elapsed_time,
    rs.count_executions,
    q.query_id,
    p.plan_id
FROM sys.query_store_query_text    qt
JOIN sys.query_store_query         q  ON qt.query_text_id  = q.query_text_id
JOIN sys.query_store_plan          p  ON q.query_id        = p.query_id
JOIN sys.query_store_runtime_stats rs ON p.plan_id         = rs.plan_id
ORDER BY rs.avg_cpu_time DESC;

-- Top 20 queries by average logical reads:
SELECT TOP 20
    qt.query_sql_text,
    rs.avg_logical_io_reads,
    rs.avg_cpu_time,
    rs.avg_elapsed_time,
    rs.count_executions
FROM sys.query_store_query_text    qt
JOIN sys.query_store_query         q  ON qt.query_text_id  = q.query_text_id
JOIN sys.query_store_plan          p  ON q.query_id        = p.query_id
JOIN sys.query_store_runtime_stats rs ON p.plan_id         = rs.plan_id
ORDER BY rs.avg_logical_io_reads DESC;


-- =============================================================================
-- DMV — CURRENTLY EXPENSIVE QUERIES (SQL Server source)
-- =============================================================================
-- Use on the source database before migration to identify queries to baseline.

-- Top 25 cached plans by total logical reads:
SELECT TOP 25
    qs.total_logical_reads / qs.execution_count AS avg_logical_reads,
    qs.total_logical_reads,
    qs.execution_count,
    qs.total_elapsed_time / qs.execution_count  AS avg_elapsed_us,
    SUBSTRING(st.text,
        (qs.statement_start_offset / 2) + 1,
        ((CASE qs.statement_end_offset
              WHEN -1 THEN DATALENGTH(st.text)
              ELSE qs.statement_end_offset
          END - qs.statement_start_offset) / 2) + 1
    ) AS query_text,
    qp.query_plan
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle)   st
CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp
ORDER BY qs.total_logical_reads DESC;

-- Missing index recommendations:
SELECT
    DB_NAME(mid.database_id)                      AS database_name,
    OBJECT_NAME(mid.object_id, mid.database_id)   AS table_name,
    migs.avg_total_user_cost * migs.avg_user_impact
        * (migs.user_seeks + migs.user_scans)      AS improvement_measure,
    mid.equality_columns,
    mid.inequality_columns,
    mid.included_columns,
    migs.user_seeks,
    migs.avg_user_impact
FROM sys.dm_db_missing_index_groups       mig
JOIN sys.dm_db_missing_index_group_stats  migs ON mig.index_group_handle = migs.group_handle
JOIN sys.dm_db_missing_index_details      mid  ON mig.index_handle        = mid.index_handle
ORDER BY improvement_measure DESC;
