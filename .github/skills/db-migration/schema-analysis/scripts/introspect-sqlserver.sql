-- =============================================================================
-- SQL Server Schema Introspection Script
-- =============================================================================
-- Usage: sqlcmd -S <server> -d <database> -U <user> -P <password> -i introspect-sqlserver.sql
--    or: sqlcmd -S <server> -d <database> -E -i introspect-sqlserver.sql  (Windows auth)
-- Outputs: Complete inventory of database objects for migration analysis
-- =============================================================================

SET NOCOUNT ON;

-- Configuration: Set the target schema (default: dbo)
DECLARE @target_schema NVARCHAR(128) = 'dbo';

PRINT '============================================';
PRINT 'SQL Server Schema Introspection Report';
PRINT '============================================';
PRINT '';

-- -----------------------------------------------------------------------------
-- 1. Database Version & Info
-- -----------------------------------------------------------------------------
PRINT '--- Database Info ---';
SELECT @@VERSION AS database_version;

SELECT
    DB_NAME() AS database_name,
    SERVERPROPERTY('ProductVersion') AS product_version,
    SERVERPROPERTY('Edition') AS edition,
    SERVERPROPERTY('Collation') AS server_collation,
    DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS database_collation;

-- -----------------------------------------------------------------------------
-- 2. Tables with Row Counts and Sizes
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- Tables ---';
SELECT
    s.name AS schema_name,
    t.name AS table_name,
    p.rows AS estimated_row_count,
    CAST(ROUND(SUM(a.total_pages) * 8.0 / 1024, 2) AS DECIMAL(18,2)) AS total_size_mb,
    CAST(ROUND(SUM(a.used_pages) * 8.0 / 1024, 2) AS DECIMAL(18,2)) AS used_size_mb,
    t.create_date,
    t.modify_date,
    CASE WHEN t.temporal_type = 2 THEN 'SYSTEM_VERSIONED' ELSE 'NORMAL' END AS temporal_type
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
INNER JOIN sys.indexes i ON t.object_id = i.object_id AND i.index_id <= 1
INNER JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
WHERE s.name = @target_schema
GROUP BY s.name, t.name, p.rows, t.create_date, t.modify_date, t.temporal_type
ORDER BY p.rows DESC;

-- -----------------------------------------------------------------------------
-- 3. Columns with Data Types
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- Columns ---';
SELECT
    s.name AS schema_name,
    t.name AS table_name,
    c.name AS column_name,
    c.column_id,
    TYPE_NAME(c.user_type_id) AS data_type,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable,
    dc.definition AS default_value,
    c.is_identity,
    CASE WHEN ic.object_id IS NOT NULL
         THEN CAST(ic.seed_value AS VARCHAR) + '/' + CAST(ic.increment_value AS VARCHAR)
         ELSE NULL
    END AS identity_seed_increment,
    c.is_computed,
    cc.definition AS computed_definition,
    c.is_rowguidcol
FROM sys.columns c
INNER JOIN sys.tables t ON c.object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
LEFT JOIN sys.identity_columns ic ON c.object_id = ic.object_id AND c.column_id = ic.column_id
LEFT JOIN sys.computed_columns cc ON c.object_id = cc.object_id AND c.column_id = cc.column_id
WHERE s.name = @target_schema
ORDER BY t.name, c.column_id;

-- -----------------------------------------------------------------------------
-- 4. Primary Keys
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- Primary Keys ---';
SELECT
    s.name AS schema_name,
    t.name AS table_name,
    kc.name AS constraint_name,
    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS key_columns,
    i.type_desc AS index_type
FROM sys.key_constraints kc
INNER JOIN sys.tables t ON kc.parent_object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
INNER JOIN sys.indexes i ON kc.parent_object_id = i.object_id AND kc.unique_index_id = i.index_id
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE kc.type = 'PK' AND s.name = @target_schema
GROUP BY s.name, t.name, kc.name, i.type_desc
ORDER BY t.name;

-- -----------------------------------------------------------------------------
-- 5. Foreign Keys
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- Foreign Keys ---';
SELECT
    s.name AS schema_name,
    OBJECT_NAME(fk.parent_object_id) AS source_table,
    fk.name AS constraint_name,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS source_column,
    OBJECT_NAME(fk.referenced_object_id) AS referenced_table,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS referenced_column,
    fk.update_referential_action_desc AS update_rule,
    fk.delete_referential_action_desc AS delete_rule,
    fk.is_disabled
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.schemas s ON fk.schema_id = s.schema_id
WHERE s.name = @target_schema
ORDER BY OBJECT_NAME(fk.parent_object_id), fk.name, fkc.constraint_column_id;

-- -----------------------------------------------------------------------------
-- 6. Unique Constraints
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- Unique Constraints ---';
SELECT
    s.name AS schema_name,
    t.name AS table_name,
    kc.name AS constraint_name,
    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS columns
FROM sys.key_constraints kc
INNER JOIN sys.tables t ON kc.parent_object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
INNER JOIN sys.indexes i ON kc.parent_object_id = i.object_id AND kc.unique_index_id = i.index_id
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE kc.type = 'UQ' AND s.name = @target_schema
GROUP BY s.name, t.name, kc.name
ORDER BY t.name;

-- -----------------------------------------------------------------------------
-- 7. Check Constraints
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- Check Constraints ---';
SELECT
    s.name AS schema_name,
    OBJECT_NAME(cc.parent_object_id) AS table_name,
    cc.name AS constraint_name,
    cc.definition AS check_clause,
    cc.is_disabled
FROM sys.check_constraints cc
INNER JOIN sys.tables t ON cc.parent_object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = @target_schema
ORDER BY OBJECT_NAME(cc.parent_object_id), cc.name;

-- -----------------------------------------------------------------------------
-- 8. Indexes
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- Indexes ---';
SELECT
    s.name AS schema_name,
    t.name AS table_name,
    i.name AS index_name,
    i.type_desc AS index_type,
    i.is_unique,
    i.is_primary_key,
    i.is_unique_constraint,
    i.has_filter,
    i.filter_definition,
    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS indexed_columns,
    STRING_AGG(CASE WHEN ic.is_included_column = 1 THEN c.name END, ', ') AS included_columns
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE s.name = @target_schema AND i.name IS NOT NULL
GROUP BY s.name, t.name, i.name, i.type_desc, i.is_unique, i.is_primary_key, i.is_unique_constraint, i.has_filter, i.filter_definition
ORDER BY t.name, i.name;

-- -----------------------------------------------------------------------------
-- 9. Views
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- Views ---';
SELECT
    s.name AS schema_name,
    v.name AS view_name,
    m.definition AS view_definition,
    v.is_ms_shipped
FROM sys.views v
INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
INNER JOIN sys.sql_modules m ON v.object_id = m.object_id
WHERE s.name = @target_schema
ORDER BY v.name;

-- -----------------------------------------------------------------------------
-- 10. Stored Procedures
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- Stored Procedures ---';
SELECT
    s.name AS schema_name,
    p.name AS procedure_name,
    m.definition AS procedure_definition,
    p.create_date,
    p.modify_date
FROM sys.procedures p
INNER JOIN sys.schemas s ON p.schema_id = s.schema_id
INNER JOIN sys.sql_modules m ON p.object_id = m.object_id
WHERE s.name = @target_schema AND p.is_ms_shipped = 0
ORDER BY p.name;

-- -----------------------------------------------------------------------------
-- 11. Functions
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- Functions ---';
SELECT
    s.name AS schema_name,
    o.name AS function_name,
    o.type_desc AS function_type,
    m.definition AS function_definition,
    o.create_date,
    o.modify_date
FROM sys.objects o
INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
INNER JOIN sys.sql_modules m ON o.object_id = m.object_id
WHERE s.name = @target_schema
    AND o.type IN ('FN', 'IF', 'TF')
ORDER BY o.name;

-- -----------------------------------------------------------------------------
-- 12. Triggers
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- Triggers ---';
SELECT
    s.name AS schema_name,
    OBJECT_NAME(tr.parent_id) AS table_name,
    tr.name AS trigger_name,
    tr.type_desc AS trigger_type,
    CASE WHEN tr.is_instead_of_trigger = 1 THEN 'INSTEAD OF' ELSE 'AFTER' END AS timing,
    m.definition AS trigger_definition,
    tr.is_disabled
FROM sys.triggers tr
INNER JOIN sys.sql_modules m ON tr.object_id = m.object_id
INNER JOIN sys.tables t ON tr.parent_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = @target_schema
ORDER BY OBJECT_NAME(tr.parent_id), tr.name;

-- -----------------------------------------------------------------------------
-- 13. Sequences
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- Sequences ---';
SELECT
    s.name AS schema_name,
    seq.name AS sequence_name,
    TYPE_NAME(seq.user_type_id) AS data_type,
    seq.start_value,
    seq.increment,
    seq.minimum_value,
    seq.maximum_value,
    seq.is_cycling,
    seq.cache_size,
    seq.current_value
FROM sys.sequences seq
INNER JOIN sys.schemas s ON seq.schema_id = s.schema_id
WHERE s.name = @target_schema
ORDER BY seq.name;

-- -----------------------------------------------------------------------------
-- 14. Linked Servers
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- Linked Servers ---';
SELECT
    name AS linked_server_name,
    product,
    provider,
    data_source,
    catalog
FROM sys.servers
WHERE is_linked = 1
ORDER BY name;

-- -----------------------------------------------------------------------------
-- 15. User-Defined Types
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- User-Defined Types ---';
SELECT
    s.name AS schema_name,
    t.name AS type_name,
    TYPE_NAME(t.system_type_id) AS base_type,
    t.max_length,
    t.precision,
    t.scale,
    t.is_nullable,
    t.is_table_type
FROM sys.types t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.is_user_defined = 1 AND s.name = @target_schema
ORDER BY t.name;

-- -----------------------------------------------------------------------------
-- 16. Partitioned Tables
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- Partition Functions & Schemes ---';
SELECT
    pf.name AS partition_function,
    pf.type_desc AS function_type,
    pf.fanout AS partition_count,
    ps.name AS partition_scheme,
    prv.value AS boundary_value
FROM sys.partition_functions pf
LEFT JOIN sys.partition_schemes ps ON pf.function_id = ps.function_id
LEFT JOIN sys.partition_range_values prv ON pf.function_id = prv.function_id
ORDER BY pf.name, prv.boundary_id;

-- -----------------------------------------------------------------------------
-- 17. SQL Agent Jobs
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- SQL Agent Jobs ---';
SELECT
    j.name AS job_name,
    j.enabled,
    j.description,
    js.step_name,
    js.subsystem,
    js.command
FROM msdb.dbo.sysjobs j
INNER JOIN msdb.dbo.sysjobsteps js ON j.job_id = js.job_id
WHERE j.enabled = 1
ORDER BY j.name, js.step_id;

-- -----------------------------------------------------------------------------
-- 18. Object Summary
-- -----------------------------------------------------------------------------
-- 18. CLR Objects (Azure SQL Database Blocker)
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- CLR Objects ---';
SELECT
    o.name        AS object_name,
    o.type_desc   AS object_type,
    am.assembly_class,
    am.assembly_method,
    a.name        AS assembly_name,
    a.permission_set_desc
FROM sys.assembly_modules am
INNER JOIN sys.objects     o ON am.object_id    = o.object_id
INNER JOIN sys.assemblies  a ON am.assembly_id  = a.assembly_id
ORDER BY o.name;

-- -----------------------------------------------------------------------------
-- 19. FILESTREAM / FILETABLE Columns (Azure SQL Database Blocker)
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- FILESTREAM and FILETABLE Objects ---';
-- FILESTREAM columns
SELECT
    s.name  AS schema_name,
    t.name  AS table_name,
    c.name  AS column_name,
    'FILESTREAM column' AS issue_type
FROM sys.columns c
INNER JOIN sys.tables  t ON c.object_id    = t.object_id
INNER JOIN sys.schemas s ON t.schema_id    = s.schema_id
WHERE c.is_filestream = 1;

-- FILETABLE
SELECT
    s.name  AS schema_name,
    t.name  AS table_name,
    'FILETABLE' AS issue_type
FROM sys.tables  t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.is_filetable = 1;

-- -----------------------------------------------------------------------------
-- 20. Cross-Database References in Modules (Azure SQL Database Blocker)
-- -----------------------------------------------------------------------------
PRINT '';
PRINT '--- Cross-Database References ---';
SELECT DISTINCT
    s.name          AS schema_name,
    o.name          AS object_name,
    o.type_desc     AS object_type
FROM sys.sql_modules m
INNER JOIN sys.objects o ON m.object_id  = o.object_id
INNER JOIN sys.schemas s ON o.schema_id  = s.schema_id
WHERE o.is_ms_shipped = 0
  AND (
        -- three-part references to a different database
        m.definition LIKE '%[A-Za-z_][A-Za-z0-9_]%.dbo.%'
        -- hard-coded USE statements
     OR m.definition LIKE '%USE [%]%'
  )
ORDER BY o.name;

-- -----------------------------------------------------------------------------
-- 21. Object Summary
SELECT
    type_desc AS object_type,
    COUNT(*) AS object_count
FROM sys.objects o
INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
WHERE s.name = @target_schema
    AND o.is_ms_shipped = 0
    AND type_desc NOT IN ('INTERNAL_TABLE', 'SYSTEM_TABLE')
GROUP BY type_desc
ORDER BY type_desc;

PRINT '';
PRINT '============================================';
PRINT 'Introspection complete.';
PRINT '============================================';
