/* Update script to update the stored procedures GetTestAntibodiesCount, GetTestStainCount and GetTestHistology count to use the test dispatch date 
	rather than the submission date */

/* Update GetTestAntibodiesCount */

if exists (select * from dbo.sysobjects where id = object_id(N'[GetTestAntibodiesCounts]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure dbo.[GetTestAntibodiesCounts]
GO

SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetTestAntibodiesCounts
	@ProjectContractCode varchar(20) = '',
	@SubmittedDateFrom datetime,
	@SubmittedDateTo datetime,
	@Code varchar(10) = '',
	@TCCode varchar(10) = '',
	@BatchType bit =0
AS

DECLARE @NumberOfSlides integer
SET @NumberOfSlides = (SELECT Sum(dbo.BlockAntibodies.NumberOfSlides)
FROM         dbo.BatchBlock INNER JOIN
                      dbo.BlockAntibodies ON dbo.BatchBlock.ID = dbo.BlockAntibodies.BlockID INNER JOIN
                      dbo.Batch ON dbo.BatchBlock.BatchID = dbo.Batch.ID INNER JOIN
                      dbo.AntibodiesTCCodes ON dbo.BlockAntibodies.ID = dbo.AntibodiesTCCodes.TestID
WHERE  	BlockAntibodies.Dispatched = 1 AND
		ISNULL(Batch.ProjectContractCode,'') = @ProjectContractCode AND 
		ISNULL(DispatchedDate, '1 January 1900') BETWEEN ISNULL(@SubmittedDateFrom, '1 January 1900') AND ISNULL(@SubmittedDateTo, GETDATE()) AND
		AntibodiesTCCodes.Code = @TCCode AND	
		Batch.BatchType = @BatchType AND
		BlockAntibodies.Code = @Code)
RETURN @NumberOfSlides
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON dbo.[GetTestAntibodiesCounts]  TO [HistologyUser]
GO

/* Update GetTestHistologyCounts */ 

if exists (select * from dbo.sysobjects where id = object_id(N'[GetTestHistologyCounts]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [GetTestHistologyCounts]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetTestHistologyCounts
	@ProjectContractCode varchar(20) = '',
	@SubmittedDateFrom datetime,
	@SubmittedDateTo datetime,
	@Code varchar(10) = '',
	@TCCode varchar(10) = '',
	@BatchType bit =0
AS

DECLARE @NumberOfSlides integer 
SET @NumberOfSlides = 
(SELECT Sum(dbo.BlockHistology.NumberOfSlides)
FROM         dbo.BatchBlock INNER JOIN
                      dbo.BlockHistology ON dbo.BatchBlock.ID = dbo.BlockHistology.BlockID INNER JOIN
                      dbo.Batch ON dbo.BatchBlock.BatchID = dbo.Batch.ID INNER JOIN
                      dbo.HistologyTCCodes ON dbo.BlockHistology.ID = dbo.HistologyTCCodes.TestID
WHERE  	BlockHistology.Dispatched = 1 AND
		ISNULL(Batch.ProjectContractCode,'') = @ProjectContractCode AND 
		ISNULL(DispatchedDate , '1 January 1900') BETWEEN ISNULL(@SubmittedDateFrom, '1 January 1900') AND ISNULL(@SubmittedDateTo, GETDATE()) AND
		HistologyTCCodes.Code = @TCCode AND	
		Batch.BatchType = @BatchType AND
		BlockHistology.Code = @Code)
RETURN  @NumberOfSlides
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON dbo.[GetTestHistologyCounts]  TO [HistologyUser]
GO


/* Update GetTestStainsCounts */ 

if exists (select * from dbo.sysobjects where id = object_id(N'[GetTestStainsCounts]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [GetTestStainsCounts]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetTestStainsCounts
	@ProjectContractCode varchar(20) = '',
	@SubmittedDateFrom datetime,
	@SubmittedDateTo datetime,
	@Code varchar(10) = '',
	@TCCode varchar(10) = '',
	@BatchType bit =0
AS

DECLARE @NumberOfSlides integer
SET @NumberOfSlides = (SELECT Sum(dbo.BlockStain.NumberOfSlides)
FROM         dbo.BatchBlock INNER JOIN
                      dbo.BlockStain ON dbo.BatchBlock.ID = dbo.BlockStain.BlockID INNER JOIN
                      dbo.Batch ON dbo.BatchBlock.BatchID = dbo.Batch.ID INNER JOIN
                      dbo.SpecialStainTCCodes ON dbo.BlockStain.ID = dbo.SpecialStainTCCodes.TestID
WHERE  	BlockStain.Dispatched = 1 AND
		ISNULL(Batch.ProjectContractCode,'') = @ProjectContractCode AND 
		ISNULL(DispatchedDate, '1 January 1900') BETWEEN ISNULL(@SubmittedDateFrom, '1 January 1900') AND ISNULL(@SubmittedDateTo, GETDATE()) AND
		SpecialStainTCCodes.Code = @TCCode AND	
		Batch.BatchType = @BatchType AND
		BlockStain.Code = @Code)
RETURN @NumberOfSlides
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON dbo.[GetTestStainsCounts]  TO [HistologyUser]
GO

/* Update GetAuditLogBySubmission to use as varchar as the submission id*/

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetAuditLogBySubmission]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetAuditLogBySubmission]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetAuditLogBySubmission
	@StartDate datetime,
	@EndDate datetime,
	@SubmissionID varchar(50) AS

	SELECT
		[AuditLog].[ID],
		[AuditLog].[TableName],
		[AuditLog].[FieldName],
		CONVERT(varchar(30),[AuditLog].[LogDate], 103) + ' ' + CONVERT(varchar(30), [AuditLog].[LogDate], 108) AS [DateTime],
		[User].[Name] AS [UserName],
		[AuditLog].[BeforeValue],
		[AuditLog].[AfterValue],
		[AuditLog].[Reason],
		[AuditLog].[KeyID]
	FROM
		[AuditLog] LEFT JOIN [User] ON [AuditLog].[UserID] = [User].[ID]
	WHERE
		LogDate BETWEEN ISNULL(@StartDate, '1 January 1900') AND ISNULL(DATEADD(d, 1, @EndDate), DATEADD(d,1, GETDATE())) AND
		[AuditLog].[ID] = @SubmissionID AND
		[AuditLog].[TableName] = 'Batch'
	ORDER BY
		[LogDate]
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON dbo.[GetAuditLogBySubmission]  TO [HistologyUser]
GO
