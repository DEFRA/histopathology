-- Object:  StoredProcedure [dbo].[DeleteSampleFromBatch]
-- ============================================================================
-- dbo.DeleteSampleFromBatch
-- ============================================================================
-- Removes a sample's blocks (and, with @IncludeSubmission = 1, its submission
-- rows and tissues) from one batch. There are no FK constraints between these
-- tables, so every child table is cleared explicitly or its rows are orphaned.
-- BatchSubmission/BatchTissues are kept unless @IncludeSubmission = 1 so the
-- sample stays pickable in Add sample (legacy clsAnimal.RemoveSubmission).
-- Not transactional itself: the caller wraps it in its own transaction.
-- ============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('[dbo].[DeleteSampleFromBatch]', 'P') IS NOT NULL
        DROP PROCEDURE [dbo].[DeleteSampleFromBatch];
GO

CREATE PROCEDURE [dbo].[DeleteSampleFromBatch]
        @BatchID int,
        @AnimalID int,
        @IncludeSubmission bit,
        @PreBookedStatus int,
        @PreBookedUsedStatus int
AS
BEGIN
        SET NOCOUNT ON;

        -- TC (premium charge) codes hang off the block test rows by TestID and are not
        -- cascaded, so they must go before their parents or they are orphaned.
        DELETE tc FROM dbo.HistologyTCCodes    tc INNER JOIN dbo.BlockHistology  bh ON bh.ID = tc.TestID INNER JOIN dbo.BatchBlock b ON b.ID = bh.BlockID WHERE b.BatchID = @BatchID AND b.AnimalID = @AnimalID;
        DELETE tc FROM dbo.AntibodiesTCCodes   tc INNER JOIN dbo.BlockAntibodies ba ON ba.ID = tc.TestID INNER JOIN dbo.BatchBlock b ON b.ID = ba.BlockID WHERE b.BatchID = @BatchID AND b.AnimalID = @AnimalID;
        DELETE tc FROM dbo.SpecialStainTCCodes tc INNER JOIN dbo.BlockStain      bs ON bs.ID = tc.TestID INNER JOIN dbo.BatchBlock b ON b.ID = bs.BlockID WHERE b.BatchID = @BatchID AND b.AnimalID = @AnimalID;

        DELETE bt FROM dbo.BlockTissues    bt INNER JOIN dbo.BatchBlock b ON b.ID = bt.BlockID WHERE b.BatchID = @BatchID AND b.AnimalID = @AnimalID;
        DELETE bh FROM dbo.BlockHistology  bh INNER JOIN dbo.BatchBlock b ON b.ID = bh.BlockID WHERE b.BatchID = @BatchID AND b.AnimalID = @AnimalID;
        DELETE ba FROM dbo.BlockAntibodies ba INNER JOIN dbo.BatchBlock b ON b.ID = ba.BlockID WHERE b.BatchID = @BatchID AND b.AnimalID = @AnimalID;
        DELETE bs FROM dbo.BlockStain      bs INNER JOIN dbo.BatchBlock b ON b.ID = bs.BlockID WHERE b.BatchID = @BatchID AND b.AnimalID = @AnimalID;

        -- Pre-booked block refs are a finite booked resource: release them back to the
        -- pool (PreBookedUsed -> PreBooked, no batch) instead of destroying the booking.
        UPDATE dbo.BatchBlock
           SET Status = @PreBookedStatus, BatchID = NULL
         WHERE BatchID = @BatchID AND AnimalID = @AnimalID AND Status = @PreBookedUsedStatus;

        DELETE FROM dbo.BatchBlock WHERE BatchID = @BatchID AND AnimalID = @AnimalID;

        IF @IncludeSubmission = 1
        BEGIN
                DELETE t FROM dbo.BatchTissues t INNER JOIN dbo.BatchSubmission s ON s.ID = t.BatchSubmissionID WHERE s.BatchID = @BatchID AND s.AnimalID = @AnimalID;
                DELETE FROM dbo.BatchSubmission WHERE BatchID = @BatchID AND AnimalID = @AnimalID;
        END
END
GO

PRINT 'Deployed stored procedure dbo.DeleteSampleFromBatch';
GO
