/****** Object:  StoredProcedure [dbo].[GetAllUnusedHistologyRefs] ******/
-- ============================================================================
-- dbo.GetAllUnusedHistologyRefs
-- ============================================================================
-- Unused pre-booked histology refs with both columns aliased to match the
-- HistologyRef model (GetUnusedHistologyRefs returns neither Ref nor SenderRef).
-- ============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('[dbo].[GetAllUnusedHistologyRefs]', 'P') IS NOT NULL
        DROP PROCEDURE [dbo].[GetAllUnusedHistologyRefs];
GO

CREATE PROCEDURE [dbo].[GetAllUnusedHistologyRefs]
AS
BEGIN
        SET NOCOUNT ON;

        SELECT HistologyRef AS Ref, SenderRef
          FROM dbo.UnUsedHistologyRefs
         WHERE Used = 0
         ORDER BY HistologyRef;
END
GO

PRINT 'Deployed stored procedure dbo.GetAllUnusedHistologyRefs';
GO
