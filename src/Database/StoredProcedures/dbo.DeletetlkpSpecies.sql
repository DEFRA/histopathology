/****** Object:  StoredProcedure [dbo].[DeletetlkpSpecies] ******/
/****** ONE-TIME DEPLOYMENT SCRIPT — Safe to re-run via pipeline ******/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Idempotent: DROP + CREATE ensures no conflicts on re-deployment
IF OBJECT_ID('[dbo].[DeletetlkpSpecies]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[DeletetlkpSpecies];
GO

CREATE PROCEDURE DeletetlkpSpecies
	@SpeciesID integer
AS
	DECLARE
		@ErrorCode int,
		@RowsUpdated int

	DELETE FROM dbo.tlkpSpecies WHERE [SpeciesID] = @SpeciesID

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
