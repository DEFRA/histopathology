/****** Object:  StoredProcedure [dbo].[GetBatchBlockTests] ******/
-- ============================================================================
-- dbo.GetBatchBlockTests
-- ============================================================================
-- Returns, for every block of @BatchID that holds tests (Status 1 Used or
-- 3 PreBookedUsed - unclaimed pre-booked placeholders excluded), the current
-- Histology / Antibodies / Stain codes. Plain reads, no locks held: the test
-- tables have no BlockID index, so holding range locks escalates to table locks.
--
-- Result sets: 1) block IDs  2) BlockHistology  3) BlockAntibodies  4) BlockStain
--              (2-4: BlockID, Code)
-- ============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('[dbo].[GetBatchBlockTests]', 'P') IS NOT NULL
        DROP PROCEDURE [dbo].[GetBatchBlockTests];
GO

CREATE PROCEDURE [dbo].[GetBatchBlockTests]
        @BatchID int
AS
BEGIN
        SET NOCOUNT ON;

        SELECT ID
          FROM dbo.BatchBlock
         WHERE BatchID = @BatchID AND Status IN (1, 3);

        SELECT BlockID, RTRIM(Code) AS Code
          FROM dbo.BlockHistology
         WHERE BlockID IN (SELECT ID FROM dbo.BatchBlock WHERE BatchID = @BatchID AND Status IN (1, 3));

        SELECT BlockID, RTRIM(Code) AS Code
          FROM dbo.BlockAntibodies
         WHERE BlockID IN (SELECT ID FROM dbo.BatchBlock WHERE BatchID = @BatchID AND Status IN (1, 3));

        SELECT BlockID, RTRIM(Code) AS Code
          FROM dbo.BlockStain
         WHERE BlockID IN (SELECT ID FROM dbo.BatchBlock WHERE BatchID = @BatchID AND Status IN (1, 3));
END
GO

PRINT 'Deployed stored procedure dbo.GetBatchBlockTests';
GO
