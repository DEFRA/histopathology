-- ============================================================================-- ============================================================================
-- DEPLOYMENT MANIFEST — One-Time Database Migration
-- ============================================================================
-- This script is idempotent and safe to re-run via CI/CD pipeline.
-- All scripts herein use DROP+CREATE or IF EXISTS guards.
--
-- Deployment checklist:
--   1. Run against target environment (DEV / STAGING / PROD)
--   2. Verify no errors in deployment log
--   4. Tag the commit after successful run (optional but recommended)
--
-- ============================================================================


-- Step 1: Load stored procedures
PRINT '--- Deploying stored procedures ---';
:r ../StoredProcedures/dbo.GetUserByEmail.sql
GO
:r ../StoredProcedures/dbo.EditResetHistologyRef.sql
GO
:r ../StoredProcedures/dbo.AddUser.sql
GO
:r ../StoredProcedures/dbo.EditUser.sql
GO
:r ../StoredProcedures/dbo.GetAnimalBlockArchiveInformation.sql
GO
:r ../StoredProcedures/dbo.GettlkpSpecies.sql
GO
:r ../StoredProcedures/dbo.AddtlkpSpecies.sql
GO
:r ../StoredProcedures/dbo.EdittlkpSpecies.sql
GO
:r ../StoredProcedures/dbo.DeletetlkpSpecies.sql
GO

-- Step 2: Deployment script to used to alter/create tables, columns etc
PRINT '--- Create or Alter Table, Column  ---';
:r ../Migrations/V20260914_01_User_Email_NotNull_NTLogin_Nullable.sql
GO

-- Step 3: Deactivate the Mouse Bioassay / Neuropath user areas
PRINT '--- Deactivating Mouse Bioassay / Neuropath user areas ---';
:r ../Migrations/V20260917_01_Deactivate_MouseBioassay_Neuropath_UserAreas.sql
GO

-- Step 4: Fix pre-existing blank-string NTLogin rows that collide under IX_User_NTLogin
PRINT '--- Fixing empty-string NTLogin rows to NULL ---';
:r ../Migrations/V20260928_01_Fix_Empty_String_NTLogin_To_Null.sql
GO

-- Step 5: Make tlkpSpecies editable via the picklist admin screen
PRINT '--- Applying picklist Species changes ---';
:r ../Migrations/picklist-species-changes.sql
GO

PRINT '=== Database deployment completed ===';
