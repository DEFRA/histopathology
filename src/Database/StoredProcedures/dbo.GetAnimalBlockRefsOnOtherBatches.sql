-- Object:  StoredProcedure [dbo].[GetAnimalBlockRefsOnOtherBatches]
-- ============================================================================
-- dbo.GetAnimalBlockRefsOnOtherBatches
-- ============================================================================
-- Block refs an animal already holds outside @BatchID. Animal is shared across
-- submissions, so these refs are unavailable to a new block. Rows with a NULL
-- BatchID are unclaimed pre-booked placeholders; they stay reserved.
-- ============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('[dbo].[GetAnimalBlockRefsOnOtherBatches]', 'P') IS NOT NULL
        DROP PROCEDURE [dbo].[GetAnimalBlockRefsOnOtherBatches];
GO

CREATE PROCEDURE [dbo].[GetAnimalBlockRefsOnOtherBatches]
        @AnimalID int,
        @BatchID int
AS
BEGIN
        SET NOCOUNT ON;

        SELECT RTRIM(BlockRef) AS BlockRef
          FROM dbo.BatchBlock
         WHERE AnimalID = @AnimalID
           AND (BatchID IS NULL OR BatchID <> @BatchID)
           AND BlockRef IS NOT NULL;
END
GO

PRINT 'Deployed stored procedure dbo.GetAnimalBlockRefsOnOtherBatches';
GO
