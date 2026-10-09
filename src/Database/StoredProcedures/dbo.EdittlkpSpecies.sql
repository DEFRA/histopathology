-- Object:  StoredProcedure [dbo].[EdittlkpSpecies]
-- ONE-TIME DEPLOYMENT SCRIPT — Safe to re-run via pipeline

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Idempotent: DROP + CREATE ensures no conflicts on re-deployment
IF OBJECT_ID('[dbo].[EdittlkpSpecies]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[EdittlkpSpecies];
GO

CREATE PROCEDURE EdittlkpSpecies
	@SpeciesID integer,
	@Species varchar(20),
	@CommonName varchar(20),
	@UserID integer
AS
	DECLARE
		@ErrorCode int,
		@RowsUpdated int,
		@columnName varchar(50),
		@columnValue varchar(500),
		@columnCount int,
		@oldColumnValue varchar(500),
		@oldColumnValue1 varchar(500)

	SET @oldColumnValue  = CONVERT(varchar(500), (SELECT [Species]    FROM tlkpSpecies WHERE SpeciesID = @SpeciesID))
	SET @oldColumnValue1 = CONVERT(varchar(500), (SELECT [CommonName] FROM tlkpSpecies WHERE SpeciesID = @SpeciesID))

	UPDATE dbo.tlkpSpecies SET
		[Species]    = @Species,
		[CommonName] = @CommonName
	WHERE
		[SpeciesID] = @SpeciesID

	SET @columnCount = 1

	WHILE @columnCount < 3
	BEGIN
		SET @columnName = CASE @columnCount
			WHEN 1 THEN 'Species'
			WHEN 2 THEN 'CommonName'
		END

		SET @columnValue = CASE @columnCount
			WHEN 1 THEN CONVERT(varchar(500), @Species)
			WHEN 2 THEN CONVERT(varchar(500), @CommonName)
		END

		SET @oldColumnValue = CASE @columnCount
			WHEN 1 THEN @oldColumnValue
			WHEN 2 THEN @oldColumnValue1
		END

		IF @oldColumnValue <> @columnValue AND NOT @oldColumnValue IS NULL BEGIN
			INSERT INTO AuditLog
				(ID, TableName, FieldName, LogDate, UserID, BeforeValue, AfterValue, Reason)
			VALUES
				(@SpeciesID, 'tlkpSpecies', @columnName, getDate(), @UserID, @oldColumnValue, @columnValue, 'EdittlkpSpecies')
		END

		SET @columnCount = @columnCount + 1
	END

	SELECT @ErrorCode = @@ERROR, @RowsUpdated = @@ROWCOUNT

	IF @ErrorCode = 0 BEGIN
		IF @RowsUpdated = 0 BEGIN
			RETURN -1
		END ELSE BEGIN
			RETURN 0
		END
	END ELSE BEGIN
		RETURN @ErrorCode
	END
