-- =============================================================================
-- 006_create_getqcnote.sql
-- Purpose : Create stored procedure dbo.GetQCNote.
--           Returns a single QC note by primary key.
-- Applies : ALL environments.
-- Run as  : db_owner or DDL admin on the Histology database.
-- Idempotent: Yes — uses CREATE OR ALTER.
-- Migration gap: The migrated QCNoteRepository calls this SP by name.
--           The legacy SP was named GetQCNoteByID.  The migrated version was
--           renamed and the column set updated to match the new QCNoteModel,
--           but the .PRC file was never written, so it was absent from every
--           environment until this script.
-- =============================================================================

USE Histology;
GO

CREATE OR ALTER PROCEDURE dbo.GetQCNote
    @ID int
AS
    SELECT
        ID,
        CreatedBy,
        CONVERT(varchar(30), DateCreated, 103) AS DateCreated,
        QCText                                 AS Text,
        RowStamp
    FROM QCNotes
    WHERE ID = @ID;
GO
