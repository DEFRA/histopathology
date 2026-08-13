-- =============================================================================
-- 004_create_getbatchesonhold.sql
-- Purpose : Create stored procedure GetBatchesOnHold.
--           Returns all batches with BatchStatus = 5 (On Hold), ordered by
--           most recent first.
-- Applies : ALL environments.
-- Run as  : db_owner or DDL admin on the Histology database.
-- Idempotent: Yes — uses CREATE OR ALTER.
-- Migration gap: The migrated BatchRepository calls this SP by name.  The .PRC
--           file was never written during the legacy migration, so it was absent
--           from every environment until this script.
-- =============================================================================

USE Histology;
GO

CREATE OR ALTER PROCEDURE dbo.GetBatchesOnHold
AS
    SELECT
        b.ID,
        CAST(b.BatchStatus AS varchar(5))   AS Status,
        b.OtherSubmittedBy                  AS CustomerRef,
        b.Comments,
        b.DateReceived                      AS ReceivedDate,
        b.DateCompleted                     AS CompletedDate,
        CAST(b.SubmittedBy AS int)          AS SubmittedByUserID,
        CAST(b.SubmittedArea AS int)        AS UserAreaCode,
        CAST(b.Cassetted AS bit)            AS IsPreCassetted,
        b.RowStamp,
        CAST(b.BatchType AS int)            AS BatchType
    FROM Batch b
    WHERE b.BatchStatus = 5
    ORDER BY b.ID DESC;
GO
