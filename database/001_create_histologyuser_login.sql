-- =============================================================================
-- 001_create_histologyuser_login.sql
-- Purpose : Create the HistologyUser SQL Server login and remap the orphaned
--           database user to it.
-- Applies : ALL environments (dev, test, staging, prod) on first setup.
-- Run as  : sysadmin / sa on the target SQL Server instance.
-- Idempotent: Yes — wrapped in IF NOT EXISTS checks.
-- =============================================================================

USE master;
GO

-- Step 1: Create the server-level login if it does not already exist.
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'HistologyUser')
BEGIN
    CREATE LOGIN [HistologyUser]
        WITH PASSWORD   = 'HistologyUser9245',
             CHECK_POLICY = OFF,
             CHECK_EXPIRATION = OFF;
    PRINT 'Created login HistologyUser.';
END
ELSE
BEGIN
    PRINT 'Login HistologyUser already exists — skipped.';
END
GO

-- Step 2: Remap the orphaned database user to the login.
--         The legacy database has a HistologyUser DB user that was created before
--         the server login existed, leaving it orphaned.  ALTER USER repairs the
--         link so SQL-auth connections work correctly.
USE Histology;
GO

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'HistologyUser')
BEGIN
    ALTER USER [HistologyUser] WITH LOGIN = [HistologyUser];
    PRINT 'Remapped HistologyUser DB user to login.';
END
ELSE
BEGIN
    PRINT 'DB user HistologyUser not found — check database name.';
END
GO
