-- =============================================================================
-- Row Count Comparison — SQL Server Source & Cloud Targets
-- =============================================================================
-- Source:  SQL Server 2022 (on-premises)
-- Targets: Azure SQL Database | Amazon RDS SQL Server
-- The same T-SQL queries run against all three environments.
-- Run on source before migration, on target after, then compare side-by-side.
-- =============================================================================


-- =============================================================================
-- SQL SERVER
-- =============================================================================
-- Option A: Fast estimate from system views (very accurate in SQL Server).

SELECT
    s.name       AS schema_name,
    t.name       AS table_name,
    SUM(p.rows)  AS row_count
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0, 1)
GROUP BY s.name, t.name
ORDER BY s.name, t.name;

-- Option B: Exact counts using sp_MSforeachtable:

EXEC sp_MSforeachtable 'SELECT ''?'' AS table_name, COUNT(*) AS row_count FROM ?';

-- Option C: Using INFORMATION_SCHEMA:

SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME;


-- =============================================================================
-- SIDE-BY-SIDE COMPARISON (run on the target after migration)
-- =============================================================================
-- Load source row counts into a temp table, then compare against target counts.
-- Works identically on Azure SQL Database and RDS SQL Server.

CREATE TABLE #source_counts (
    schema_name  NVARCHAR(128) NOT NULL,
    table_name   NVARCHAR(128) NOT NULL,
    row_count    BIGINT        NOT NULL,
    PRIMARY KEY (schema_name, table_name)
);

-- Populate #source_counts from the Option A output captured on the source:
-- INSERT INTO #source_counts VALUES ('dbo', 'users', 50000);
-- INSERT INTO #source_counts VALUES ('dbo', 'orders', 1200000);

-- Compare:
WITH target_counts AS (
    SELECT
        s.name          AS schema_name,
        t.name          AS table_name,
        SUM(p.rows)     AS row_count
    FROM sys.tables t
    JOIN sys.schemas s    ON t.schema_id = s.schema_id
    JOIN sys.partitions p ON t.object_id = p.object_id
                         AND p.index_id IN (0, 1)
    GROUP BY s.name, t.name
)
SELECT
    COALESCE(src.schema_name + '.' + src.table_name,
             tgt.schema_name + '.' + tgt.table_name) AS table_name,
    src.row_count                                     AS source_count,
    tgt.row_count                                     AS target_count,
    CASE
        WHEN src.row_count IS NULL                                       THEN 'EXTRA ON TARGET'
        WHEN tgt.row_count IS NULL                                       THEN 'MISSING ON TARGET'
        WHEN src.row_count = tgt.row_count                               THEN 'MATCH'
        WHEN ABS(src.row_count - tgt.row_count) * 100.0
             / NULLIF(src.row_count, 0) <= 0.1                           THEN 'WITHIN TOLERANCE'
        ELSE                                                                  'MISMATCH'
    END AS status,
    CASE
        WHEN src.row_count > 0
            THEN ROUND((tgt.row_count - src.row_count) * 100.0
                       / src.row_count, 4)
        ELSE NULL
    END AS diff_pct
FROM #source_counts src
FULL OUTER JOIN target_counts tgt
    ON  src.schema_name = tgt.schema_name
    AND src.table_name  = tgt.table_name
ORDER BY status DESC, table_name;

DROP TABLE #source_counts;
