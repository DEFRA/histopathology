-- =============================================================================
-- LOCAL_DEV_ONLY_testdata_pdf_reports.sql
-- *** TEST DATA — DO NOT RUN IN TEST, STAGING, OR PRODUCTION ***
--
-- Purpose : Insert self-contained test data so that all three migrated PDF
--           reports can be exercised on any fresh LocalDB instance.
--
--           HistologyReport  → use batch ID 29401 (or 29402, 29404)
--           SubmissionNotes  → use batch ID 29401 (or 29402, 29404)
--           QCNote           → use qcNoteRef 1911 (or 1910, 1909)
--
-- Navigation:
--   HistologyReport / SubmissionNotes
--     Edit submission status → type 29401 in "Go directly to..." → Edit
--     → BatchDetails → "Print submission form" / "Print submission notes"
--
--   QCNote
--     Navigate directly to: /Reports/QCNote?qcNoteRef=1911
--     Or: BatchDetails → "QC notes" → Print on any row
--
-- Idempotent: Yes — uses MERGE on every table, safe to re-run.
-- Applies   : LOCAL DEV (LocalDB) ONLY.
-- Run as    : db_owner on the Histology database.
-- =============================================================================

USE Histology;
GO

-- SAFETY CHECK: abort if not on LocalDB
IF @@SERVERNAME NOT LIKE '%(LocalDB)%' AND @@SERVERNAME NOT LIKE '%LOCALDB%'
BEGIN
    RAISERROR('This script must only be run against a LocalDB instance. Aborting.', 16, 1);
    RETURN;
END
GO

-- ===========================================================================
-- 1. Lookup tables
-- ===========================================================================

-- luProjects
SET IDENTITY_INSERT luProjects ON;
MERGE luProjects AS tgt
USING (VALUES
    (117, 'FT5008',        1, 3),
    (300, 'FT1434',        1, 1),
    (310, 'DATABASE TEST', 1, 1),
    (440, 'ED1500',        1, 5),
    (464, 'FZ2100',        1, 5)
) AS src (ID, Description, IsActive, Area)
ON tgt.ID = src.ID
WHEN NOT MATCHED THEN
    INSERT (ID, Description, IsActive, Area)
    VALUES (src.ID, src.Description, src.IsActive, src.Area);
SET IDENTITY_INSERT luProjects OFF;
GO

-- luContacts
SET IDENTITY_INSERT luContacts ON;
MERGE luContacts AS tgt
USING (VALUES
    (46,  'A Nunez',       1, 5),
    (72,  'H Nilli',       1, 3),
    (147, 'DIAGNOSTICS',   1, 5)
) AS src (ID, Description, IsActive, Area)
ON tgt.ID = src.ID
WHEN NOT MATCHED THEN
    INSERT (ID, Description, IsActive, Area)
    VALUES (src.ID, src.Description, src.IsActive, src.Area);
SET IDENTITY_INSERT luContacts OFF;
GO

-- luStatus (codes used across test data)
MERGE luStatus AS tgt
USING (VALUES
    (1, 'Not Received'),
    (2, 'Received'),
    (4, 'Completed'),
    (5, 'On Hold'),
    (6, 'In Progress')
) AS src (Code, Description)
ON tgt.Code = src.Code
WHEN NOT MATCHED THEN
    INSERT (Code, Description)
    VALUES (src.Code, src.Description);
GO

-- luHistology code H&E (Code='2')
MERGE luHistology AS tgt
USING (VALUES ('2', 'H&E', 1)) AS src (Code, Description, Active)
ON tgt.Code = src.Code
WHEN NOT MATCHED THEN
    INSERT (Code, Description, Active)
    VALUES (src.Code, src.Description, src.Active);
GO

-- luSubmittedAs codes used
MERGE luSubmittedAs AS tgt
USING (VALUES
    (1, 'Wet Tissue',       1),
    (3, 'Stained Section',  1)
) AS src (Code, Description, IsActive)
ON tgt.Code = src.Code
WHEN NOT MATCHED THEN
    INSERT (Code, Description, IsActive)
    VALUES (src.Code, src.Description, src.IsActive);
GO

-- ===========================================================================
-- 2. Users referenced by Batch.SubmittedBy and QCNotes.CreatedBy
-- ===========================================================================

SET IDENTITY_INSERT [User] ON;
MERGE [User] AS tgt
USING (VALUES
    (6,   'm126110', 'Pete Bellerby',             3, 5, 1),
    (50,  'dh000082','Daniel Hicks',              3, 5, 1),
    (71,  'm161048', 'Mark Chambers',             1, 3, 0),
    (140, 'm169809', 'Tony Strickland',           3, 5, 1),
    (240, 'ns000060','Narasimha Sanagaram',       3, 3, 0),
    (243, 'sd000106','Silambarasan Duraiswamy',   3, 3, 1)
) AS src (ID, NTLogin, Name, UserGroup, UserArea, Active)
ON tgt.ID = src.ID
WHEN NOT MATCHED THEN
    INSERT (ID, NTLogin, Name, UserGroup, UserArea, Active)
    VALUES (src.ID, src.NTLogin, src.Name, src.UserGroup, src.UserArea, src.Active);
SET IDENTITY_INSERT [User] OFF;
GO

-- ===========================================================================
-- 3. Batches
--    29401, 29402, 29404  — used for HistologyReport + SubmissionNotes PDFs
--    24351, 29388, 29389  — linked via BlockAntibodies to QCNotes 1909/1910/1911
-- ===========================================================================

SET IDENTITY_INSERT Batch ON;
MERGE Batch AS tgt
USING (VALUES
    -- HistologyReport / SubmissionNotes test batches (status 6 = In Progress)
    (29401, 310, 71, '12', '2026-02-05', 0, 0, 6, '2026-03-11', '2026-03-11', 243, 243, 1, 0, NULL, NULL),
    (29402, 117, 72, '46', '2026-03-10', 0, 0, 6,  NULL,         NULL,        240, 240, 3, 1, NULL, NULL),
    (29404, 310, 71, '12', '2026-03-16', 0, 0, 6,  NULL,         NULL,        243, 243, 1, 0, NULL, NULL),
    -- QCNote source batches (status 4/6 as per live data)
    (24351, 300, 46, '28', '2010-01-01', 1, 0, 6,  NULL,         NULL,        140, 140, 5, 0, NULL, NULL),
    (29388, 464,147, '1',  '2023-01-09', 1, 0, 4,  NULL,         NULL,        50,  50,  5, 0, NULL, NULL),
    (29389, 440,147, '25', '2018-03-16', 1, 0, 4,  NULL,         NULL,        6,   6,   5, 0, NULL, NULL)
) AS src (ID, ProjectContractCode, ContactName, Species, BatchDate,
          BatchType, SafeToHandle, BatchStatus,
          DateReceived, DateCompleted,
          OtherSubmittedBy, SubmittedBy, SubmittedArea, Cassetted,
          Fixation, Comments)
ON tgt.ID = src.ID
WHEN NOT MATCHED THEN
    INSERT (ID, ProjectContractCode, ContactName, Species, BatchDate,
            BatchType, SafeToHandle, BatchStatus,
            DateReceived, DateCompleted,
            OtherSubmittedBy, SubmittedBy, SubmittedArea, Cassetted,
            Fixation, Comments)
    VALUES (src.ID, src.ProjectContractCode, src.ContactName, src.Species,
            src.BatchDate, src.BatchType, src.SafeToHandle, src.BatchStatus,
            src.DateReceived, src.DateCompleted,
            src.OtherSubmittedBy, src.SubmittedBy, src.SubmittedArea,
            src.Cassetted, src.Fixation, src.Comments);
SET IDENTITY_INSERT Batch OFF;
GO

-- ===========================================================================
-- 4. BatchHistology — histology codes per batch
-- ===========================================================================

SET IDENTITY_INSERT BatchHistology ON;
MERGE BatchHistology AS tgt
USING (VALUES
    (36904, 29401, '2'),
    (36905, 29402, '2'),
    (36907, 29404, '2')
) AS src (ID, BatchID, Code)
ON tgt.ID = src.ID
WHEN NOT MATCHED THEN INSERT (ID, BatchID, Code) VALUES (src.ID, src.BatchID, src.Code);
SET IDENTITY_INSERT BatchHistology OFF;
GO

-- ===========================================================================
-- 5. BatchSubmittedAs — submitted-as codes per batch
-- ===========================================================================

SET IDENTITY_INSERT BatchSubmittedAs ON;
MERGE BatchSubmittedAs AS tgt
USING (VALUES
    (29361, 29401, 1),
    (29362, 29402, 3),
    (29364, 29404, 1)
) AS src (ID, BatchID, Code)
ON tgt.ID = src.ID
WHEN NOT MATCHED THEN INSERT (ID, BatchID, Code) VALUES (src.ID, src.BatchID, src.Code);
SET IDENTITY_INSERT BatchSubmittedAs OFF;
GO

-- ===========================================================================
-- 6. Animal + BatchSubmission + BatchTissues
--    These provide the per-submission row data for HistologyReport /
--    SubmissionNotes reports on batch 29401.
-- ===========================================================================

SET IDENTITY_INSERT Animal ON;
MERGE Animal AS tgt
USING (VALUES
    (101534, 'PD0573/93', '26/10026', '02', 0, '2026-02-06')
) AS src (ID, SenderRef, HistologyRef, NextBlockRef, OnHold, PMDate)
ON tgt.ID = src.ID
WHEN NOT MATCHED THEN
    INSERT (ID, SenderRef, HistologyRef, NextBlockRef, OnHold, PMDate)
    VALUES (src.ID, src.SenderRef, src.HistologyRef, src.NextBlockRef, src.OnHold, src.PMDate);
SET IDENTITY_INSERT Animal OFF;
GO

SET IDENTITY_INSERT BatchSubmission ON;
MERGE BatchSubmission AS tgt
USING (VALUES
    (65907, 29401, 101534, 0)
) AS src (ID, BatchID, AnimalID, [Order])
ON tgt.ID = src.ID
WHEN NOT MATCHED THEN
    INSERT (ID, BatchID, AnimalID, [Order])
    VALUES (src.ID, src.BatchID, src.AnimalID, src.[Order]);
SET IDENTITY_INSERT BatchSubmission OFF;
GO

SET IDENTITY_INSERT BatchTissues ON;
MERGE BatchTissues AS tgt
USING (VALUES
    (136432, 65907, 'ABCESS', 1, '10', '2026-03-11', 'Test')
) AS src (ID, BatchSubmissionID, TissueCode, NoPieces, ArchiveLocation, ArchivedDate, Comment)
ON tgt.ID = src.ID
WHEN NOT MATCHED THEN
    INSERT (ID, BatchSubmissionID, TissueCode, NoPieces, ArchiveLocation, ArchivedDate, Comment)
    VALUES (src.ID, src.BatchSubmissionID, src.TissueCode, src.NoPieces,
            src.ArchiveLocation, src.ArchivedDate, src.Comment);
SET IDENTITY_INSERT BatchTissues OFF;
GO

-- ===========================================================================
-- 7. QCNotes
-- ===========================================================================

SET IDENTITY_INSERT QCNotes ON;
MERGE QCNotes AS tgt
USING (VALUES
    (1909, 'The list below are recommendations. Please advise on whether to proceed or not.', 140, '2017-01-18'),
    (1910, 'Sender Ref            Histo Ref             Block Ref      Test',                   6, '2018-03-16'),
    (1911, '',                                                                                  50, '2023-01-09')
) AS src (ID, QCText, CreatedBy, DateCreated)
ON tgt.ID = src.ID
WHEN NOT MATCHED THEN
    INSERT (ID, QCText, CreatedBy, DateCreated)
    VALUES (src.ID, src.QCText, src.CreatedBy, src.DateCreated);
SET IDENTITY_INSERT QCNotes OFF;
GO

-- ===========================================================================
-- 8. BlockAntibodies — these rows link QCNotes to a batch/block context
--    via GetBatchQCNotes SP (which joins through BlockAntibodies → Batch).
--    BlockID values are synthetic references (no FK constraint exists).
-- ===========================================================================

SET IDENTITY_INSERT BlockAntibodies ON;
MERGE BlockAntibodies AS tgt
USING (VALUES
    -- QCNote 1909: batch 24351
    (340905, 297466, '2', '1', 'Sec.det.', 1, 1909,  NULL,         NULL,        '140', '140', 0, NULL, 'Rpt.',  NULL, NULL,  1, NULL,  NULL),
    -- QCNote 1911: batch 29388
    (365857, 317137, '2', '1', 'Bn. sp.', 1, 1911,  '123', '2023-01-09', '50',  '50',  1, 'Liam Evans', NULL, NULL,  NULL,  1, 'poor block, lot of material', NULL),
    -- QCNote 1910: batch 29389
    (365859, 317139, '2', '1', 'F',       1, 1910, 'FDAFDAFA','2018-03-16', '6', '6',  1, 'PB', 'R.mnt.', '9', '2018-03-16', 1, 'Rubbish;.', 'Test made up.')
) AS src (ID, BlockID, Code, Result, QCCode, QCNote, QCNoteRef,
          StainRef, DispatchedDate, DispatchedBy, EnteredBy,
          Dispatched, DispatchedTo, RemedialAction,
          ArchiveLocation, ArchivedDate, NumberOfSlides, Comment, ArchiveComment)
ON tgt.ID = src.ID
WHEN NOT MATCHED THEN
    INSERT (ID, BlockID, Code, Result, QCCode, QCNote, QCNoteRef,
            StainRef, DispatchedDate, DispatchedBy, EnteredBy,
            Dispatched, DispatchedTo, RemedialAction,
            ArchiveLocation, ArchivedDate, NumberOfSlides, Comment, ArchiveComment)
    VALUES (src.ID, src.BlockID, src.Code, src.Result, src.QCCode, src.QCNote, src.QCNoteRef,
            src.StainRef, src.DispatchedDate, src.DispatchedBy, src.EnteredBy,
            src.Dispatched, src.DispatchedTo, src.RemedialAction,
            src.ArchiveLocation, src.ArchivedDate, src.NumberOfSlides,
            src.Comment, src.ArchiveComment);
SET IDENTITY_INSERT BlockAntibodies OFF;
GO

-- ===========================================================================
-- 9. Grant permissions (idempotent)
-- ===========================================================================

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'HistologyUser')
BEGIN
    GRANT EXECUTE ON dbo.GetAllBatches TO HistologyUser;
END
GO

-- ===========================================================================
-- Verification
-- ===========================================================================

PRINT '=== Verification ===';
PRINT '';

DECLARE @batchCount INT = (SELECT COUNT(*) FROM Batch WHERE ID IN (29401, 29402, 29404));
PRINT 'HistologyReport/SubmissionNotes batches: ' + CAST(@batchCount AS varchar) + '/3 (expect 3)';

DECLARE @qcCount INT = (SELECT COUNT(*) FROM QCNotes WHERE ID IN (1909, 1910, 1911));
PRINT 'QCNote records: ' + CAST(@qcCount AS varchar) + '/3 (expect 3)';

DECLARE @blockCount INT = (SELECT COUNT(*) FROM BlockAntibodies WHERE QCNoteRef IN (1909, 1910, 1911));
PRINT 'BlockAntibodies for QCNotes: ' + CAST(@blockCount AS varchar) + '/3 (expect 3)';

PRINT '';
PRINT '=== Test PDF reports via these URLs ===';
PRINT 'HistologyReport : /Reports/HistologyReport        (select batch 29401 first)';
PRINT 'SubmissionNotes : /Reports/SubmissionNotes        (select batch 29401 first)';
PRINT 'QCNote          : /Reports/QCNote?qcNoteRef=1911';
PRINT 'QCNote          : /Reports/QCNote?qcNoteRef=1910';
PRINT 'QCNote          : /Reports/QCNote?qcNoteRef=1909';
GO
