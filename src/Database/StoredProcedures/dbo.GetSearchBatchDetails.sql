/****** Object:  StoredProcedure [dbo].[GetSearchBatchDetails]    Script Date: 08/10/2026 21:57:45 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE OR ALTER PROCEDURE [dbo].[GetSearchBatchDetails]
	@SubmittedBy integer = null,
	@ProjectContract varchar(50) = null,
	@ContactName varchar(50) = null,
	@Species varchar(10) = null,
	@SubmittedArea varchar(10)= null,
	@Fixation varchar(10) = null,
	@Status  integer = null,
	@SubmittedDateFrom datetime = null,
	@SubmittedDateTo datetime = null,
	@ReceivedDateFrom datetime = null,
	@ReceivedDateTo datetime = null,
	@Number integer = null,
	@HistologyRef varchar(20)= null,
	@SenderRef varchar(20)= null,
	@EnteredBy integer = null,
	@All integer = null
with recompile
AS
-- Performance rewrite (2026-10-08, UAT 504 timeout fix): UNION_BATCH is 1-to-many per Batch (one
-- row per tissue/block ref), so the previous INNER JOIN fanned out rows before DISTINCT collapsed
-- them again — cost grew with tissues-per-submission as data volume increased. Replaced with an
-- EXISTS semi-join (same "batch must have a matching UNION_BATCH row" semantics as the INNER JOIN,
-- zero result-set difference) so DISTINCT is no longer needed. Date-range and tlkpSpecies predicates
-- rewritten to be SARGable (previously wrapped the compared/joined column in ISNULL/CONVERT, which
-- defeats index usage even when a real filter value is supplied).
If @All = 0 BEGIN
		SELECT TOP 200
			[Batch].[ID],
			[luProjects].[Description] AS ProjectDescription, 
			[luContacts].[Description] AS ContactDescription,
			CONVERT(varchar(30), [Batch].[BatchDate],103) AS BatchDate,
			[Batch].[BatchType],
			[UserB].[Name] AS SubmittedBy,
			[Batch].[SafeToHandle], 
			CONVERT(varchar(30), [Batch].[DateReceived], 103) AS DateReceived,
			[UserC].[Name] AS ReceivedBy,
			[UserA].[Name] AS OtherSubmittedBy,
			[luFixatives].[Description] AS Fixation,
			[AreaA].[Description] AS OtherSubmittedArea,
			[Batch].[Cassetted],
			[Batch].[Comments],
			[Batch].[CustomerReceivedDate],
			[luStatus].[Description] AS Status,
			CONVERT(varchar(30), [Batch].[DateCompleted],103) As DateCompleted,
			[luTimeReceived].[Description] AS ReceivedTime,	
			[tblSpecies].[Species],
			[AreaB].[Description] AS SubmittedArea,
			[Batch].[BatchStatus]
		FROM
			Batch LEFT JOIN luTimeReceived ON [luTimeReceived].[Code] = [Batch].[TimeReceived]
			LEFT JOIN dbo.tlkpSpecies tblSpecies ON tblSpecies.SpeciesID = TRY_CONVERT(int, Batch.Species)
			LEFT JOIN luStatus ON [luStatus].[Code] = [Batch].[BatchStatus]
			LEFT JOIN luProjects ON [luProjects].[ID] = [Batch].[ProjectContractCode]
			LEFT JOIN luContacts ON [luContacts].[ID] = [Batch].[ContactName]
			LEFT JOIN [User] AS UserA ON [UserA].[ID] = [Batch].[OtherSubmittedBy]
			LEFT JOIN [User] AS UserB ON [UserB].[ID] = [Batch].[SubmittedBy]
			LEFT JOIN [User] AS UserC ON [UserC].[ID] = [Batch].[ReceivedBy]
			LEFT JOIN luUserArea AS AreaA ON [AreaA].[Code] = [Batch].[OtherSubmittedArea]
			LEFT JOIN luUserArea AS AreaB ON [AreaB].[Code] = [Batch].[SubmittedArea]
			LEFT JOIN luFixatives ON [luFixatives].[Code] = [Batch].[Fixation]
		WHERE
			([luProjects].[Description] = @ProjectContract or @ProjectContract IS NULL) AND
			([luContacts].[Description] = @ContactName or @ContactName IS NULL) AND
			([Batch].[Species] =@Species or @Species IS NULL) AND 
			([Batch].[OtherSubmittedArea] = @SubmittedArea or @SubmittedArea IS NULL) AND 
			([Batch].[Fixation] = @Fixation OR @Fixation IS NULL )AND
			(@SubmittedDateFrom IS NULL OR [Batch].[BatchDate] >= @SubmittedDateFrom) AND
			(@SubmittedDateTo IS NULL OR [Batch].[BatchDate] < DATEADD(day, 1, @SubmittedDateTo)) AND
			(@ReceivedDateFrom IS NULL OR [Batch].[DateReceived] >= @ReceivedDateFrom) AND
			(@ReceivedDateTo IS NULL OR [Batch].[DateReceived] < DATEADD(day, 1, @ReceivedDateTo)) AND
			EXISTS (
				SELECT 1 FROM UNION_BATCH ub
				WHERE ub.BatchID = Batch.ID
				AND (ub.HistologyRef = @HistologyRef OR @HistologyRef IS NULL)
				AND (ub.SenderRef = @SenderRef OR @SenderRef IS NULL)
			) AND(
			([Batch].[ID] = @Number OR @Number  IS NULL) AND
			([Batch].[SubmittedBy]  = @EnteredBy OR @EnteredBy IS NULL) AND
			([Batch].[OtherSubmittedBy] = @SubmittedBy OR @SubmittedBy IS NULL) AND
			([Batch].[BatchStatus] = @Status OR @Status IS NULL))
		ORDER BY
			[Batch].[ID] DESC
	END
	ELSE
	BEGIN
			SELECT
				[Batch].[ID],
				[luProjects].[Description] AS ProjectDescription, 
				[luContacts].[Description] AS ContactDescription,
				CONVERT(varchar(30), [Batch].[BatchDate],103) AS BatchDate,
				[Batch].[BatchType],
				[UserB].[Name] AS SubmittedBy,
				[Batch].[SafeToHandle], 
				CONVERT(varchar(30), [Batch].[DateReceived], 103) AS DateReceived,
				[UserC].[Name] AS ReceivedBy,
				[UserA].[Name] AS OtherSubmittedBy,
				[luFixatives].[Description] AS Fixation,
				[AreaA].[Description] AS OtherSubmittedArea,
				[Batch].[Cassetted],
				[Batch].[Comments],
				[Batch].[CustomerReceivedDate],
				[luStatus].[Description] AS Status,
				CONVERT(varchar(30), [Batch].[DateCompleted],103) As DateCompleted,
				[luTimeReceived].[Description] AS ReceivedTime,	
				[tblSpecies].[Species],
				[AreaB].[Description] AS SubmittedArea,
				[Batch].[BatchStatus]
			FROM
				Batch LEFT JOIN luTimeReceived ON [luTimeReceived].[Code] = [Batch].[TimeReceived]
				LEFT JOIN dbo.tlkpSpecies tblSpecies ON tblSpecies.SpeciesID = TRY_CONVERT(int, Batch.Species)
				LEFT JOIN luStatus ON [luStatus].[Code] = [Batch].[BatchStatus]
				LEFT JOIN luProjects ON [luProjects].[ID] = [Batch].[ProjectContractCode]
				LEFT JOIN luContacts ON [luContacts].[ID] = [Batch].[ContactName]
				LEFT JOIN [User] AS UserA ON [UserA].[ID] = [Batch].[OtherSubmittedBy]
				LEFT JOIN [User] AS UserB ON [UserB].[ID] = [Batch].[SubmittedBy]
				LEFT JOIN [User] AS UserC ON [UserC].[ID] = [Batch].[ReceivedBy]
				LEFT JOIN luUserArea AS AreaA ON [AreaA].[Code] = [Batch].[OtherSubmittedArea]
				LEFT JOIN luUserArea AS AreaB ON [AreaB].[Code] = [Batch].[SubmittedArea]
				LEFT JOIN luFixatives ON [luFixatives].[Code] = [Batch].[Fixation]
			WHERE
				([luProjects].[Description] = @ProjectContract or @ProjectContract IS NULL) AND
				([luContacts].[Description] = @ContactName or @ContactName IS NULL) AND
				([Batch].[Species] =@Species or @Species IS NULL) AND 
				([Batch].[OtherSubmittedArea] = @SubmittedArea or @SubmittedArea IS NULL) AND 
				([Batch].[Fixation] = @Fixation OR @Fixation IS NULL )AND
				(@SubmittedDateFrom IS NULL OR [Batch].[BatchDate] >= @SubmittedDateFrom) AND
				(@SubmittedDateTo IS NULL OR [Batch].[BatchDate] < DATEADD(day, 1, @SubmittedDateTo)) AND
				(@ReceivedDateFrom IS NULL OR [Batch].[DateReceived] >= @ReceivedDateFrom) AND
				(@ReceivedDateTo IS NULL OR [Batch].[DateReceived] < DATEADD(day, 1, @ReceivedDateTo)) AND
				EXISTS (
					SELECT 1 FROM UNION_BATCH ub
					WHERE ub.BatchID = Batch.ID
					AND (ub.HistologyRef = @HistologyRef OR @HistologyRef IS NULL)
					AND (ub.SenderRef = @SenderRef OR @SenderRef IS NULL)
				) AND(
				([Batch].[ID] = @Number OR @Number  IS NULL) AND
				([Batch].[SubmittedBy]  = @EnteredBy OR @EnteredBy IS NULL) AND
				([Batch].[OtherSubmittedBy] = @SubmittedBy OR @SubmittedBy IS NULL) AND
				([Batch].[BatchStatus] = @Status OR @Status IS NULL))
			ORDER BY
				[Batch].[ID] DESC
	END
GO
PRINT '--- dbo.GetSearchBatchDetails: created or altered ---';
GO


