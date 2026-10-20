/****** Object:  StoredProcedure [dbo].[GetPooledHistologyRef] ******/
-- ============================================================================
-- dbo.GetPooledHistologyRef
-- ============================================================================
-- Lowest unused pooled ref for a year prefix and number range. UnUsedHistologyRefs
-- has no type column, so the type is the range of the last 5 digits; TRY_CAST
-- keeps non-numeric pool rows from failing the statement.
-- UPDLOCK holds the row for the CALLER's transaction (a plain read-committed
-- SELECT would release it at once and let a concurrent copy pick the same ref);
-- READPAST sends concurrent copies to the next free row.
-- Must be called inside the caller's transaction.
-- ============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('[dbo].[GetPooledHistologyRef]', 'P') IS NOT NULL
        DROP PROCEDURE [dbo].[GetPooledHistologyRef];
GO

CREATE PROCEDURE [dbo].[GetPooledHistologyRef]
        @YearPrefix varchar(3),
        @Lower int,
        @Upper int
AS
BEGIN
        SET NOCOUNT ON;

        SELECT TOP (1) HistologyRef
          FROM dbo.UnUsedHistologyRefs WITH (UPDLOCK, ROWLOCK, READPAST)
         WHERE Used = 0
           AND LEFT(RTRIM(HistologyRef), 3) = @YearPrefix
           AND TRY_CAST(RIGHT(RTRIM(HistologyRef), 5) AS INT) >= @Lower
           AND TRY_CAST(RIGHT(RTRIM(HistologyRef), 5) AS INT) <  @Upper
         ORDER BY TRY_CAST(RIGHT(RTRIM(HistologyRef), 5) AS INT);
END
GO

PRINT 'Deployed stored procedure dbo.GetPooledHistologyRef';
GO
