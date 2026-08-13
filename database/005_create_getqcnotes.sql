-- =============================================================================
-- 005_create_getqcnotes.sql
-- Purpose : Create stored procedure dbo.GetQCNotes.
--           Returns all QC notes ordered by most recent first.
-- Applies : ALL environments.
-- Run as  : db_owner or DDL admin on the Histology database.
-- Idempotent: Yes — uses CREATE OR ALTER.
-- Migration gap: The migrated QCNoteRepository calls this SP by name.
--           The legacy SP had a different name (GetAllQCNotes) and a different
--           column set.  The migrated version was renamed and the column set
--           was updated to match the new QCNoteModel, but the .PRC file was
--           never written, so it was absent from every environment until this
--           script.
-- Note    : The @ID parameter is accepted but not used in the WHERE clause —
--           this matches the "get all" intent.  See 006_create_getqcnote.sql
--           for the single-record variant.
-- =============================================================================

USE Histology;
GO

CREATE OR ALTER PROCEDURE dbo.GetQCNotes
    @ID int
AS
    SELECT
        ID,
        CreatedBy,
        CONVERT(varchar(30), DateCreated, 103) AS DateCreated,
        QCText                                 AS Text,
        RowStamp
    FROM QCNotes
    ORDER BY ID DESC;
GO
