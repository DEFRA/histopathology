# SQL Server Quirks — Azure SQL Database & Amazon RDS SQL Server Compatibility Reference

Known compatibility issues when migrating from SQL Server 2022 on-premises to Azure SQL Database or Amazon RDS SQL Server. Each entry documents an on-premises feature that is unavailable, restricted, or behaves differently on the cloud target.

---

## 1. Windows Authentication / Kerberos

- **Applies To**: Both targets
- **Pattern**: `CREATE LOGIN [DOMAIN\user] FROM WINDOWS`, `EXECUTE AS LOGIN = 'DOMAIN\user'`, `sp_grantlogin`
- **Problem**:
  - Azure SQL Database: Windows Authentication not supported. Must use Azure Active Directory or SQL Authentication.
  - Amazon RDS SQL Server: Supported via AWS Managed Microsoft AD but requires additional setup.
- **Fix**:
  - Azure SQL Database: Replace Windows logins with Azure AD logins (`CREATE LOGIN [user@domain.com] FROM EXTERNAL PROVIDER`).
  - RDS: Configure AWS Managed Microsoft AD or convert to SQL Authentication.
- **Severity**: Blocker (Azure SQL Database), High (RDS)
- **Added**: 2026-07, baseline

---

## 2. Linked Servers

- **Applies To**: Azure SQL Database (Blocker); Amazon RDS SQL Server (restricted)
- **Pattern**: `CREATE LINKED SERVER`, `EXEC sp_addlinkedserver`, `OPENQUERY(linked_server, ...)`, four-part identifiers `server.database.schema.table`
- **Problem**:
  - Azure SQL Database: Linked servers are not supported at all.
  - Amazon RDS SQL Server: Supported between RDS instances but restricted for on-premises targets.
- **Fix**:
  - Azure SQL Database: Replace with Elastic Query, Azure Data Factory pipelines, or application-layer federation.
  - RDS: Test linked server connectivity; document external dependencies.
- **Severity**: Blocker (Azure SQL Database), Medium (RDS)
- **Added**: 2026-07, baseline

---

## 3. Cross-Database Queries

- **Applies To**: Azure SQL Database only
- **Pattern**: `USE [OtherDatabase]`, three-part identifiers `OtherDatabase.dbo.TableName` in stored procedures and views
- **Problem**: Azure SQL Database is single-database scope. Cross-database `USE` statements and three-part identifiers referencing other databases fail.
- **Fix**: Use Elastic Query, move logic to application layer, or consolidate databases.
- **Severity**: Blocker (Azure SQL Database), Info (RDS — fully supported)
- **Added**: 2026-07, baseline

---

## 4. CLR Integration (Custom Assemblies)

- **Applies To**: Azure SQL Database (Blocker); Amazon RDS SQL Server (option group required)
- **Pattern**: `CREATE ASSEMBLY`, `EXTERNAL NAME assembly.class.method`, CLR user-defined types/aggregates/functions
- **Problem**:
  - Azure SQL Database: CLR integration is blocked. No custom assemblies allowed.
  - Amazon RDS SQL Server: Disabled by default; enable via `clr enabled = 1` in a custom DB parameter group. SAFE assemblies only.
- **Fix**:
  - Azure SQL Database: Rewrite CLR logic in T-SQL or move to application code / Azure Functions.
  - RDS: Enable CLR in parameter group; verify all assemblies are SAFE permission level.
- **Severity**: Blocker (Azure SQL Database), Medium (RDS)
- **Added**: 2026-07, baseline

---

## 5. FILESTREAM / FILETABLE

- **Applies To**: Both targets
- **Pattern**: `FILESTREAM` column attribute, `CREATE TABLE ... AS FileTable`, `FILETABLE_DIRECTORY`, `CONTAINSTABLE` on FILESTREAM columns
- **Problem**: FILESTREAM and FILETABLE require direct OS-level file system access, which is unavailable on both managed platforms.
- **Fix**: Migrate binary content to Azure Blob Storage (Azure SQL) or Amazon S3 (RDS). Store only the URI or metadata in the database. Update application code to use cloud storage SDK.
- **Severity**: Blocker (both targets)
- **Added**: 2026-07, baseline

---

## 6. SQL Server Agent Jobs

- **Applies To**: Azure SQL Database (Blocker); Amazon RDS SQL Server (restricted)
- **Pattern**: `msdb.dbo.sp_add_job`, `msdb.dbo.sp_add_jobstep`, `msdb.dbo.sp_add_schedule`, `msdb.dbo.sp_attach_schedule`
- **Problem**:
  - Azure SQL Database: SQL Server Agent is not available. All `msdb` agent procedures fail.
  - Amazon RDS SQL Server: SQL Agent is available but `msdb` direct writes are restricted via RDS-proxied procedures.
- **Fix**:
  - Azure SQL Database: Migrate jobs to Elastic Jobs, Azure Automation, or Azure Logic Apps / Azure Functions with timer triggers.
  - RDS: Verify job compatibility with RDS wrapper procedures.
- **Severity**: Blocker (Azure SQL Database), High (RDS)
- **Added**: 2026-07, baseline

---

## 7. Database Mail

- **Applies To**: Azure SQL Database (not available); Amazon RDS SQL Server (option group required)
- **Pattern**: `msdb.dbo.sp_send_dbmail`, `msdb.dbo.sysmail_*`, `sp_configure 'Database Mail XPs'`
- **Problem**:
  - Azure SQL Database: Database Mail is not available.
  - Amazon RDS SQL Server: Available via the `SQLSERVER_AGENT` RDS option group.
- **Fix**:
  - Azure SQL Database: Replace with Azure Communication Services Email, SendGrid, or Logic Apps.
  - RDS: Enable Database Mail option in the RDS option group.
- **Severity**: High (Azure SQL Database), Medium (RDS)
- **Added**: 2026-07, baseline

---

## 8. BULK INSERT with Local File Paths

- **Applies To**: Both targets
- **Pattern**: `BULK INSERT table FROM 'C:\data\file.csv'`, `OPENROWSET(BULK 'C:\...', ...)`, BCP with local paths
- **Problem**: Local file system paths are inaccessible from managed database services.
- **Fix**:
  - Azure SQL Database: Use `BULK INSERT ... FROM 'https://storage.blob.core.windows.net/...'` with a `DATA_SOURCE`.
  - Amazon RDS SQL Server: Use S3 paths with S3 integration option enabled.
- **Severity**: High (both targets)
- **Added**: 2026-07, baseline

---

## 9. Service Broker

- **Applies To**: Azure SQL Database (limited); Amazon RDS SQL Server (limited)
- **Pattern**: `CREATE MESSAGE TYPE`, `CREATE CONTRACT`, `CREATE QUEUE`, `CREATE SERVICE`, `SEND ON CONVERSATION`, `BEGIN DIALOG CONVERSATION`
- **Problem**:
  - Azure SQL Database: Single-database Service Broker is supported but cross-database and cross-server Service Broker is not.
  - Amazon RDS SQL Server: Available but cross-server routing is restricted.
- **Fix**: For cross-database/server messaging replace with Azure Service Bus (Azure SQL) or Amazon SQS (RDS). Internal-only usage: test and document scope.
- **Severity**: High (cross-database/server), Low (internal-only)
- **Added**: 2026-07, baseline

---

## 10. Resource Governor

- **Applies To**: Azure SQL Database only
- **Pattern**: `CREATE RESOURCE POOL`, `CREATE WORKLOAD GROUP`, `ALTER RESOURCE GOVERNOR`
- **Problem**: Resource Governor is not available on Azure SQL Database. Workload management is handled by the service tier.
- **Fix**: Remove Resource Governor DDL. Map workload isolation requirements to elastic pools, separate databases, or vCore tier selection.
- **Severity**: High (Azure SQL Database), Info (RDS — supported)
- **Added**: 2026-07, baseline

---

## 11. Server-Level DDL Triggers

- **Applies To**: Azure SQL Database only
- **Pattern**: `CREATE TRIGGER ... ON ALL SERVER`, server-scoped trigger DDL
- **Problem**: Azure SQL Database operates at database scope only. Server-level triggers are not supported.
- **Fix**: Convert to database-level triggers where applicable. For auditing use Azure SQL Auditing or Microsoft Defender for SQL.
- **Severity**: High (Azure SQL Database), Info (RDS — supported)
- **Added**: 2026-07, baseline

---

## 12. Extended Stored Procedures (xp_)

- **Applies To**: Both targets
- **Pattern**: `EXEC xp_cmdshell`, `xp_fileexist`, `xp_sendmail`, `xp_logevent`, `xp_instance_regread`
- **Problem**:
  - Azure SQL Database: Most `xp_` procedures are unavailable.
  - Amazon RDS SQL Server: `xp_cmdshell` is permanently disabled. Registry-access XPs are unavailable.
- **Fix**: Replace OS-interaction procedures with application code or cloud services. Replace `xp_logevent` with Application Insights / CloudWatch.
- **Severity**: Blocker (`xp_cmdshell`), High (others)
- **Added**: 2026-07, baseline

---

## 13. Backup / Restore to Local Disk Paths

- **Applies To**: Both targets
- **Pattern**: `BACKUP DATABASE ... TO DISK = 'C:\...'`, `RESTORE DATABASE ... FROM DISK = 'C:\...'`
- **Problem**: Managed services do not allow direct disk-path backup/restore commands.
- **Fix**:
  - Azure SQL Database: Use built-in automated backups (point-in-time restore). For export use `BACPAC` via `SqlPackage.exe`.
  - Amazon RDS SQL Server: Use `msdb.dbo.rds_backup_database` with S3 bucket target.
- **Severity**: High (both targets)
- **Added**: 2026-07, baseline

---

## 14. Always On Availability Group Objects

- **Applies To**: Both targets (Info level)
- **Pattern**: `CREATE AVAILABILITY GROUP`, `ALTER AVAILABILITY GROUP`, `sys.availability_groups` references in application queries
- **Problem**: Platform-managed HA replaces on-premises AG configuration. AG DDL cannot be deployed on managed services.
- **Fix**: Remove AG DDL from migration scripts. Update monitoring queries to use platform-native monitoring.
- **Severity**: Info (both targets)
- **Added**: 2026-07, baseline

---

## 15. Transparent Data Encryption (TDE) Certificates

- **Applies To**: Both targets (Info level)
- **Pattern**: `CREATE CERTIFICATE ... WITH SUBJECT = 'TDE Certificate'`, `CREATE DATABASE ENCRYPTION KEY`
- **Problem**: Both platforms manage TDE natively. On-premises TDE certificates do not need to be migrated.
- **Fix**: Remove TDE certificate DDL from migration scripts. Enable encryption at the platform level.
- **Severity**: Info (both targets)
- **Added**: 2026-07, baseline

---

## 16. Compatibility Level Below 150

- **Applies To**: Azure SQL Database (minimum 150); Amazon RDS SQL Server (varies by version)
- **Pattern**: `ALTER DATABASE ... SET COMPATIBILITY_LEVEL = 100`, `= 110`, `= 120`, `= 130`, `= 140`
- **Problem**: Azure SQL Database requires compatibility level 150 or higher. Lower levels are rejected.
- **Fix**: Test database at compatibility level 150 on dev/staging before migration. Address breaking behaviour changes.
- **Severity**: High (Azure SQL Database if current level < 150), Low (RDS)
- **Added**: 2026-07, baseline

---

## 17. PolyBase External Tables

- **Applies To**: Both targets
- **Pattern**: `CREATE EXTERNAL DATA SOURCE`, `CREATE EXTERNAL FILE FORMAT`, `CREATE EXTERNAL TABLE`
- **Problem**: PolyBase is not available on Azure SQL Database or Amazon RDS SQL Server.
- **Fix**: Replace with Azure Data Factory, AWS Glue, or application-layer ETL. For Azure SQL cross-database querying use Elastic Query.
- **Severity**: High (both targets if used)
- **Added**: 2026-07, baseline

---

## 18. Startup Stored Procedures

- **Applies To**: Amazon RDS SQL Server only
- **Pattern**: `EXEC sp_procoption @ProcName = 'proc_name', @OptionName = 'startup', @OptionValue = 'true'`
- **Problem**: RDS SQL Server does not support custom startup stored procedures.
- **Fix**: Move startup logic to application initialisation code.
- **Severity**: High (RDS), Info (Azure SQL Database — startup procedures not applicable)
- **Added**: 2026-07, baseline

---

## Adding New Patterns

After each engagement add newly discovered incompatibilities using this format:

```markdown
## [N]. [Feature / Pattern Name]

- **Applies To**: Azure SQL Database | Amazon RDS SQL Server | Both
- **Pattern**: What to scan for in source schema or procedure bodies
- **Problem**: Why it fails or behaves differently on the target platform
- **Fix**: The correct remediation or workaround
- **Severity**: Blocker / High / Medium / Low / Info
- **Added**: [date], [engagement reference]
```



---

## 3. CROSS APPLY / OUTER APPLY

- **Pattern**: `CROSS APPLY`, `OUTER APPLY` with table-valued functions or subqueries
- **Problem**: Not available in PostgreSQL (use `LATERAL JOIN`). Not available in MySQL <8.0.14.
- **Fix**: Convert `CROSS APPLY` → `CROSS JOIN LATERAL`. Convert `OUTER APPLY` → `LEFT JOIN LATERAL ... ON true`.
- **Example**:

  ```sql
  -- SQL Server
  SELECT o.id, i.* FROM orders o CROSS APPLY (
      SELECT TOP 3 * FROM order_items WHERE order_id = o.id ORDER BY price DESC
  ) i;

  -- PostgreSQL
  SELECT o.id, i.* FROM orders o CROSS JOIN LATERAL (
      SELECT * FROM order_items WHERE order_id = o.id ORDER BY price DESC LIMIT 3
  ) i;
  ```

- **Confidence**: High
- **Added**: 2025-01, baseline

---

## 4. Temporary Tables with # Prefix

- **Pattern**: `#temp_table`, `##global_temp_table`, `CREATE TABLE #staging`
- **Problem**: PostgreSQL does not use `#` prefix. `#temp` in PG is a syntax error.
- **Fix**: Replace `#table_name` with `temp_table_name` and use `CREATE TEMP TABLE`. For `##global_temp` (visible across sessions), use a regular table with cleanup.
- **Example**:

  ```sql
  -- SQL Server
  CREATE TABLE #staging (id INT, data NVARCHAR(100));
  INSERT INTO #staging SELECT ...;

  -- PostgreSQL
  CREATE TEMP TABLE staging (id INTEGER, data VARCHAR(100));
  INSERT INTO staging SELECT ...;
  ```

- **Confidence**: High
- **Added**: 2025-01, baseline

---

## 5. Table Variables

- **Pattern**: `DECLARE @table TABLE (col1 INT, col2 VARCHAR(100))`, `INSERT INTO @table`
- **Problem**: PostgreSQL has no table variables. Table variables are memory-resident in SQL Server (small data) or tempdb-backed (large data).
- **Fix**: Convert to `CREATE TEMP TABLE` or use arrays/JSONB for simple cases. For performance-critical code, CTE may work.
- **Example**:

  ```sql
  -- SQL Server
  DECLARE @results TABLE (id INT, name VARCHAR(100));
  INSERT INTO @results SELECT id, name FROM users WHERE active = 1;
  SELECT * FROM @results;

  -- PostgreSQL
  CREATE TEMP TABLE results (id INTEGER, name VARCHAR(100));
  INSERT INTO results SELECT id, name FROM users WHERE active;
  SELECT * FROM results;
  DROP TABLE results;
  ```

- **Confidence**: High
- **Added**: 2025-01, baseline

---

## 6. TRY/CATCH Exception Handling

- **Pattern**: `BEGIN TRY ... END TRY BEGIN CATCH ... END CATCH`, `ERROR_MESSAGE()`, `ERROR_NUMBER()`, `ERROR_SEVERITY()`
- **Problem**: PostgreSQL uses `BEGIN ... EXCEPTION WHEN ... THEN ... END` (PL/pgSQL blocks). The error metadata functions don't exist.
- **Fix**: Convert to PL/pgSQL EXCEPTION blocks. Use `SQLERRM` and `SQLSTATE` for error details. Note: PL/pgSQL exception blocks create a subtransaction (performance impact).
- **Example**:

  ```sql
  -- SQL Server
  BEGIN TRY
      INSERT INTO orders (...) VALUES (...);
  END TRY
  BEGIN CATCH
      PRINT ERROR_MESSAGE();
      THROW;
  END CATCH;

  -- PostgreSQL (in PL/pgSQL function)
  BEGIN
      INSERT INTO orders (...) VALUES (...);
  EXCEPTION WHEN OTHERS THEN
      RAISE NOTICE '%', SQLERRM;
      RAISE;
  END;
  ```

- **Confidence**: High
- **Added**: 2025-01, baseline

---

## 7. RAISERROR vs THROW

- **Pattern**: `RAISERROR('message', severity, state)`, `THROW error_number, 'message', state`
- **Problem**: Neither syntax exists in PostgreSQL. `RAISERROR` has complex format strings (%s, %d, etc.) and severity levels. `THROW` (SQL Server 2012+) is simpler.
- **Fix**: Convert both to `RAISE EXCEPTION 'message'` (PL/pgSQL). For format strings, use `RAISE EXCEPTION 'message: %', variable`. Severity mapping: SS severity 16+ → PG EXCEPTION; severity 10 → PG NOTICE.
- **Example**:

  ```sql
  -- SQL Server
  RAISERROR('Order %d not found', 16, 1, @order_id);

  -- PostgreSQL
  RAISE EXCEPTION 'Order % not found', p_order_id;
  ```

- **Confidence**: High
- **Added**: 2025-01, baseline

---

## 8. SET NOCOUNT ON

- **Pattern**: `SET NOCOUNT ON` at start of procedures
- **Problem**: Suppresses "n rows affected" messages in SQL Server. PostgreSQL doesn't send row count messages by default, so this has no equivalent.
- **Fix**: Remove the line entirely. It's a no-op in PostgreSQL.
- **Example**:

  ```sql
  -- SQL Server
  CREATE PROCEDURE get_orders AS
  BEGIN
      SET NOCOUNT ON;
      SELECT * FROM orders;
  END;

  -- PostgreSQL (simply remove SET NOCOUNT ON)
  CREATE OR REPLACE FUNCTION get_orders() RETURNS SETOF orders AS $$
  BEGIN
      RETURN QUERY SELECT * FROM orders;
  END;
  $$ LANGUAGE plpgsql;
  ```

- **Confidence**: High
- **Added**: 2025-01, baseline

---

## 9. @@IDENTITY vs SCOPE_IDENTITY()

- **Pattern**: `SELECT @@IDENTITY`, `SELECT SCOPE_IDENTITY()`, `IDENT_CURRENT('table')`
- **Problem**: PostgreSQL has no equivalent global variables. `@@IDENTITY` can return values from triggers (wrong table); `SCOPE_IDENTITY()` is safer but still not available.
- **Fix**: Use `RETURNING id` on INSERT statements. This is more reliable and explicit. For existing code calling `SCOPE_IDENTITY()` after insert: refactor to use `INSERT ... RETURNING`.
- **Example**:

  ```sql
  -- SQL Server
  INSERT INTO orders (customer_id) VALUES (1);
  SELECT @new_id = SCOPE_IDENTITY();

  -- PostgreSQL
  INSERT INTO orders (customer_id) VALUES (1) RETURNING id INTO v_new_id;
  ```

- **Confidence**: High
- **Added**: 2025-01, baseline

---

## 10. Computed (Persisted) Columns

- **Pattern**: `AS (expression)`, `AS (expression) PERSISTED`
- **Problem**: PostgreSQL 12+ has `GENERATED ALWAYS AS (expr) STORED` but the syntax differs. Non-persisted computed columns in SS are virtual; PG only supports stored (persisted) generated columns.
- **Fix**: Convert `PERSISTED` computed columns to `GENERATED ALWAYS AS (expr) STORED`. For non-persisted virtual columns: use a view or expression index if needed for queries.
- **Example**:

  ```sql
  -- SQL Server
  CREATE TABLE orders (
      qty INT, price DECIMAL(10,2),
      total AS (qty * price) PERSISTED
  );

  -- PostgreSQL
  CREATE TABLE orders (
      qty INTEGER, price NUMERIC(10,2),
      total NUMERIC(10,2) GENERATED ALWAYS AS (qty * price) STORED
  );
  ```

- **Confidence**: High (PERSISTED → STORED), Medium (non-persisted virtual)
- **Added**: 2025-01, baseline

---

## 11. MERGE Statement

- **Pattern**: `MERGE INTO target USING source ON ... WHEN MATCHED ... WHEN NOT MATCHED`
- **Problem**: PostgreSQL <15 has no MERGE. PG 15+ has MERGE but with some restrictions vs SQL Server (no OUTPUT clause on MERGE, limited DELETE in WHEN MATCHED).
- **Fix**: For PG <15: use `INSERT ... ON CONFLICT DO UPDATE`. For PG 15+: convert directly, removing unsupported features.
- **Example**:

  ```sql
  -- SQL Server
  MERGE INTO target t USING source s ON t.id = s.id
  WHEN MATCHED THEN UPDATE SET t.name = s.name
  WHEN NOT MATCHED THEN INSERT (id, name) VALUES (s.id, s.name);

  -- PostgreSQL (ON CONFLICT)
  INSERT INTO target (id, name) SELECT id, name FROM source
  ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name;
  ```

- **Confidence**: High
- **Added**: 2025-01, baseline

---

## 12. OUTPUT Clause (INSERT/UPDATE/DELETE)

- **Pattern**: `OUTPUT INSERTED.*`, `OUTPUT DELETED.*`, `OUTPUT INSERTED.id INTO @table`
- **Problem**: PostgreSQL uses `RETURNING` clause instead. `RETURNING` only returns the final state; SQL Server's `OUTPUT DELETED.*` gives the pre-change values.
- **Fix**: Replace `OUTPUT INSERTED.*` with `RETURNING *`. For `OUTPUT DELETED.*`: no direct equivalent — use a trigger or a CTE with a pre-change snapshot.
- **Example**:

  ```sql
  -- SQL Server
  DELETE FROM orders WHERE status = 'cancelled'
  OUTPUT DELETED.id, DELETED.total;

  -- PostgreSQL
  DELETE FROM orders WHERE status = 'cancelled'
  RETURNING id, total;
  ```

- **Confidence**: High (INSERTED → RETURNING), Medium (DELETED → needs CTE/trigger)
- **Added**: 2025-01, baseline

---

## 13. STRING_SPLIT Table-Valued Function

- **Pattern**: `STRING_SPLIT(string, delimiter)`
- **Problem**: Returns a table with a `value` column in SQL Server. PostgreSQL has similar but different functions.
- **Fix**: Use `STRING_TO_TABLE(string, delimiter)` (PG 14+) or `UNNEST(STRING_TO_ARRAY(string, delimiter))`. Column name is different — SS returns `value`, PG functions return unnamed or differently named columns.
- **Example**:

  ```sql
  -- SQL Server
  SELECT value FROM STRING_SPLIT('a,b,c', ',');

  -- PostgreSQL
  SELECT * FROM STRING_TO_TABLE('a,b,c', ',');
  -- or
  SELECT UNNEST(STRING_TO_ARRAY('a,b,c', ','));
  ```

- **Confidence**: High
- **Added**: 2025-01, baseline

---

## 14. OPENQUERY / Linked Servers

- **Pattern**: `OPENQUERY(linked_server, 'query')`, `OPENROWSET`, `linked_server...table`
- **Problem**: No linked server concept in PostgreSQL. Distributed queries use a different mechanism.
- **Fix**: Use `postgres_fdw` (Foreign Data Wrapper) for persistent cross-database access. Use `dblink` extension for ad-hoc remote queries. For non-PostgreSQL remote sources, use the appropriate FDW (e.g., `oracle_fdw`, `mysql_fdw`, `tds_fdw`).
- **Example**:

  ```sql
  -- SQL Server
  SELECT * FROM OPENQUERY(REMOTE_SRV, 'SELECT id, name FROM products');

  -- PostgreSQL (using postgres_fdw)
  CREATE EXTENSION IF NOT EXISTS postgres_fdw;
  CREATE SERVER remote_srv FOREIGN DATA WRAPPER postgres_fdw OPTIONS (host 'remote', dbname 'db');
  CREATE USER MAPPING FOR CURRENT_USER SERVER remote_srv OPTIONS (user 'u', password 'p');
  CREATE FOREIGN TABLE remote_products (id INTEGER, name VARCHAR(100)) SERVER remote_srv OPTIONS (table_name 'products');
  SELECT * FROM remote_products;
  ```

- **Confidence**: Medium (setup complexity varies)
- **Added**: 2025-01, baseline

---

## 15. CURSOR FAST_FORWARD

- **Pattern**: `DECLARE cur CURSOR FAST_FORWARD FOR`, `LOCAL`, `READ_ONLY`, `FORWARD_ONLY`
- **Problem**: PostgreSQL cursor syntax differs. Most SQL Server cursor options (`FAST_FORWARD`, `LOCAL`, `SCROLL`) have different or no equivalents.
- **Fix**: PostgreSQL cursors are forward-only by default and always local to the session. Convert: `DECLARE cur CURSOR FOR SELECT ...`. The `FAST_FORWARD` hint is implicit in PG. For `SCROLL` cursors, use `DECLARE cur SCROLL CURSOR FOR ...`.
- **Example**:

  ```sql
  -- SQL Server
  DECLARE cur CURSOR FAST_FORWARD FOR SELECT id, name FROM users;

  -- PostgreSQL (in PL/pgSQL)
  DECLARE cur CURSOR FOR SELECT id, name FROM users;
  ```

- **Confidence**: High
- **Added**: 2025-01, baseline

---

## 16. EXEC Dynamic SQL

- **Pattern**: `EXEC(@sql)`, `EXEC sp_executesql @sql, N'@param INT', @param = 1`
- **Problem**: PostgreSQL PL/pgSQL uses `EXECUTE` keyword for dynamic SQL, with different parameter binding syntax.
- **Fix**: Replace `EXEC(@sql)` with `EXECUTE sql_var`. Replace `sp_executesql` with `EXECUTE ... USING` for parameterised dynamic SQL.
- **Example**:

  ```sql
  -- SQL Server
  DECLARE @sql NVARCHAR(MAX) = N'SELECT * FROM users WHERE id = @id';
  EXEC sp_executesql @sql, N'@id INT', @id = 42;

  -- PostgreSQL (PL/pgSQL)
  EXECUTE 'SELECT * FROM users WHERE id = $1' USING 42;
  ```

- **Confidence**: High
- **Added**: 2025-01, baseline

---

## 17. GO Batch Separator

- **Pattern**: `GO` between statements in SQL scripts
- **Problem**: `GO` is not SQL — it's a batch separator for SSMS/sqlcmd. PostgreSQL and other databases don't recognise it.
- **Fix**: Remove all `GO` statements. Replace with semicolons `;` if needed for statement termination. If batches are logically important (e.g., DDL that must commit before next statement), use explicit `COMMIT;` or split into separate script files.
- **Example**:

  ```sql
  -- SQL Server (SSMS script)
  CREATE TABLE users (id INT PRIMARY KEY);
  GO
  CREATE INDEX idx_users_name ON users(name);
  GO

  -- PostgreSQL
  CREATE TABLE users (id INTEGER PRIMARY KEY);
  CREATE INDEX idx_users_name ON users(name);
  ```

- **Confidence**: High
- **Added**: 2025-01, baseline
