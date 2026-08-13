-- =============================================================================
-- 007_create_gettestsbybatchid.sql
-- Purpose : Create stored procedure dbo.GetTestsByBatchID.
--           Returns all test rows (Histology, Antibodies, Stain) for a given
--           batch, with derived OnHold and Archived flag columns.
-- Applies : ALL environments.
-- Run as  : db_owner or DDL admin on the Histology database.
-- Idempotent: Yes — uses CREATE OR ALTER.
-- Migration gap: The legacy VB.NET app built this result set entirely in
--           application memory (clsBatchSummary.CreateTestSummaryData) by
--           iterating over separate SP results.  The migrated C# layer moved
--           the join logic to a single SP for performance and correctness,
--           but the .PRC file was never written, so it was absent from every
--           environment until this script.
-- =============================================================================

USE Histology;
GO

CREATE OR ALTER PROCEDURE dbo.GetTestsByBatchID
    @BatchID int
AS
    -- Histology tests
    SELECT
        bh.ID,
        bh.BlockID,
        bb.BlockRef,
        a.HistologyRef,
        'Histology'                             AS TestType,
        bh.Code,
        lh.Description                          AS TestDetails,
        bh.Result,
        bh.QCCode,
        CAST(bh.QCNote AS bit)                  AS QCNote,
        bh.QCNoteRef,
        bh.StainRef,
        CAST(bh.Dispatched AS bit)              AS Dispatched,
        bh.DispatchedDate,
        CAST(bh.DispatchedBy AS varchar(50))    AS DispatchedBy,
        bh.DispatchedTo,
        bh.Comment,
        bh.RemedialAction,
        bh.ArchiveLocation,
        bh.ArchivedDate,
        bh.ArchiveComment,
        bh.NumberOfSlides,
        CAST(CASE WHEN bb.Status = 2 THEN 1 ELSE 0 END AS bit) AS OnHold,
        CAST(CASE WHEN bh.ArchiveLocation IS NOT NULL
                   AND bh.ArchivedDate    IS NOT NULL
                  THEN 1 ELSE 0 END AS bit)     AS Archived,
        bh.RowStamp
    FROM  BatchBlock    bb
    JOIN  BlockHistology bh ON bh.BlockID = bb.ID
    JOIN  Animal         a  ON a.ID       = bb.AnimalID
    LEFT  JOIN luHistology lh ON lh.Code  = bh.Code
    WHERE bb.BatchID = @BatchID

    UNION ALL

    -- Antibody tests (looks up TSE and Non-TSE antibody tables)
    SELECT
        ba.ID,
        ba.BlockID,
        bb.BlockRef,
        a.HistologyRef,
        'Antibodies'                            AS TestType,
        ba.Code,
        COALESCE(lt.Description, ln.Description) AS TestDetails,
        ba.Result,
        ba.QCCode,
        CAST(ba.QCNote AS bit)                  AS QCNote,
        ba.QCNoteRef,
        ba.StainRef,
        CAST(ba.Dispatched AS bit)              AS Dispatched,
        ba.DispatchedDate,
        CAST(ba.DispatchedBy AS varchar(50))    AS DispatchedBy,
        ba.DispatchedTo,
        ba.Comment,
        ba.RemedialAction,
        ba.ArchiveLocation,
        ba.ArchivedDate,
        ba.ArchiveComment,
        ba.NumberOfSlides,
        CAST(CASE WHEN bb.Status = 2 THEN 1 ELSE 0 END AS bit) AS OnHold,
        CAST(CASE WHEN ba.ArchiveLocation IS NOT NULL
                   AND ba.ArchivedDate    IS NOT NULL
                  THEN 1 ELSE 0 END AS bit)     AS Archived,
        ba.RowStamp
    FROM  BatchBlock      bb
    JOIN  BlockAntibodies  ba ON ba.BlockID = bb.ID
    JOIN  Animal           a  ON a.ID       = bb.AnimalID
    LEFT  JOIN luTSEAntibodies    lt ON lt.Code = ba.Code
    LEFT  JOIN luNonTSEAntibodies ln ON ln.Code = ba.Code
    WHERE bb.BatchID = @BatchID

    UNION ALL

    -- Special stain tests
    SELECT
        bs.ID,
        bs.BlockID,
        bb.BlockRef,
        a.HistologyRef,
        'Stain'                                 AS TestType,
        bs.Code,
        ls.Description                          AS TestDetails,
        bs.Result,
        bs.QCCode,
        CAST(bs.QCNote AS bit)                  AS QCNote,
        bs.QCNoteRef,
        bs.StainRef,
        CAST(bs.Dispatched AS bit)              AS Dispatched,
        bs.DispatchedDate,
        CAST(bs.DispatchedBy AS varchar(50))    AS DispatchedBy,
        bs.DispatchedTo,
        bs.Comment,
        bs.RemedialAction,
        bs.ArchiveLocation,
        bs.ArchivedDate,
        bs.ArchiveComment,
        bs.NumberOfSlides,
        CAST(CASE WHEN bb.Status = 2 THEN 1 ELSE 0 END AS bit) AS OnHold,
        CAST(CASE WHEN bs.ArchiveLocation IS NOT NULL
                   AND bs.ArchivedDate    IS NOT NULL
                  THEN 1 ELSE 0 END AS bit)     AS Archived,
        bs.RowStamp
    FROM  BatchBlock   bb
    JOIN  BlockStain    bs ON bs.BlockID = bb.ID
    JOIN  Animal        a  ON a.ID       = bb.AnimalID
    LEFT  JOIN luSpecialStain ls ON ls.Code = bs.Code
    WHERE bb.BatchID = @BatchID

    ORDER BY BlockRef, TestType, Code;
GO
