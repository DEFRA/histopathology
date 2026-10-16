/****** Object:  StoredProcedure [dbo].[GetBatchBlockTests] ******/
-- ============================================================================
-- dbo.GetBatchBlockTests
-- ============================================================================
-- Returns, for every block of @BatchID that holds tests, the current
-- Histology / Antibodies / Stain codes. Plain reads, no locks held: the test
-- tables have no BlockID index, so holding range locks escalates to table locks.
--
-- BatchBlock.Status is the BLOCK status (not luStatus, which is the submission
-- status): 0 Not used, 1 Used, 2 Pre-booked, 3 Pre-booked and used. It has no
-- lookup table. Unclaimed pre-booked placeholders (2) are excluded.
--
-- Result sets: 1) block IDs  2) BlockHistology  3) BlockAntibodies  4) BlockStain
--              (2-4: BlockID, Code)
-- ============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[GetBatchBlockTests]
        @BatchID int
AS
BEGIN
        SET NOCOUNT ON;

        DECLARE @Used int = 1, @PreBookedUsed int = 3;

        DECLARE @blocks TABLE (ID int PRIMARY KEY);
        INSERT INTO @blocks (ID)
        SELECT ID
          FROM dbo.BatchBlock
         WHERE BatchID = @BatchID AND Status IN (@Used, @PreBookedUsed);

        SELECT ID FROM @blocks;

        SELECT BlockID, RTRIM(Code) AS Code
          FROM dbo.BlockHistology
         WHERE BlockID IN (SELECT ID FROM @blocks);

        SELECT BlockID, RTRIM(Code) AS Code
          FROM dbo.BlockAntibodies
         WHERE BlockID IN (SELECT ID FROM @blocks);

        SELECT BlockID, RTRIM(Code) AS Code
          FROM dbo.BlockStain
         WHERE BlockID IN (SELECT ID FROM @blocks);
END
GO

PRINT 'Deployed stored procedure dbo.GetBatchBlockTests';
GO
