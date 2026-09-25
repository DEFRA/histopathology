/****** Object:  StoredProcedure [dbo].[GetAnimalBlockArchiveInformation] ******/
/****** ONE-TIME DEPLOYMENT SCRIPT — Safe to re-run via pipeline ******/

-- Aliases [BatchBlock].[ID] as BlockID (was a second, unaliased "ID" column, identical
-- name to [Batch].[ID]) — a result set with two same-named columns can't be typed-mapped
-- by Dapper: QueryAsync<BlockArchiveInfo> sets the ID property once per matching column,
-- so the LAST "ID" column silently wins, overwriting the intended Batch.ID (submission
-- number) with BatchBlock.ID (the block's own row id). See Histo.Histology.Models.BlockArchiveInfo.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Idempotent: DROP + CREATE ensures no conflicts on re-deployment
IF OBJECT_ID('[dbo].[GetAnimalBlockArchiveInformation]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[GetAnimalBlockArchiveInformation];
GO

CREATE PROCEDURE [dbo].[GetAnimalBlockArchiveInformation]
	@SenderRef varchar(20),
	@HistologyRef varchar(20),
	@BlockRef varchar(10),
	@ArchiveLocation varchar(10)
AS

IF @HistologyRef IS NULL
BEGIN
	SELECT
		[Batch].[ID],
		[BatchBlock].[BlockRef],
		[luArchiveLocation].[Description] AS ArchiveLocation,
		CONVERT(varchar(30),[BatchBlock].[ArchivedDate], 103) AS ArchivedDate,
		[BatchBlock].[ArchiveComment],
		[luTissueType].[Description] AS TissueDescription,
		[BlockTissues].[NoPieces],
		[BatchBlock].[ID] AS BlockID
	FROM
		[Batch] INNER JOIN
		[BatchBlock] ON [Batch].[ID] = [BatchBlock].[BatchID] INNER JOIN
		[Animal] ON [BatchBlock].[AnimalID] = [Animal].[ID] INNER JOIN
		[BlockTissues] ON [BatchBlock].[ID] = [BlockTissues].[BlockID] INNER JOIN
		[luTissueType] ON [BlockTissues].[TissueCode] = [luTissueType].[Code] LEFT JOIN
		[luArchiveLocation] ON [BatchBlock].[ArchiveLocation] = [luArchiveLocation].[Code]
	WHERE
		[Animal].[SenderRef] = @SenderRef
		AND (@BlockRef = [BatchBlock].[BlockRef] OR @BlockRef  IS NULL)
		AND (@ArchiveLocation = [BatchBlock].[ArchiveLocation] OR @ArchiveLocation IS NULL)
END
ELSE
	SELECT
		[Batch].[ID],
		[BatchBlock].[BlockRef],
		[luArchiveLocation].[Description] AS ArchiveLocation,
		CONVERT(varchar(30),[BatchBlock].[ArchivedDate], 103) AS ArchivedDate,
		[BatchBlock].[ArchiveComment],
		[luTissueType].[Description] AS TissueDescription,
		[BlockTissues].[NoPieces],
		[BatchBlock].[ID] AS BlockID
	FROM
		[Batch] INNER JOIN
		[BatchBlock] ON [Batch].[ID] = [BatchBlock].[BatchID] INNER JOIN
		[Animal] ON [BatchBlock].[AnimalID] = [Animal].[ID] INNER JOIN
		[BlockTissues] ON [BatchBlock].[ID] = [BlockTissues].[BlockID] INNER JOIN
		[luTissueType] ON [BlockTissues].[TissueCode] = [luTissueType].[Code] LEFT JOIN
		[luArchiveLocation] ON [BatchBlock].[ArchiveLocation] = [luArchiveLocation].[Code]
	WHERE
		[Animal].[HistologyRef] = @HistologyRef AND (@BlockRef = [BatchBlock].[BlockRef] OR @BlockRef  IS NULL)
		AND (@ArchiveLocation = [BatchBlock].[ArchiveLocation] OR @ArchiveLocation IS NULL)
GO
