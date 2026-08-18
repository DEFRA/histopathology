-- =============================================================================
-- 008_grant_permissions.sql
-- Purpose : Grant EXECUTE on stored procedures used by the migrated application
--           to the HistologyUser database principal.
--
--           GetBatchesWithStatus is a legacy SP now called directly by the
--           migrated BatchRepository for all batch-list pages.
--
--           GetBatchBlocksByID is a legacy SP used by the QualityData page
--           to load all block-level test rows (replaces in-memory DataSet
--           assembly that the legacy session-based app used).
--
-- Applies : ALL environments.
-- Run as  : db_owner or security admin on the Histology database.
-- Idempotent: Yes — GRANT is safe to re-run.
-- =============================================================================

USE Histology;
GO

GRANT EXECUTE ON dbo.GetBatchesWithStatus TO [HistologyUser];
GRANT EXECUTE ON dbo.GetBatchBlocksByID   TO [HistologyUser];

GO

PRINT 'EXECUTE permissions granted to HistologyUser.';
GO
