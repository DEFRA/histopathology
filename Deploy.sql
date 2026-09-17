-- ============================================================================
-- DEPLOYMENT MANIFEST — One-Time Database Migration
-- ============================================================================
-- This script is idempotent and safe to re-run via CI/CD pipeline.
-- All scripts herein use DROP+CREATE or IF EXISTS guards.
--
-- Deployment checklist:
--   1. Run against target environment (DEV / STAGING / PROD)
--   2. Verify no errors in deployment log
--   3. Log deployment entry in a [dbo].[DeploymentLog] table (optional)
--   4. Tag the commit after successful run (optional but recommended)
--
-- ============================================================================

-- Step 1: Create deployment tracking table (if not exists)
IF OBJECT_ID('[dbo].[DeploymentLog]', 'U') IS NULL
BEGIN
	CREATE TABLE [dbo].[DeploymentLog] (
		[ID] INT IDENTITY(1,1) PRIMARY KEY,
		[ScriptName] NVARCHAR(256) NOT NULL,
		[DeployedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
		[Environment] NVARCHAR(50) NOT NULL,
		[Status] NVARCHAR(20) NOT NULL -- 'Success' or 'Failed'
	);
	PRINT 'Created [dbo].[DeploymentLog] for audit trail.';
END
GO

-- Step 2: Load stored procedures
PRINT '--- Deploying stored procedures ---';
:r .\StoredProcedures\usp_GetCustomer.sql
:r .\dbo.GetUserByEmail.sql
GO

-- Step 3: Apply permissions
PRINT '--- Applying permissions ---';
:r .\Permissions\GrantPermissions.sql
GO

-- Step 4: Deployment script to used to alter/create tables, columns etc
PRINT '--- Create or Alter Table, Column  ---';
:r .\DeploymentScripts\V20260914_01_User_Email_NotNull_NTLogin_Nullable.sql
GO

-- Step 5: Deactivate the Mouse Bioassay / Neuropath user areas
PRINT '--- Deactivating Mouse Bioassay / Neuropath user areas ---';
:r .\DeploymentScripts\V20260917_01_Deactivate_MouseBioassay_Neuropath_UserAreas.sql
GO


-- Step 6: Log successful deployment (optional)
IF OBJECT_ID('[dbo].[DeploymentLog]', 'U') IS NOT NULL
BEGIN
	INSERT INTO [dbo].[DeploymentLog] ([ScriptName], [Environment], [Status])
	VALUES (
		'Deploy.sql',
		ISNULL(DB_NAME(), 'Unknown'),
		'Success'
	);
	PRINT 'Deployment logged successfully.';
END
GO

PRINT '=== Database deployment completed ===';
