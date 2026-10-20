/****** Object:  StoredProcedure [dbo].[GetAnimalBlockRefs] ******/
-- ============================================================================
-- dbo.GetAnimalBlockRefs
-- ============================================================================
-- Every block ref held by an animal across all submissions (trimmed).
-- ============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('[dbo].[GetAnimalBlockRefs]', 'P') IS NOT NULL
        DROP PROCEDURE [dbo].[GetAnimalBlockRefs];
GO

CREATE PROCEDURE [dbo].[GetAnimalBlockRefs]
        @AnimalID int
AS
BEGIN
        SET NOCOUNT ON;

        SELECT RTRIM(BlockRef) AS BlockRef
          FROM dbo.BatchBlock
         WHERE AnimalID = @AnimalID
           AND BlockRef IS NOT NULL;
END
GO

PRINT 'Deployed stored procedure dbo.GetAnimalBlockRefs';
GO
