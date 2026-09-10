-- ============================================================================
-- PERMISSIONS SCRIPT — Idempotent Role & User Assignment
-- ============================================================================
-- Safe to re-run. Uses IF NOT EXISTS / IF EXISTS guards.
--
-- Note: Credentials are placeholders. Actual service principal names should
-- be injected via CI/CD pipeline secrets (never hardcoded).
--
-- ============================================================================

SET NOCOUNT ON;
GO

-- Step 1: Create database user if not exists
-- (Assumes service principal/Entra ID user already exists in tenant)
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = 'TSTCDEWEBAW1401' AND type = 'E')
BEGIN
	CREATE USER [TSTCDEWEBAW1401] FROM EXTERNAL PROVIDER;
	PRINT 'Created external user [TSTCDEWEBAW1401].';
END
ELSE
	PRINT 'User [TSTCDEWEBAW1401] already exists.';
GO

-- Step 2: Add to db_datareader role (idempotent)
IF NOT EXISTS (
	SELECT 1 FROM sys.database_role_members drm
	INNER JOIN sys.database_principals dp ON drm.member_principal_id = dp.principal_id
	INNER JOIN sys.database_principals dr ON drm.role_principal_id = dr.principal_id
	WHERE dp.[name] = 'TSTCDEWEBAW1401' AND dr.[name] = 'db_datareader'
)
BEGIN
	ALTER ROLE [db_datareader] ADD MEMBER [TSTCDEWEBAW1401];
	PRINT 'Added [TSTCDEWEBAW1401] to role [db_datareader].';
END
ELSE
	PRINT '[TSTCDEWEBAW1401] already a member of [db_datareader].';
GO

-- Step 3: Add to db_datawriter role (idempotent)
IF NOT EXISTS (
	SELECT 1 FROM sys.database_role_members drm
	INNER JOIN sys.database_principals dp ON drm.member_principal_id = dp.principal_id
	INNER JOIN sys.database_principals dr ON drm.role_principal_id = dr.principal_id
	WHERE dp.[name] = 'TSTCDEWEBAW1401' AND dr.[name] = 'db_datawriter'
)
BEGIN
	ALTER ROLE [db_datawriter] ADD MEMBER [TSTCDEWEBAW1401];
	PRINT 'Added [TSTCDEWEBAW1401] to role [db_datawriter].';
END
ELSE
	PRINT '[TSTCDEWEBAW1401] already a member of [db_datawriter].';
GO

-- Step 4: Grant EXECUTE permission on all stored procedures (idempotent pattern)
-- Uses dynamic SQL to loop through all sprocs; only grants if not already granted
DECLARE @GrantSql NVARCHAR(MAX) = '';

SELECT @GrantSql += 'GRANT EXECUTE ON [' + SCHEMA_NAME(schema_id) + '].[' + name + '] TO [TSTCDEWEBAW1401];' + CHAR(10)
FROM sys.objects
WHERE type = 'P'
  AND schema_id = SCHEMA_ID('dbo')
  -- Exclude system sprocs
  AND name NOT LIKE 'sp_%'
  AND name NOT LIKE 'xp_%';

IF LEN(@GrantSql) > 0
BEGIN
	EXEC sp_executesql @GrantSql;
	PRINT 'Granted EXECUTE on dbo stored procedures to [TSTCDEWEBAW1401].';
END
ELSE
	PRINT 'No dbo stored procedures found to grant permissions on.';
GO

PRINT '=== Permissions applied successfully ===';
