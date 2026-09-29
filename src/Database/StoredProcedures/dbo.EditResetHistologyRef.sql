/****** Object:  StoredProcedure [dbo].[EditResetHistologyRef] ******/
/****** ONE-TIME DEPLOYMENT SCRIPT — Safe to re-run via pipeline ******/
-- ============================================================================
-- dbo.EditResetHistologyRef
-- ============================================================================
-- Real production reset logic (captured from SSMS, script date 29/09/2026
-- 14:35:32). Resets each histology reference counter
-- (dbo.HistologyRef.NextHistologyRef) to its fixed per-type starting value,
-- but ONLY once per calendar year - guarded by comparing GETDATE() against
-- the most recent HistRefResetLog entry
-- (DATEDIFF(Year, @LastUpdate, @CurrentDate) > 0).
--
-- Before resetting, current values are snapshotted into HistologyRefBackup,
-- then each type is updated in its own statement inside an explicit
-- transaction, with a rowcount check + ROLLBACK after every statement. On
-- success, a new HistRefResetLog row is inserted recording @CurrentDate as
-- the reset date (this becomes @LastUpdate on the next run).
--
-- IMPORTANT: because of the year-gate, calling this procedure on a schedule
-- (e.g. every 15 minutes) is a deliberate no-op on every run except the
-- first run after the calendar year rolls over - this is expected
-- production behaviour, not a bug. To test locally without waiting for
-- 1 January, temporarily back-date the most recent HistRefResetLog row (or
-- insert a dummy earlier-year row) so the year-gate evaluates true, then
-- restore/re-seed HistRefResetLog afterwards.
-- ============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('[dbo].[EditResetHistologyRef]', 'P') IS NOT NULL
	DROP PROCEDURE [dbo].[EditResetHistologyRef];
GO

CREATE PROCEDURE [dbo].[EditResetHistologyRef] AS

DECLARE
	@LastUpdate datetime,
	@CurrentDate datetime,
	@ErrorCode int,
	@Rowcount int


	SET @CurrentDate = GETDATE()

	SELECT @LastUpdate = (SELECT TOP 1  HistRefResetDate FROM HistRefResetLog ORDER BY ResetID DESC)


	IF  DATEDIFF(Year,@LastUpdate,@CurrentDate)>0 BEGIN

		UPDATE HistologyRefBackup SET
			HistologyRefBackup.NextHistologyRef = HistologyRef.NextHistologyRef
		FROM
			HistologyRef
		WHERE
			HistologyRefBackup.Type = HistologyRef.Type

		BEGIN TRANSACTION

		UPDATE HistologyRef SET NextHistologyRef = 10000 WHERE Type = 1

		SET @RowCount = @@ROWCOUNT
		IF @RowCount <> 1 BEGIN
			ROLLBACK TRANSACTION
			RETURN 1
		END

		UPDATE HistologyRef SET NextHistologyRef = 20000 WHERE Type = 2

		SET @RowCount = @@ROWCOUNT
		IF @RowCount <> 1 BEGIN
			ROLLBACK TRANSACTION
			RETURN 1
		END

		UPDATE HistologyRef SET NextHistologyRef = 30000 WHERE Type = 3

		SET @RowCount = @@ROWCOUNT
		IF @RowCount <> 1 BEGIN
			ROLLBACK TRANSACTION
			RETURN 1
		END

		UPDATE HistologyRef SET NextHistologyRef = 40000 WHERE Type = 4

		SET @RowCount = @@ROWCOUNT
		IF @RowCount <> 1 BEGIN
			ROLLBACK TRANSACTION
			RETURN 1
		END

		UPDATE HistologyRef SET NextHistologyRef = 60000 WHERE Type = 5

		SET @RowCount = @@ROWCOUNT
		IF @RowCount <> 1 BEGIN
			ROLLBACK TRANSACTION
			RETURN 1
		END

		INSERT INTO HistRefResetLog
			(
				HistRefResetDate
			)
		VALUES
			(
				 @CurrentDate
			)

		SET @RowCount = @@ROWCOUNT
		IF @RowCount <> 1 BEGIN
			ROLLBACK TRANSACTION
			RETURN 1
		END

		SET @ErrorCode = @@ERROR

		IF @ErrorCode <> 0 BEGIN
			ROLLBACK TRANSACTION
			RETURN 1
		END

		COMMIT TRANSACTION

		RETURN 0

	END
GO
