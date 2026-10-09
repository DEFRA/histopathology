-- Object:  StoredProcedure [dbo].[GettlkpSpecies]
-- ONE-TIME DEPLOYMENT SCRIPT — Safe to re-run via pipeline

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Idempotent: DROP + CREATE ensures no conflicts on re-deployment
IF OBJECT_ID('[dbo].[GettlkpSpecies]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[GettlkpSpecies];
GO

CREATE PROCEDURE dbo.GettlkpSpecies
AS

SET NOCOUNT ON

SELECT
	[SpeciesID],
	[Species],
	[CommonName]
FROM
	tlkpSpecies
ORDER BY
	[Species]

SET NOCOUNT OFF

RETURN
