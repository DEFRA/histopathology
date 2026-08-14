-- =============================================================================
-- 010_create_getallbatches.sql
-- Purpose : Create stored procedure GetAllBatches.
--           Returns all batches regardless of status, ordered by most recent
--           first. Used by BatchesForEditing page (Edit submission status).
-- Applies : ALL environments.
-- Run as  : db_owner or DDL admin on the Histology database.
-- Idempotent: Yes — uses CREATE OR ALTER.
-- Migration gap: The migrated BatchRepository.GetAllBatchesAsync calls this SP
--           by name but it was never created.  The legacy equivalent was
--           GetBatchesWithStatus @BatchStatus = 0 which joined to a linked
--           server (DEFACPVWTSQL003) for species lookup — not available in
--           LocalDB or Azure SQL.  Species is returned as the raw stored value.
-- =============================================================================

USE Histology;
GO

CREATE OR ALTER PROCEDURE dbo.GetAllBatches
AS
    SELECT
        b.ID,
        COALESCE(p.Description, '')         AS ProjectDescription,
        COALESCE(c.Description, '')         AS ContactDescription,
        b.Species,
        b.BatchDate,
        b.DateReceived                      AS ReceivedDate,
        b.DateCompleted                     AS CompletedDate,
        b.OtherSubmittedBy                  AS CustomerRef,
        COALESCE(s.Description,
                 CAST(b.BatchStatus AS varchar(5)))  AS Status,
        CAST(0 AS bit)                      AS AllTissuesAssigned
    FROM Batch b
    LEFT JOIN luProjects p  ON p.ID  = b.ProjectContractCode
    LEFT JOIN luContacts c  ON c.ID  = b.ContactName
    LEFT JOIN luStatus   s  ON s.Code = b.BatchStatus
    ORDER BY b.ID DESC;
GO

GRANT EXECUTE ON dbo.GetAllBatches TO HistologyUser;
GO
