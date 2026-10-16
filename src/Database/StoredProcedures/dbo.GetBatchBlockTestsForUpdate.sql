/****** Object:  StoredProcedure [dbo].[GetBatchBlockTestsForUpdate] ******/
-- ============================================================================
-- dbo.GetBatchBlockTestsForUpdate
-- ============================================================================
-- Returns, for every block of @BatchID that holds tests (Status 1 Used or
-- 3 PreBookedUsed - unclaimed pre-booked placeholders excluded), the current
-- Histology / Antibodies / Stain codes, READING THEM WITH UPDLOCK + HOLDLOCK.
-- Must run inside the caller's transaction: the locks last until it ends, so
-- a concurrent per-block save waits instead of reading the same "current" rows
-- and then deleting what the caller adds.
--
-- Result sets: 1) block IDs  2) BlockHistology  3) BlockAntibodies  4) BlockStain
--              (2-4: BlockID, Code)
-- ============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('[dbo].[GetBatchBlockTestsForUpdate]', 'P') IS NOT NULL
        DROP PROCEDURE [dbo].[GetBatchBlockTestsForUpdate];
GO

CREATE PROCEDURE [dbo].[GetBatchBlockTestsForUpdate]
        @BatchID int
AS
BEGIN
        SET NOCOUNT ON;

        IF @@TRANCOUNT = 0
                THROW 50000, 'GetBatchBlockTestsForUpdate must be called inside a transaction.', 1;

        SELECT ID
          FROM dbo.BatchBlock
         WHERE BatchID = @BatchID AND Status IN (1, 3);

        SELECT BlockID, RTRIM(Code) AS Code
          FROM dbo.BlockHistology WITH (UPDLOCK, HOLDLOCK)
         WHERE BlockID IN (SELECT ID FROM dbo.BatchBlock WHERE BatchID = @BatchID AND Status IN (1, 3));

        SELECT BlockID, RTRIM(Code) AS Code
          FROM dbo.BlockAntibodies WITH (UPDLOCK, HOLDLOCK)
         WHERE BlockID IN (SELECT ID FROM dbo.BatchBlock WHERE BatchID = @BatchID AND Status IN (1, 3));

        SELECT BlockID, RTRIM(Code) AS Code
          FROM dbo.BlockStain WITH (UPDLOCK, HOLDLOCK)
         WHERE BlockID IN (SELECT ID FROM dbo.BatchBlock WHERE BatchID = @BatchID AND Status IN (1, 3));
END
GO

PRINT 'Deployed stored procedure dbo.GetBatchBlockTestsForUpdate';
GO
