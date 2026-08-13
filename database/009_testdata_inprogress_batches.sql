-- =============================================================================
-- 009_testdata_inprogress_batches.sql
-- *** TEST DATA — DO NOT RUN IN TEST, STAGING, OR PRODUCTION ***
-- Purpose : Promote three specific batches to BatchStatus = 6 (In Progress)
--           so that the "In Progress" batch list page has data to display
--           during local development smoke-testing of the migrated app.
-- Applies : LOCAL DEV (LocalDB) ONLY.
-- Safe    : No — modifies real batch rows.  Only safe when those batch IDs
--           are known development-only records and will not be promoted to
--           any real environment.
-- =============================================================================

USE Histology;
GO

-- SAFETY CHECK: abort if not on LocalDB
IF @@SERVERNAME NOT LIKE '%(LocalDB)%' AND @@SERVERNAME NOT LIKE '%LOCALDB%'
BEGIN
    RAISERROR('This script must only be run against a LocalDB instance.  Aborting.', 16, 1);
    RETURN;
END
GO

UPDATE Batch
SET    BatchStatus = 6
WHERE  ID IN (29401, 29402, 29404);

PRINT CAST(@@ROWCOUNT AS varchar) + ' batch row(s) set to In Progress (status 6).';
GO
