/****** Object:  StoredProcedure [dbo].[GetOrCreateSubmissionQCNote] ******/
-- ============================================================================
-- dbo.GetOrCreateSubmissionQCNote
-- ============================================================================
-- A submission has ONE QC note. Finds it (the latest QCNoteRef on any of the
-- batch's histology-worklist / antibody / stain tests) or, when there is none,
-- creates it and links @TestID to it. No explicit locks (legacy uses none):
-- the link is optimistic, exactly like the Edit* procedures - it only happens
-- if the test's RowStamp still equals @RowStamp (the stamp the caller read it
-- with). On a mismatch the note just created is rolled back.
--
-- RETURN: 0 existing note found (@QCNoteRef set)
--         1 note created and test linked (@QCNoteRef + @LinkedRowStamp set;
--           use the new stamp for the caller's next save of that test)
--         2 RowStamp mismatch - nothing created
-- @TestType: 'Histology' | 'Antibodies' | 'Stain'
-- ============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('[dbo].[GetOrCreateSubmissionQCNote]', 'P') IS NOT NULL
        DROP PROCEDURE [dbo].[GetOrCreateSubmissionQCNote];
GO

CREATE PROCEDURE [dbo].[GetOrCreateSubmissionQCNote]
        @BatchID int,
        @TestID int,
        @TestType varchar(20),
        @RowStamp binary(8),
        @UserID int,
        @DateCreated datetime,
        @QCNoteRef int OUTPUT,
        @LinkedRowStamp binary(8) OUTPUT
AS
BEGIN
        SET NOCOUNT ON;
        SET XACT_ABORT ON;

        SET @QCNoteRef = NULL;
        SET @LinkedRowStamp = NULL;

        IF @TestType NOT IN ('Histology', 'Antibodies', 'Stain')
                THROW 50000, 'Unknown test type.', 1;

        DECLARE @stamp TABLE (RowStamp binary(8));

        BEGIN TRY
                BEGIN TRANSACTION;

                SELECT @QCNoteRef = MAX(r)
                  FROM (
                        SELECT h.QCNoteRef AS r
                          FROM dbo.BlockHistology h
                          JOIN dbo.BatchBlock b ON b.ID = h.BlockID
                         WHERE b.BatchID = @BatchID AND RTRIM(h.Code) IN ('1', '2', '5', '7')
                        UNION ALL
                        SELECT a.QCNoteRef
                          FROM dbo.BlockAntibodies a
                          JOIN dbo.BatchBlock b ON b.ID = a.BlockID
                         WHERE b.BatchID = @BatchID
                        UNION ALL
                        SELECT s.QCNoteRef
                          FROM dbo.BlockStain s
                          JOIN dbo.BatchBlock b ON b.ID = s.BlockID
                         WHERE b.BatchID = @BatchID
                  ) x
                 WHERE r > 0;

                IF @QCNoteRef IS NOT NULL
                BEGIN
                        COMMIT TRANSACTION;
                        RETURN 0;
                END

                EXEC dbo.AddQCNote @CreatedBy = @UserID, @DateCreated = @DateCreated, @NewID = @QCNoteRef OUTPUT;

                IF @TestType = 'Histology'
                        UPDATE dbo.BlockHistology SET QCNote = 1, QCNoteRef = @QCNoteRef
                        OUTPUT inserted.RowStamp INTO @stamp (RowStamp) WHERE ID = @TestID AND RowStamp = @RowStamp;
                ELSE IF @TestType = 'Antibodies'
                        UPDATE dbo.BlockAntibodies SET QCNote = 1, QCNoteRef = @QCNoteRef
                        OUTPUT inserted.RowStamp INTO @stamp (RowStamp) WHERE ID = @TestID AND RowStamp = @RowStamp;
                ELSE
                        UPDATE dbo.BlockStain SET QCNote = 1, QCNoteRef = @QCNoteRef
                        OUTPUT inserted.RowStamp INTO @stamp (RowStamp) WHERE ID = @TestID AND RowStamp = @RowStamp;

                IF NOT EXISTS (SELECT 1 FROM @stamp)
                BEGIN
                        ROLLBACK TRANSACTION;
                        SET @QCNoteRef = NULL;
                        RETURN 2;
                END

                SELECT @LinkedRowStamp = RowStamp FROM @stamp;

                COMMIT TRANSACTION;
                RETURN 1;
        END TRY
        BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
        END CATCH
END
GO

PRINT 'Deployed stored procedure dbo.GetOrCreateSubmissionQCNote';
GO
