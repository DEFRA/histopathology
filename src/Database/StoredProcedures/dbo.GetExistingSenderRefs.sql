/****** Object:  StoredProcedure [dbo].[GetExistingSenderRefs] ******/
-- ============================================================================
-- dbo.GetExistingSenderRefs
-- ============================================================================
-- Of a comma-separated list of Sender Refs, returns those that already exist
-- on an Animal.
-- ============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('[dbo].[GetExistingSenderRefs]', 'P') IS NOT NULL
        DROP PROCEDURE [dbo].[GetExistingSenderRefs];
GO

CREATE PROCEDURE [dbo].[GetExistingSenderRefs]
        @SenderRefs nvarchar(max)
AS
BEGIN
        SET NOCOUNT ON;

        SELECT a.SenderRef
          FROM dbo.Animal a
         WHERE a.SenderRef IN (SELECT RTRIM(LTRIM(value)) FROM STRING_SPLIT(@SenderRefs, ','));
END
GO

PRINT 'Deployed stored procedure dbo.GetExistingSenderRefs';
GO
