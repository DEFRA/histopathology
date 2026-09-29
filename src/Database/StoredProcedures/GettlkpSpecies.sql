CREATE PROCEDURE dbo.GettlkpSpecies
AS

DECLARE @ttblSpecies TABLE
(
	[SpeciesID] int,
	[Species] varchar(20),
	[CommonName] varchar(20)
)

SET NOCOUNT ON

INSERT INTO @ttblSpecies
(
	[SpeciesID],
	[Species],
	[CommonName]
)
SELECT
	[SpeciesID],
	[Species],
	[CommonName]
FROM
	tlkpSpecies
ORDER BY
	[Species]

SELECT
	[SpeciesID],
	[Species],
	[CommonName]
FROM
	@ttblSpecies
ORDER BY
	[Species]

SET NOCOUNT OFF

RETURN
