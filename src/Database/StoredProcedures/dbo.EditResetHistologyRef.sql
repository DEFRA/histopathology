-- ============================================================================
-- dbo.EditResetHistologyRef
-- ============================================================================
-- Resets each histology reference counter (dbo.HistologyRef.NextHistologyRef,
-- confirmed varchar(5) per the real dbo.EditHistologyRef definition) to a
-- clock-derived value, so scheduled runs can be verified end-to-end without
-- waiting for the annual 1-January reset.
--
-- TESTING value: minutes-since-midnight (UTC), zero-padded to 5 digits
-- (e.g. '00555' = 09:15 UTC). Fits varchar(5) (range 00000-01439) and visibly
-- changes every minute, so a 15-minute schedule is easy to verify.
--
-- NOTE: RowStamp is a SQL Server `timestamp`/rowversion column, auto-updated
-- by the engine on any row change - it is never set explicitly, matching
-- dbo.EditHistologyRef's own usage (RowStamp only ever appears in a WHERE
-- clause there, never in a SET clause).
--
-- TESTING NOTE: currently scheduled every 15 minutes (see .azure-devops/pipeline.yaml)
-- for verification purposes. Once verified, replace the body below with the
-- real production reset rule (e.g. each type's own starting number) and
-- revert the pipeline schedule back to the annual CRON (0 4 1 1 *).
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.EditResetHistologyRef
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @MinutesSinceMidnight INT = DATEDIFF(MINUTE, CAST(GETUTCDATE() AS DATE), GETUTCDATE());
	DECLARE @TestValue VARCHAR(5) = RIGHT('0000' + CAST(@MinutesSinceMidnight AS VARCHAR(5)), 5);

	UPDATE dbo.[HistologyRef]
	SET NextHistologyRef = @TestValue;
END
GO
