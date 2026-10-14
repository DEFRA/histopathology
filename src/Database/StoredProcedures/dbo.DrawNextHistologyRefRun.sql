/****** Object:  StoredProcedure [dbo].[DrawNextHistologyRefRun] ******/
-- ============================================================================
-- dbo.DrawNextHistologyRefRun
-- ============================================================================
-- Reserves @Count consecutive numbers from a type's counter in ONE UPDATE (the
-- row lock makes the run atomic) and returns the first number as a single
-- 'Start' row. No row is returned when no clean run could be reserved.
--
--   * The counter is only touched when it is numeric, >= @Lower and the run
--     ends at or below @Upper (exclusive bound of the type's range), so an
--     out-of-range run never moves it. 99999 is the varchar(5) ceiling.
--   * A run containing a number a sample already holds ('<@RefPrefix>NNNNN')
--     is discarded whole and a fresh run reserved, up to @MaxAttempts times,
--     so the result is always consecutive. Discarded runs stay consumed.
--   * @RefPrefix is the app's current-year prefix, e.g. '26/'.
-- ============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('[dbo].[DrawNextHistologyRefRun]', 'P') IS NOT NULL
        DROP PROCEDURE [dbo].[DrawNextHistologyRefRun];
GO

CREATE PROCEDURE [dbo].[DrawNextHistologyRefRun]
        @Type int,
        @Count int,
        @Lower int,
        @Upper int,
        @RefPrefix varchar(3),
        @MaxAttempts int = 50
AS
BEGIN
        SET NOCOUNT ON;

        IF @Count <= 0 RETURN;

        DECLARE @Attempt int = 0;
        DECLARE @Start int;
        DECLARE @Drawn TABLE (Start int);

        WHILE @Attempt < @MaxAttempts
        BEGIN
                SET @Attempt += 1;
                SET @Start = NULL;
                DELETE FROM @Drawn;

                UPDATE dbo.HistologyRef
                   SET NextHistologyRef = CONVERT(varchar(5), CONVERT(int, NextHistologyRef) + @Count)
                OUTPUT CONVERT(int, DELETED.NextHistologyRef) INTO @Drawn (Start)
                 WHERE [Type] = @Type
                   AND TRY_CONVERT(int, NextHistologyRef) >= @Lower
                   AND TRY_CONVERT(int, NextHistologyRef) + @Count <= @Upper
                   AND TRY_CONVERT(int, NextHistologyRef) + @Count <= 99999;

                SELECT @Start = Start FROM @Drawn;
                IF @Start IS NULL RETURN;

                IF NOT EXISTS (
                        SELECT 1
                          FROM dbo.Animal a
                         WHERE RTRIM(a.HistologyRef) IN (
                                SELECT @RefPrefix + RIGHT('00000' + CONVERT(varchar(5), @Start + r.n), 5)
                                  FROM (SELECT TOP (@Count) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS n
                                          FROM sys.all_objects) r))
                BEGIN
                        SELECT @Start AS Start;
                        RETURN;
                END
        END
END
GO

PRINT 'Deployed stored procedure dbo.DrawNextHistologyRefRun';
GO
