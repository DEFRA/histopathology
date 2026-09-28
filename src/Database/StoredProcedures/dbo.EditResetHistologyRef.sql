-- ============================================================================
-- dbo.EditResetHistologyRef
-- ============================================================================
-- Resets each histology reference counter (dbo.HistologyRef.NextHistologyRef)
-- to a timestamp-based value, so scheduled runs can be verified end-to-end
-- without waiting for the annual 1-January reset.
--
-- Idempotent behaviour: on every run, the existing prefix (the portion of
-- NextHistologyRef before its first '-') is preserved and only the trailing
-- timestamp suffix is replaced. If no '-' is present yet, the current value
-- is treated as the prefix and a suffix is appended.
--
-- TESTING NOTE: currently scheduled every 15 minutes (see .azure-devops/pipeline.yaml)
-- for verification purposes. Revert the pipeline schedule back to the annual
-- CRON (0 4 1 1 *) once testing is complete.
-- ============================================================================
CREATE OR ALTER PROCEDURE dbo.EditResetHistologyRef
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @Timestamp VARCHAR(20) = FORMAT(GETUTCDATE(), 'yyyyMMddHHmmss');

	UPDATE dbo.[HistologyRef]
	SET NextHistologyRef =
		CASE
			WHEN CHARINDEX('-', NextHistologyRef) > 0
				THEN LEFT(NextHistologyRef, CHARINDEX('-', NextHistologyRef) - 1) + '-' + @Timestamp
			ELSE NextHistologyRef + '-' + @Timestamp
		END;
END
GO
