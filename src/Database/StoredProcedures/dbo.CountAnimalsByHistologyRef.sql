/****** Object:  StoredProcedure [dbo].[CountAnimalsByHistologyRef] ******/
-- ============================================================================
-- dbo.CountAnimalsByHistologyRef
-- ============================================================================
-- Number of samples already holding the given full histology ref (e.g. '26/60001').
-- Used to skip a number drawn from the counter that a sample already has.
-- ============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('[dbo].[CountAnimalsByHistologyRef]', 'P') IS NOT NULL
        DROP PROCEDURE [dbo].[CountAnimalsByHistologyRef];
GO

CREATE PROCEDURE [dbo].[CountAnimalsByHistologyRef]
        @HistologyRef varchar(20)
AS
BEGIN
        SET NOCOUNT ON;

        SELECT COUNT(1) FROM dbo.Animal WHERE RTRIM(HistologyRef) = @HistologyRef;
END
GO

PRINT 'Deployed stored procedure dbo.CountAnimalsByHistologyRef';
GO
