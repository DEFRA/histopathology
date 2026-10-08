/****** Object:  StoredProcedure [dbo].[AddtlkpSpecies] ******/
/****** ONE-TIME DEPLOYMENT SCRIPT — Safe to re-run via pipeline ******/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Idempotent: DROP + CREATE ensures no conflicts on re-deployment
IF OBJECT_ID('[dbo].[AddtlkpSpecies]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[AddtlkpSpecies];
GO

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
