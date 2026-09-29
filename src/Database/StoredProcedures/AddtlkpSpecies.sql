CREATE PROCEDURE dbo.AddtlkpSpecies
	@SpeciesID integer,
	@Species varchar(20),
	@CommonName varchar(20)
AS
	DECLARE
		@ErrorCode int

	INSERT INTO dbo.tlkpSpecies
		([SpeciesID], [Species], [CommonName])
	VALUES
		(@SpeciesID, @Species, @CommonName)

	SET @ErrorCode=@@Error

	IF @ErrorCode = 0 BEGIN
		RETURN 0
	END ELSE BEGIN
		RETURN @ErrorCode
	END
