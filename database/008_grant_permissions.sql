-- =============================================================================
-- 008_grant_permissions.sql
-- Purpose : Grant EXECUTE on all newly created stored procedures to the
--           HistologyUser database principal used by the migrated application.
-- Applies : ALL environments.
-- Run as  : db_owner or security admin on the Histology database.
-- Idempotent: Yes — GRANT is safe to re-run.
-- =============================================================================

USE Histology;
GO

GRANT EXECUTE ON dbo.GetInProgressBatches  TO [HistologyUser];
GRANT EXECUTE ON dbo.GetBatchesNotReceived TO [HistologyUser];
GRANT EXECUTE ON dbo.GetBatchesOnHold      TO [HistologyUser];
GRANT EXECUTE ON dbo.GetQCNotes            TO [HistologyUser];
GRANT EXECUTE ON dbo.GetQCNote             TO [HistologyUser];
GRANT EXECUTE ON dbo.GetTestsByBatchID     TO [HistologyUser];
GO

PRINT 'EXECUTE permissions granted to HistologyUser on all new stored procedures.';
GO
