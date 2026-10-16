/****** Object:  StoredProcedure [dbo].[GetOrCreateSubmissionQCNote] ******/
-- ============================================================================
-- dbo.GetOrCreateSubmissionQCNote
-- ============================================================================
-- A submission has ONE QC note. Finds it (the latest QCNoteRef on any of the
-- batch's histology-worklist / antibody / stain tests) or, when there is none,
-- creates it and links @TestID to it - all under an application lock for the
-- batch, so two concurrent saves can't each create a note.
--
-- Linking only happens if the test's RowStamp still equals @RowStamp (the stamp
-- the caller read it with). A mismatch creates nothing. The row is locked
-- before the note is created, so the link cannot fail afterwards.
--
-- RETURN: 0 existing note found (@QCNoteRef set)
--         1 note created and test linked (@QCNoteRef + @LinkedRowStamp set;
--           use the new stamp for the caller's next save of that test)
--         2 RowStamp mismatch - nothing created
--         3 timed out waiting for the batch lock
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

        DECLARE @lock int, @matched bit = 0;
        DECLARE @resource nvarchar(255) = N'Histo:QCNote:Batch:' + CONVERT(nvarchar(20), @BatchID);
        DECLARE @stamp TABLE (RowStamp binary(8));

        BEGIN TRY
                BEGIN TRANSACTION;

                EXEC @lock = sp_getapplock
                        @Resource = @resource, @LockMode = 'Exclusive',
                        @LockOwner = 'Transaction', @LockTimeout = 15000;

                IF @lock < 0
                BEGIN
                        COMMIT TRANSACTION;
                        RETURN 3;
                END

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

                IF @TestType = 'Histology'
                        SELECT @matched = 1 FROM dbo.BlockHistology WITH (UPDLOCK, ROWLOCK) WHERE ID = @TestID AND RowStamp = @RowStamp;
                ELSE IF @TestType = 'Antibodies'
                        SELECT @matched = 1 FROM dbo.BlockAntibodies WITH (UPDLOCK, ROWLOCK) WHERE ID = @TestID AND RowStamp = @RowStamp;
                ELSE
                        SELECT @matched = 1 FROM dbo.BlockStain WITH (UPDLOCK, ROWLOCK) WHERE ID = @TestID AND RowStamp = @RowStamp;

                IF @matched = 0
                BEGIN
                        COMMIT TRANSACTION;
                        RETURN 2;
                END

                EXEC dbo.AddQCNote @CreatedBy = @UserID, @DateCreated = @DateCreated, @NewID = @QCNoteRef OUTPUT;

                IF @TestType = 'Histology'
                        UPDATE dbo.BlockHistology SET QCNote = 1, QCNoteRef = @QCNoteRef
                        OUTPUT inserted.RowStamp INTO @stamp (RowStamp) WHERE ID = @TestID;
                ELSE IF @TestType = 'Antibodies'
                        UPDATE dbo.BlockAntibodies SET QCNote = 1, QCNoteRef = @QCNoteRef
                        OUTPUT inserted.RowStamp INTO @stamp (RowStamp) WHERE ID = @TestID;
                ELSE
                        UPDATE dbo.BlockStain SET QCNote = 1, QCNoteRef = @QCNoteRef
                        OUTPUT inserted.RowStamp INTO @stamp (RowStamp) WHERE ID = @TestID;

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
