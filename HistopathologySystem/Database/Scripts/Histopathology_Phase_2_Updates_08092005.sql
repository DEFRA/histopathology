-- Phase 2 changes 15-08-2005

--- Alter the BatchBlock table to allow BatchId to be NULL
ALTER TABLE BatchBlock
ALTER COLUMN BatchId int NULL;
GO
--- Alter the Animal table to allow the SenderRef to be NULL
ALTER TABLE Animal
ALTER COLUMN SenderRef varchar(20) NULL;
GO
--- Update the Animal stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[AddAnimal]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[AddAnimal]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE AddAnimal
	@SenderRef varchar(20),
	@HistologyRef varchar(20),
	@NextBlockRef varchar(4),
	@OnHold bit,
	@PMDate datetime,
	@NewID int OUTPUT
AS        
	DECLARE
	@ErrorCode int,
	@RowCount int,
	@Test int

	IF NOT @HistologyRef IS NULL AND NOT @HistologyRef = '' BEGIN
		SELECT 
			HistologyRef
		FROM
			Animal
		WHERE
			HistologyRef=@HistologyRef
	
		SET @RowCount = @@ROWCOUNT
	
		IF @RowCount > 0  BEGIN
			RETURN 2
		END
	END

	SELECT
		[SenderRef]
	FROM
		[Animal]
	WHERE
		[SenderRef] = @SenderRef

	SET @RowCount = @@ROWCOUNT

	IF NOT @SenderRef IS NULL AND NOT @SenderRef = '' BEGIN
	IF @RowCount > 0 BEGIN
		SELECT
			@NewID = [ID]
		FROM
			[Animal]
		WHERE
			[SenderRef] = @SenderRef
		RETURN 1
	END
	END

	INSERT INTO Animal
		(SenderRef, HistologyRef, NextBlockRef, PMDate)
	VALUES
		(@SenderRef, @HistologyRef, @NextBlockRef, @PMDate)

	SET @NewID = SCOPE_IDENTITY()
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].[AddAnimal]  TO HistologyUser
GO

--- Add status column to BatchBlock table
ALTER TABLE BatchBlock
ADD Status int;
GO

UPDATE BatchBlock
SET
	BatchBlock.Status = 1
WHERE
	BatchBlock.Status IS NULL
GO

--- Add GetAnimalPreBookedBlocks stored procedure
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetAnimalPreBookedBlocks]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetAnimalPreBookedBlocks]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetAnimalPreBookedBlocks
	@AnimalID integer
AS

SELECT DISTINCT
	[BatchBlock].[ID],
	[BatchBlock].[BatchID],
	[BatchBlock].[AnimalID],
	[BatchBlock].[BlockRef],
	[BatchBlock].[CustomerRef],
	[BatchBlock].[RepeatBlock],
	[BatchBlock].[ArchiveLocation],
	[BatchBlock].[ArchivedDate],
	[BatchBlock].[ArchiveComment],
	[BatchBlock].[Comment],
	[BatchBlock].[Status],
	Convert(integer, [BatchBlock].[Blockref]) AS BlockRefOrder
FROM
	[Animal] INNER JOIN [BatchBlock] ON [Animal].[ID] = [BatchBlock].[AnimalID]	
WHERE
	[Animal].[ID] = @AnimalID AND
	([BatchBlock].[Status] = 2 OR [BatchBlock].[Status] = 3)
ORDER BY
	BlockRefOrder
RETURN
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetAnimalPreBookedBlocks  TO HistologyUser
GO

--- Update the AddBlock stored procedure 

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[AddBlock]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[AddBlock]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE AddBlock
	@ID int,
	@BatchID int,
	@AnimalID int,
	@BlockRef varchar(4),
	@CustomerRef varchar(20),
	@RepeatBlock bit,
	@Comment varchar(500),
	@Status int,
	@OldID int OUTPUT,
	@NewID int OUTPUT
AS
	INSERT INTO BatchBlock
		(BatchID, AnimalID, BlockRef, CustomerRef, RepeatBlock, Comment, Status)
	VALUES
		(@BatchID, @AnimalID, @BlockRef, @CustomerRef, @RepeatBlock, @Comment, @Status)    

	SET @NewID = SCOPE_IDENTITY()  
	SET @OldID = @ID
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].AddBlock  TO HistologyUser
GO
--- Update the EditBlock stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[EditBlock]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[EditBlock]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE EditBlock
	@ID int,
	@BatchID int,
	@AnimalID int,
	@BlockRef varchar(4),
	@CustomerRef varchar(20),
	@RepeatBlock bit,
	@ArchiveLocation varchar(10),
	@ArchivedDate datetime,
	@ArchiveComment varchar(500),
	@Comment varchar(500),
	@Status int,
	@UserID int
AS
DECLARE 
	@ErrorCode int, 
	@RowsUpdated int,
	@columnName  varchar(50),
	@columnValue varchar(500),
	@columnCount int,
	@oldColumnValue varchar(500), 
	@BatchStatus int,
	@oldColumnValue1 varchar(500), @oldColumnValue2 varchar(500), @oldColumnValue3 varchar(500), @oldColumnValue4 varchar(500), @oldColumnValue5 varchar(500), 
	@oldColumnValue6 varchar(500), @oldColumnValue7 varchar(500), @oldColumnValue8 varchar(500), @oldColumnValue9 varchar(500), @oldColumnValue10 varchar(500),
	@oldColumnValue11 varchar(500)

	SET @oldColumnValue2 = CONVERT(varchar(500), (SELECT BatchID  FROM BatchBlock WHERE ID=@ID))
	SET @oldColumnValue3 = CONVERT(varchar(500), (SELECT AnimalID  FROM BatchBlock WHERE ID=@ID))
	SET @oldColumnValue4 = CONVERT(varchar(500), (SELECT BlockRef  FROM BatchBlock WHERE ID=@ID))
	SET @oldColumnValue5 = CONVERT(varchar(500), (SELECT CustomerRef  FROM BatchBlock WHERE ID=@ID))
	SET @oldColumnValue6 = CONVERT(varchar(500), (SELECT RepeatBlock  FROM BatchBlock WHERE ID=@ID))
	SET @oldColumnValue7 = CONVERT(varchar(500), (SELECT ArchiveLocation  FROM BatchBlock WHERE ID=@ID))
	SET @oldColumnValue8 = CONVERT(varchar(500), (SELECT ArchivedDate  FROM BatchBlock WHERE ID=@ID))
	SET @oldColumnValue9 = CONVERT(varchar(500), (SELECT ArchiveComment  FROM BatchBlock WHERE ID=@ID))
	SET @oldColumnValue10 = CONVERT(varchar(500), (SELECT Comment  FROM BatchBlock WHERE ID=@ID))
	SET @oldColumnValue11 = CONVERT(varchar(500), (SELECT Status  FROM BatchBlock WHERE ID=@ID))

	UPDATE BatchBlock SET
		BatchID = @BatchID,
		AnimalID =@AnimalID,
		BlockRef =@BlockRef,
		CustomerRef=@CustomerRef,
		RepeatBlock=@RepeatBlock,
		ArchiveLocation=@ArchiveLocation,
		ArchivedDate=@ArchivedDate,
		ArchiveComment=@ArchiveComment,
		Comment=@Comment,
		Status=@Status
	WHERE
		ID=@ID

	SELECT @ErrorCode = @@ERROR, @RowsUpdated = @@ROWCOUNT

	SET @columnCount = 1

	SET @BatchStatus = (SELECT BatchStatus FROM Batch WHERE ID=@BatchID)
	IF @BatchStatus <>1 BEGIN

		WHILE @columnCount < 11
		BEGIN
			SET @columnName = CASE @columnCount
				WHEN 1 THEN 'ID'
				WHEN 2 THEN 'BatchID'
				WHEN 3 THEN 'AnimalID'
				WHEN 4 THEN 'BlockRef'
				WHEN 5 THEN 'CustomerRef'
				WHEN 6 THEN 'RepeatBlock'
				WHEN 7 THEN 'ArchiveLocation'
				WHEN 8 THEN 'ArchivedDate'
				WHEN 9 THEN 'ArchiveComment'
				WHEN 10 THEN 'Comment'
				WHEN 11 THEN 'Status'
			END
	
			SET @columnVALUE = CASE @columnCount 
				WHEN 1 THEN CONVERT(varchar(500), @ID)
				WHEN 2 THEN CONVERT(varchar(500), @BatchID)
				WHEN 3 THEN CONVERT(varchar(500), @AnimalID)
				WHEN 4 THEN CONVERT(varchar(500), @BlockRef)
				WHEN 5 THEN CONVERT(varchar(500), @CustomerRef)
				WHEN 6 THEN CONVERT(varchar(500), @RepeatBlock)
				WHEN 7 THEN CONVERT(varchar(500), @ArchiveLocation)
				WHEN 8 THEN CONVERT(varchar(500), @ArchivedDate)
				WHEN 9 THEN CONVERT(varchar(500), @ArchiveComment)
				WHEN 10 THEN CONVERT(varchar(500), @Comment)
				WHEN 11 THEN CONVERT(varchar(500), @Status)
			END
	
			SET @oldColumnValue = CASE @columnCount
				WHEN 1 THEN CONVERT(varchar(500), @ID)
				WHEN 2 THEN @oldColumnValue2
				WHEN 3 THEN @oldColumnValue3
				WHEN 4 THEN @oldColumnValue4
				WHEN 5 THEN @oldColumnValue5
				WHEN 6 THEN @oldColumnValue6
				WHEN 7 THEN @oldColumnValue7
				WHEN 8 THEN @oldColumnValue8
				WHEN 9 THEN @oldColumnValue9
				WHEN 10 THEN @oldColumnValue10
				WHEN 11 THEN @oldColumnValue11
			END
			
	
			IF @oldColumnValue <> @columnValue AND NOT @oldColumnValue IS NULL BEGIN
				INSERT INTO AuditLog 
					(ID, TableName, FieldName, LogDate, UserID, BeforeValue , AfterValue, Reason)
				VALUES
			        	(@ID, 'BatchBlock', @columnName, GetDate(), @UserID, @oldColumnValue, @columnValue, 'BlockEdit')
			END
	
			SET @columnCount = @columnCount + 1
		END

	END

	IF @ErrorCode = 0 BEGIN
		IF @RowsUpdated = 0 BEGIN
		    RETURN -1
	END ELSE BEGIN
		    RETURN 0
	END
	END ELSE BEGIN
		RETURN @ErrorCode
	END
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].EditBlock  TO HistologyUser
GO
--- Update GetBatchBlockDetails to return the status of the block

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetBatchBlockDetails]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetBatchBlockDetails]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetBatchBlockDetails
	@ID integer
AS	
SELECT
	[BatchBlock].[ID],
	[BatchBlock].[BatchID],
	[BatchBlock].[AnimalID],
	[BatchBlock].[BlockRef],
	[BatchBlock].[CustomerRef],
	[BatchBlock].[RepeatBlock],
	[BatchBlock].[ArchiveLocation],
	[BatchBlock].[ArchivedDate],
	[BatchBlock].[ArchiveComment],
	[BatchBlock].[Comment],
	[BatchBlock].[Status]
FROM 
	[Batch] INNER JOIN [BatchBlock] ON [BatchBlock].[BatchID] = [Batch].[ID]
WHERE 
	[Batch].[ID] = @ID
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetBatchBlockDetails  TO HistologyUser
GO

--- Create GetBlockByID stored procedure 

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetBlockByID]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetBlockByID]
GO

CREATE PROCEDURE GetBlockByID
	@ID integer
AS	
SELECT
	[BatchBlock].[ID],
	[BatchBlock].[BatchID],
	[BatchBlock].[AnimalID],
	[BatchBlock].[BlockRef],
	[BatchBlock].[CustomerRef],
	[BatchBlock].[RepeatBlock],
	[BatchBlock].[ArchiveLocation],
	[BatchBlock].[ArchivedDate],
	[BatchBlock].[ArchiveComment],
	[BatchBlock].[Comment],
	[BatchBlock].[Status]
FROM 
	[BatchBlock]
WHERE 
	[BatchBlock].[ID] = @ID

GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetBlockByID  TO HistologyUser
GO

--- Create EditBlockStatus stored procedure.

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[EditBlockStatus]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[EditBlockStatus]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE EditBlockStatus 
	@BlockID integer,
	@Status integer
AS
UPDATE
	[BatchBlock]
SET
	[BatchBlock].[Status] = @Status
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].EditBlockStatus  TO HistologyUser
GO

--- Add GetAnimalPreBookedBlocksBySenderRef stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetAnimalPreBookedBlocksBySenderRef]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetAnimalPreBookedBlocksBySenderRef]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetAnimalPreBookedBlocksBySenderRef
	@SenderRef varchar(20)
AS

SELECT DISTINCT
	[BatchBlock].[ID],
	[BatchBlock].[BatchID],
	[BatchBlock].[AnimalID],
	[BatchBlock].[BlockRef],
	[BatchBlock].[CustomerRef],
	[BatchBlock].[RepeatBlock],
	[BatchBlock].[ArchiveLocation],
	[BatchBlock].[ArchivedDate],
	[BatchBlock].[ArchiveComment],
	[BatchBlock].[Comment],
	[BatchBlock].[Status],
	Convert(integer, [BatchBlock].[BlockRef]) AS BlockRefOrder
FROM
	[Animal] INNER JOIN [BatchBlock] ON [Animal].[ID] = [BatchBlock].[AnimalID]	
WHERE
	[Animal].[SenderRef] = @SenderRef AND
	([BatchBlock].[Status] = 2 OR [BatchBlock].[Status] = 3)
ORDER BY
	BlockRefOrder

RETURN

GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetAnimalPreBookedBlocksBySenderRef  TO HistologyUser
GO

--- Create EditPreBookedBlock stored procedure
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[EditPreBookedBlock]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[EditPreBookedBlock]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO



CREATE PROCEDURE EditPreBookedBlock
	@ID int,
	@BlockRef varchar(4),
	@SubmissionID int,
	@Status int

AS
DECLARE 
	@ErrorCode int, 
	@RowsUpdated int

	UPDATE BatchBlock SET
		BlockRef =@BlockRef,
		BatchID = @SubmissionID,
		Status = @Status
	WHERE
		ID=@ID

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
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].EditPreBookedBlock  TO HistologyUser
GO

--- Create GetBlocksForSenderRef stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetBlocksForSenderRef]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetBlocksForSenderRef]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetBlocksForSenderRef
	@SenderRef varchar(20)
AS
	SELECT DISTINCT
		Convert(integer, [BatchBlock].[BlockRef]) AS BlockRef,
		[BatchBlock].[Status]
	FROM  
		[BatchBlock] INNER JOIN [Animal] ON [BatchBlock].[AnimalID] = [Animal].[ID]
	WHERE 
		[Animal].[SenderRef] = @SenderRef
	ORDER BY
		Convert(integer, BlockRef)

GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].[GetBlocksForSenderRef]  TO HistologyUser
GO

--- Create GetAnimalByHistologyRef stored procedure
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetAnimalByHistologyRef]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetAnimalByHistologyRef]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetAnimalByHistologyRef
	@HistologyRef varchar(20)
AS

SELECT
	[Animal].[ID],
	[Animal].[SenderRef],
	[Animal].[HistologyRef],
	[Animal].[PMDate],
	[Animal].[OnHold],
	[Animal].[NextBlockRef],
	[Animal].[RowStamp]
FROM
	[Animal]
WHERE
	[HistologyRef]=@HistologyRef

RETURN
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetAnimalByHistologyRef  TO HistologyUser
GO


--- Update GetBlocksForHistoRef stored procedure
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetBlocksForHistoRef]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetBlocksForHistoRef]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetBlocksForHistoRef 
	@HistologyRef varchar(20) = NULL,
	@SenderRef varchar(20) = NULL
AS
	IF @SenderRef = NULL BEGIN
		SELECT DISTINCT
			Convert(int, [BatchBlock].[BlockRef]) AS BlockRef,
			[BatchBlock].[Status]
		FROM  
			[BatchBlock] INNER JOIN [Animal] ON [BatchBlock].[AnimalID] = [Animal].[ID]
		WHERE 
			[Animal].[HistologyRef] = @HistologyRef
		ORDER BY
			BlockRef
	END ELSE
		SELECT DISTINCT
			Convert(int, [BatchBlock].[BlockRef]) AS BlockRef,
			[BatchBlock].[Status]
		FROM  
			[BatchBlock] INNER JOIN [Animal] ON [BatchBlock].[AnimalID] = [Animal].[ID]
		WHERE 
			[Animal].[SenderRef] = @SenderRef
		ORDER BY
			BlockRef
		
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetBlocksForHistoRef  TO HistologyUser
GO

--- Update GetBatchSampleTissues stored procedure to remove the orderby clause

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetBatchSampleTissues]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetBatchSampleTissues]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetBatchSampleTissues
	@BatchID integer,
	@AnimalID integer
AS

SELECT DISTINCT
	[BatchTissues].[TissueCode] as [Code],
	[luTissueType].[Description],
	[luTissueType].[Code] +'  ' + '--' +' ' + [luTissueType].[Description] as [LongDescription],
	[BatchTissues].[ID],
	[BatchTissues].[NoPieces]

FROM  
	[Batch] INNER JOIN [BatchSubmission] ON [Batch].[ID] = [BatchSubmission].[BatchID] 
	INNER JOIN [Animal] ON [BatchSubmission].[AnimalID] = [Animal].[ID] 
	INNER JOIN [BatchTissues] ON [BatchSubmission].[ID] = [BatchTissues].[BatchSubmissionID] 
	INNER JOIN [luTissueType] ON [BatchTissues].[TissueCode] = [luTissueType].[Code]

WHERE [Batch].[ID] = @BatchID AND [BatchSubmission].[AnimalID] = @AnimalID
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].[GetBatchSampleTissues]  TO HistologyUser
GO
--- Create GetBatchComments stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetBatchComments]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetBatchComments]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetBatchComments 
	@ID int
AS
	
	SELECT
		[Batch].[ID],
		[Batch].[Comments],
		[Batch].[StatusComments],
		[Batch].[BatchType]
	FROM
		[Batch]
	WHERE
		[Batch].[ID] = @ID
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetBatchComments  TO HistologyUser
GO
--- Create GetBatchBlockComments stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetBatchBlockComments]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetBatchBlockComments]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetBatchBlockComments 
	@ID int
AS
	SELECT
		[BatchBlock].[BlockRef],
		[BatchBlock].[Comment],
		[BatchBlock].[ArchiveComment],
		[Animal].[SenderRef],
		[Animal].[HistologyRef]
	FROM
		[Batch] INNER JOIN [BatchBlock] ON [BatchBlock].[BatchID] = [Batch].[ID]
		INNER JOIN [Animal] ON [Animal].[ID] = [BatchBlock].[AnimalID]
	WHERE
		[Batch].[ID]= @ID AND
		NOT([BatchBlock].[Comment] = '' AND [BatchBlock].[ArchiveComment] = '')
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetBatchBlockComments  TO HistologyUser
GO

--- Create GetBatchBlockAntibodiesNotes stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetBatchBlockAntibodiesNotes]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetBatchBlockAntibodiesNotes]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetBatchBlockAntibodiesNotes 
	@ID int
AS
	DECLARE @BatchType int

	SELECT 
		@BatchType = [Batch].[BatchType]
	FROM
		[Batch]
	WHERE
		[Batch].[ID] = @ID

	IF @BatchType = 1 BEGIN
		SELECT
			[BatchBlock].[BlockRef], 
			[luNonTseAntibodies].[Description],
			[BlockAntibodies].[Comment],
			[BlockAntibodies].[ArchiveComment]
		FROM
			[Batch] INNER JOIN [BatchBlock] ON [Batch].[ID] = [BatchBlock].[BatchID]
			INNER JOIN [BlockAntibodies] ON [BlockAntibodies].[BlockID] = [BatchBlock].[ID]
			INNER JOIN [luNonTseAntibodies] ON [luNonTseAntibodies].[Code] = [BlockAntibodies].[Code]
		
		WHERE
			[Batch].[ID] = @ID AND
			NOT ([BlockAntibodies].[Comment] = '' AND [BlockAntibodies].[ArchiveComment] = '')
		UNION
		SELECT
			[BatchBlock].[BlockRef],
			'Other Antibodies' AS [Description],
			[BlockAntibodies].[Comment],
			[BlockAntibodies].[ArchiveComment]
		FROM 
			[Batch] INNER JOIN [BatchBlock] ON [Batch].[ID] = [BatchBlock].[BatchID]
			INNER JOIN [BlockAntibodies] ON [BlockAntibodies].[BlockID] = [BatchBlock].[ID]
		WHERE
			[Batch].[ID] = @ID AND [BlockAntibodies].[Code] = 'Other' AND
			NOT ([BlockAntibodies].[Comment] = '' AND [BlockAntibodies].[ArchiveComment] = '')
	END ELSE
		SELECT
			[BatchBlock].[BlockRef], 
			[luTseAntibodies].[Description],
			[BlockAntibodies].[Comment],
			[BlockAntibodies].[ArchiveComment]
		FROM
			[Batch] INNER JOIN [BatchBlock] ON [Batch].[ID] = [BatchBlock].[BatchID]
			INNER JOIN [BlockAntibodies] ON [BlockAntibodies].[BlockID] = [BatchBlock].[ID]
			INNER JOIN [luTseAntibodies] ON [luTseAntibodies].[Code] = [BlockAntibodies].[Code]
		
		WHERE
			[Batch].[ID] = @ID AND
			NOT ([BlockAntibodies].[Comment] = '' AND [BlockAntibodies].[ArchiveComment] = '')
		UNION
		SELECT
			[BatchBlock].[BlockRef],
			'Other Antibodies' AS [Description],
			[BlockAntibodies].[Comment],
			[BlockAntibodies].[ArchiveComment]
		FROM 
			[Batch] INNER JOIN [BatchBlock] ON [Batch].[ID] = [BatchBlock].[BatchID]
			INNER JOIN [BlockAntibodies] ON [BlockAntibodies].[BlockID] = [BatchBlock].[ID]
		WHERE
			[Batch].[ID] = @ID AND [BlockAntibodies].[Code] = 'Other' AND
			NOT ([BlockAntibodies].[Comment] = '' AND [BlockAntibodies].[ArchiveComment] = '')
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetBatchBlockAntibodiesNotes  TO HistologyUser
GO
--- Create GetBatchBlockHistologyNotes stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetBatchBlockHistologyNotes]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetBatchBlockHistologyNotes]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetBatchBlockHistologyNotes 
	@ID int
AS
	
		SELECT
			[BatchBlock].[BlockRef], 
			[luHistology].[Description],
			[BlockHistology].[Comment],
			[BlockHistology].[ArchiveComment]
		FROM
			[Batch] INNER JOIN [BatchBlock] ON [Batch].[ID] = [BatchBlock].[BatchID]
			INNER JOIN [BlockHistology] ON [BlockHistology].[BlockID] = [BatchBlock].[ID]
			INNER JOIN [luHistology] ON [luHistology].[Code] = [BlockHistology].[Code]
		WHERE
			[Batch].[ID] = @ID AND
			([BlockHistology].[Code] <> 3 AND [BlockHistology].[Code] <> 4 AND [BlockHistology].[Code] <> 6) AND
			NOT ([BlockHistology].[Comment] = '' AND [BlockHistology].[ArchiveComment] = '')
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetBatchBlockHistologyNotes  TO HistologyUser
GO
--- Create GetBatchBlockStainNotes stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetBatchBlockStainNotes]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetBatchBlockStainNotes]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetBatchBlockStainNotes 
	@ID int
AS
	
		SELECT
			[BatchBlock].[BlockRef], 
			[luSpecialStain].[Description],
			[BlockStain].[Comment],
			[BlockStain].[ArchiveComment]
		FROM
			[Batch] INNER JOIN [BatchBlock] ON [Batch].[ID] = [BatchBlock].[BatchID]
			INNER JOIN [BlockStain] ON [BlockStain].[BlockID] = [BatchBlock].[ID]
			INNER JOIN [luSpecialStain] ON [luSpecialStain].[Code] = [BlockStain].[Code]
		WHERE
			[Batch].[ID] = @ID AND
			NOT([BlockStain].[Comment] = '' AND [BlockStain].[ArchiveComment] ='')
		UNION
		SELECT
			[BatchBlock].[BlockRef], 
			'Other Stain' AS [Description],
			[BlockStain].[Comment],
			[BlockStain].[ArchiveComment]
		FROM
			[Batch] INNER JOIN [BatchBlock] ON [Batch].[ID] = [BatchBlock].[BatchID]
			INNER JOIN [BlockStain] ON [BlockStain].[BlockID] = [BatchBlock].[ID]
		WHERE
			[Batch].[ID] = @ID AND [BlockStain].[Code] = 'Other' AND
			NOT([BlockStain].[Comment] = '' AND [BlockStain].[ArchiveComment] ='')
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetBatchBlockStainNotes  TO HistologyUser
GO

--- Create GetBatchTissuesComments stored procedure
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetBatchTissuesComments]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetBatchTissuesComments]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetBatchTissuesComments
	@ID int
 AS

SELECT
	[BatchTissues].[TissueCode],
	[BatchTissues].[Comment],
	[BatchTissues].[ArchiveComment],
	[Animal].[SenderRef],
	[Animal].[HistologyRef]
FROM
	[Batch] INNER JOIN [BatchSubmission] ON [Batch].[ID] = [BatchSubmission].[BatchID]
	INNER JOIN [BatchTissues] ON [BatchSubmission].[ID] = [BatchTissues].[BatchSubmissionID]
	INNER JOIN [Animal] ON [BatchSubmission].[AnimalID] = [Animal].[ID]
WHERE
	[Batch].[ID] = @ID AND
	(NOT ([BatchTissues].[Comment] = '' AND [BatchTissues].[ArchiveComment] = ''))

GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetBatchTissuesComments  TO HistologyUser
GO

--- Create GetAllBatchComments stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetAllBatchComments]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetAllBatchComments]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetAllBatchComments
	@ID int
 AS
	EXEC GetBatchComments @ID
	EXEC GetBatchTissuesComments @ID
	EXEC GetBatchBlockComments @ID
	EXEC GetBatchBlockAntibodiesNotes @ID
	EXEC GetBatchBlockHistologyNotes @ID
	EXEC GetBatchBlockStainNotes @ID
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].[GetAllBatchComments]  TO HistologyUser
GO


--- Create GetICCSUBMI1TISSUEONLYTO12THJAN2001 stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetICCSUBMI1TISSUEONLYTO12THJAN2001]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetICCSUBMI1TISSUEONLYTO12THJAN2001]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetICCSUBMI1TISSUEONLYTO12THJAN2001
 AS
SELECT
	[Ext Ref] As SenderRef,
	[CPU Ref] As HistologyRef,
	[Block No] AS BlockRef,
	[Project No] AS Project,
	Species,
	Tissue,
	CONVERT(varchar(30), [Date sub to CPU],103) AS DateSubmitted,
	Comments
FROM 
	IMPORTEDICCSUBMI1TISSUEONLYTO12THJAN2001
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetICCSUBMI1TISSUEONLYTO12THJAN2001  TO HistologyUser
GO
--- Create Get2001EXTSUB stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[Get2001EXTSUB]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[Get2001EXTSUB]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE Get2001EXTSUB
AS
SELECT
	[CPU REF] As HistologyRef,
	CONVERT(varchar(30), [Date sub to CPU],103) AS DateSubmitted,
	[Ext Ref] As SenderRef,
	[Project No] As Project,
	Species,
	Tissue,
	[Block No] As BlockRef,
	Comments
FROM
	IMPORTED2001EXTSUB
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].[Get2001EXTSUB]  TO HistologyUser
GO

--- Create Get2001NEUROSUB stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[Get2001NEUROSUB]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[Get2001NEUROSUB]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE Get2001NEUROSUB AS

SELECT
	[CPU REF] As HistologyRef,
	CONVERT(varchar(30), [Date sub to CPU],103) AS DateSubmitted,
	[Ext Ref] As SenderRef,
	[Project No] As Project,
	Species,
	Tissue,
	[Block No] As BlockRef,
	Comments
FROM
	IMPORTED2001NEUROSUB
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].Get2001NEUROSUB  TO HistologyUser
GO

--- Create Get2002EXTSUB stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[Get2002EXTSUB]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[Get2002EXTSUB]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE Get2002EXTSUB AS

SELECT
	[CPU ref] As HistologyRef,
	CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
	[External Ref] As SenderRef,
	NULL As Project,
	NULL As Species,
	NULL As Tissue,
	[Block Ref] As BlockRef,
	Comments
FROM
	IMPORTED2002EXTSUB
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].Get2002EXTSUB  TO HistologyUser
GO

--- Create Get2002EXTSUBNOCPU stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[Get2002EXTSUBNOCPU]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[Get2002EXTSUBNOCPU]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE Get2002EXTSUBNOCPU AS

SELECT
	NULL As HistologyRef,
	CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
	[External Ref] As SenderRef,
	NULL As Project,
	NULL As Species,
	NULL As Tissue,
	[Block Ref] As BlockRef,
	Comments
FROM
	IMPORTED2002EXTSUBNOCPU
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].Get2002EXTSUBNOCPU  TO HistologyUser
GO

--- Create Get2002MOUSESUB stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[Get2002MOUSESUB]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[Get2002MOUSESUB]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE Get2002MOUSESUB AS

SELECT
	NULL As HistologyRef,
	CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
	[MC Ref] As SenderRef,
	NULL As Project,
	NULL As Species,
	NULL As Tissue,
	[Block Ref] As BlockRef,
	Comments
FROM
	IMPORTED2002MOUSESUB
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].Get2002MOUSESUB  TO HistologyUser
GO

--- Create Get2002NEUROSUB stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[Get2002NEUROSUB]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[Get2002NEUROSUB]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE Get2002NEUROSUB AS

SELECT
	[CPU ref] As HistologyRef,
	CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
	[External ref] As SenderRef,
	NULL As Project,
	NULL As Species,
	NULL As Tissue,
	[Block Ref] As BlockRef,
	Comments
FROM
	IMPORTED2002NEUROSUB
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].Get2002NEUROSUB  TO HistologyUser
GO

--- Create Get2003EXTSUB stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[Get2003EXTSUB]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[Get2003EXTSUB]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE Get2003EXTSUB AS

SELECT
	[CPU ref] As HistologyRef,
	CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
	[External ref] As SenderRef,
	Project,
	Species,
	Tissues AS Tissue,
	[Block Ref] As BlockRef,
	Comments
FROM
	IMPORTED2003EXTSUB
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].Get2003EXTSUB  TO HistologyUser
GO

--- Create Get2003MOUSESUB stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[Get2003MOUSESUB]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[Get2003MOUSESUB]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE Get2003MOUSESUB AS

SELECT
	[CPU ref] As HistologyRef,
	CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
	[MC ref] As SenderRef,
	NULL AS Project,
	NULL AS Species,
	NULL AS Tissue,
	NULL As BlockRef,
	Comments
FROM
	IMPORTED2003MOUSESUB
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].Get2003MOUSESUB  TO HistologyUser
GO

--- Create Get2003NEUROSUB stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[Get2003NEUROSUB]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[Get2003NEUROSUB]
GO

SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE Get2003NEUROSUB AS

SELECT
	[CPU ref] As HistologyRef,
	CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
	[Ext ref] As SenderRef,
	Project AS Project,
	Species AS Species,
	Tissues AS Tissue,
	[Block ref] As BlockRef,
	Comments
FROM
	IMPORTED2003NEUROSUB
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].Get2003NEUROSUB  TO HistologyUser
GO

--- Create Get2004EXTSUB stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[Get2004EXTSUB]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[Get2004EXTSUB]
GO

SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE Get2004EXTSUB AS

SELECT
	[Histo ref] As HistologyRef,
	CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
	[External ref] As SenderRef,
	Project AS Project,
	Species AS Species,
	Tissues AS Tissue,
	[Block ref] As BlockRef,
	Comments
FROM
	IMPORTED2004EXTSUB
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].Get2004EXTSUB  TO HistologyUser
GO

--- Create Get2004MOUSESUB stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[Get2004MOUSESUB]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[Get2004MOUSESUB]
GO

SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE Get2004MOUSESUB AS

SELECT
	[Histo ref] As HistologyRef,
	CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
	[Histo ref] As SenderRef,
	NULL AS Project,
	Species AS Species,
	Tissues AS Tissue,
	[Block ref] As BlockRef,
	Comments
FROM
	IMPORTED2004MOUSESUB
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].Get2004MOUSESUB  TO HistologyUser
GO

--- Create Get2004NEUROSUB stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[Get2004NEUROSUB]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[Get2004NEUROSUB]
GO

SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE Get2004NEUROSUB AS

SELECT
	[Histo ref] As HistologyRef,
	CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
	[Ext ref] As SenderRef,
	[Project Number] AS Project,
	Species AS Species,
	Tissues AS Tissue,
	[Block ref] As BlockRef,
	Comments
FROM
	IMPORTED2004NEUROSUB
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].Get2004NEUROSUB  TO HistologyUser
GO

--- Get2005TBDIAGSUB stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[Get2005TBDIAGSUB]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[Get2005TBDIAGSUB]
GO

SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE Get2005TBDIAGSUB AS

SELECT
	[Histo ref] As HistologyRef,
	CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
	[External ref] As SenderRef,
	[Project] AS Project,
	Species AS Species,
	Tissues AS Tissue,
	[Block ref] As BlockRef,
	Comments
FROM
	IMPORTED2005TBDIAGSUB
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].[Get2005TBDIAGSUB]  TO HistologyUser
GO

--- Create GetICCSUBMI11999TO12JAN2001 stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetICCSUBMI11999TO12JAN2001]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetICCSUBMI11999TO12JAN2001]
GO

SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetICCSUBMI11999TO12JAN2001 AS

SELECT
	[CPU Ref] As HistologyRef,
	CONVERT(varchar(30), [Date Sub to CPU],103) AS DateSubmitted,
	[Ext ref] As SenderRef,
	[Project No] AS Project,
	Species AS Species,
	Tissue AS Tissue,
	[Block No] As BlockRef,
	Comments
FROM
	IMPORTEDICCSUBMI11999TO12JAN2001
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetICCSUBMI11999TO12JAN2001  TO HistologyUser
GO

--- Create luImportedTables stored procedure and insert the table names 

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[luImportedTables]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[luImportedTables]
GO

CREATE TABLE luImportedTables
(
	ID int IDENTITY,
	Name varchar(100) NOT NULL
	PRIMARY KEY(ID)
);
GO	
	
INSERT INTO luImportedTables VALUES('2001 External Submissions');
INSERT INTO luImportedTables VALUES('2001 Neuropath Submissions');
INSERT INTO luImportedTables VALUES('2002 External Submissions');
INSERT INTO luImportedTables VALUES('2002 External Submissions No CPU Number To Be Assigned');
INSERT INTO luImportedTables VALUES('2002 Mouse Bioassay Team');
INSERT INTO luImportedTables VALUES('2002 Neuropath Submissions');
INSERT INTO luImportedTables VALUES('2003 External Submissions');
INSERT INTO luImportedTables VALUES('2003 Mouse Bioassay Team');
INSERT INTO luImportedTables VALUES('2003 Neuropath Submissions');
INSERT INTO luImportedTables VALUES('2004 External Submissions');
INSERT INTO luImportedTables VALUES('2004 Mouse Bioassay Team');
INSERT INTO luImportedTables VALUES('2004 Neuropath Team');
INSERT INTO luImportedTables VALUES('2005 TB Diag (30000-39999)');
INSERT INTO luImportedTables VALUES('SPARE COPY ICCSUBMI1 1999 TO 12 JAN2001(do not use this)');
INSERT INTO luImportedTables VALUES('ICCSUBMI1-NOW ADD ADDITIONAL TISSUE ONLY-CASES TO 12TH JAN 2001');


--- Create GetluImportedTables stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetluImportedTables]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetluImportedTables]
GO

SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetluImportedTables AS
SELECT
	[luImportedTables].[ID],
	[luImportedTables].[Name]
FROM
	[luImportedTables]
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].[GetluImportedTables]  TO HistologyUser
GO

--- Create GetStainDispatched stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetStainDispatched]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetStainDispatched]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetStainDispatched 
	@BatchID int
AS

SELECT 
	BlockStain.DispatchedDate,
	BlockStain.Dispatched
FROM
	Batch INNER JOIN BatchBlock ON Batch.ID = BatchBlock.BatchID
	INNER JOIN BlockStain ON BatchBlock.ID = BlockStain.BlockID
WHERE
	Batch.ID = @BatchID
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].[GetStainDispatched]  TO HistologyUser
GO

--- Create GetAntibodiesDispatched stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetAntibodiesDispatched]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetAntibodiesDispatched]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetAntibodiesDispatched 
	@BatchID int
AS

SELECT 
	BlockAntibodies.DispatchedDate,
	BlockAntibodies.Dispatched
FROM
	Batch INNER JOIN BatchBlock ON Batch.ID = BatchBlock.BatchID
	INNER JOIN BlockAntibodies ON BatchBlock.ID = BlockAntibodies.BlockID
WHERE
	Batch.ID = @BatchID
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].[GetAntibodiesDispatched]  TO HistologyUser
GO

--- Create GetHistologyDispatched stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetHistologyDispatched]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetHistologyDispatched]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetHistologyDispatched 
	@BatchID int
AS

SELECT 
	BlockHistology.Code,
	BlockHistology.DispatchedDate,
	BlockHistology.Dispatched
FROM
	Batch INNER JOIN BatchBlock ON Batch.ID = BatchBlock.BatchID
	INNER JOIN BlockHistology ON BatchBlock.ID = BlockHistology.BlockID
WHERE
	Batch.ID = @BatchID AND
	not BlockHistology.Code IN (3, 4, 6)
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].[GetHistologyDispatched]  TO HistologyUser
GO

--- Create GetBatchesLinkedToBlocks stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetBatchesLinkedToBlocks]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetBatchesLinkedToBlocks]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetBatchesLinkedToBlocks 

AS
	SELECT DISTINCT
		Batch.ID
	FROM
		Batch INNER JOIN BatchBlock ON Batch.ID = BatchBlock.BatchID
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].[GetBatchesLinkedToBlocks]  TO HistologyUser
GO

--- Create EditBatchCompletedDate stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[EditBatchCompletedDate]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[EditBatchCompletedDate]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE EditBatchCompletedDate 
	@BatchID int,
	@CompletedDate datetime
AS

	UPDATE
		Batch
	SET
		Batch.DateCompleted = @CompletedDate
	WHERE
		Batch.ID = @BatchID
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].[EditBatchCompletedDate]  TO HistologyUser
GO

--- Update GetSearchBatchDetails stored procedure to search on project and contact description

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetSearchBatchDetails]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetSearchBatchDetails]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

CREATE PROCEDURE GetSearchBatchDetails
	@SubmittedBy integer = null,
	@ProjectContract varchar(50) = null,
	@ContactName varchar(50) = null,
	@Species varchar(10) = null,
	@SubmittedArea varchar(10)= null,
	@Fixation varchar(10) = null,
	@Status  integer = null,
	@SubmittedDateFrom datetime,
	@SubmittedDateTo datetime,
	@ReceivedDateFrom datetime,
	@ReceivedDateTo datetime,
	@Number integer = null,
	@HistologyRef varchar(20)= null,
	@SenderRef varchar(20)= null,
	@EnteredBy integer = null,
	@All integer = null
AS
	If @All = 0 BEGIN
		SELECT DISTINCT TOP 200
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
			LEFT JOIN DEFACPVWPSQL001.TSE_VLA.dbo.tlkpSpecies tblSpecies ON  Convert(varchar(10), tblSpecies.SpeciesID, 103) = Batch.Species
			LEFT JOIN UNION_BATCH ON Batch.ID = [UNION_BATCH].[BatchID]
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
			ISNULL(BatchDate, '1 January 1900') BETWEEN ISNULL(@SubmittedDateFrom, '1 January 1900') AND ISNULL(@SubmittedDateTo, GETDATE()+7) AND
			ISNULL(DateReceived, '1 January 1900') BETWEEN ISNULL(@ReceivedDateFrom, '1 January 1900') AND ISNULL(@ReceivedDateTo, GETDATE()+7) AND
			([UNION_BATCH].[HistologyRef]= @HistologyRef or @HistologyRef IS NULL) AND
			([UNION_BATCH].[SenderRef] = @SenderRef or  @SenderRef IS NULL) AND(
			([Batch].[ID] = @Number OR @Number  IS NULL) AND
			([Batch].[SubmittedBy]  = @EnteredBy OR @EnteredBy IS NULL) AND
			([Batch].[OtherSubmittedBy] = @SubmittedBy OR @SubmittedBy IS NULL) AND
			([Batch].[BatchStatus] = @Status OR @Status IS NULL))
		ORDER BY
			[Batch].[ID] DESC
	END
	ELSE
	BEGIN
			SELECT DISTINCT
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
				LEFT JOIN DEFACPVWPSQL001.TSE_VLA.dbo.tlkpSpecies tblSpecies ON  Convert(varchar(10), tblSpecies.SpeciesID, 103) = Batch.Species
				LEFT JOIN UNION_BATCH ON Batch.ID = [UNION_BATCH].[BatchID]
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
				ISNULL(BatchDate, '1 January 1900') BETWEEN ISNULL(@SubmittedDateFrom, '1 January 1900') AND ISNULL(@SubmittedDateTo, GETDATE()+7) AND
				ISNULL(DateReceived, '1 January 1900') BETWEEN ISNULL(@ReceivedDateFrom, '1 January 1900') AND ISNULL(@ReceivedDateTo, GETDATE()+7) AND
				([UNION_BATCH].[HistologyRef]= @HistologyRef or @HistologyRef IS NULL) AND
				([UNION_BATCH].[SenderRef] = @SenderRef or  @SenderRef IS NULL) AND(
				([Batch].[ID] = @Number OR @Number  IS NULL) AND
				([Batch].[SubmittedBy]  = @EnteredBy OR @EnteredBy IS NULL) AND
				([Batch].[OtherSubmittedBy] = @SubmittedBy OR @SubmittedBy IS NULL) AND
				([Batch].[BatchStatus] = @Status OR @Status IS NULL))
			ORDER BY
				[Batch].[ID] DESC
	END

GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetSearchBatchDetails  TO HistologyUser
GO

--- Create GetluProjects stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetluProjects]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetluProjects]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetluProjects AS
	DECLARE @ttblProjects TABLE
	(
		[ID] int IDENTITY(1,1),
		[Description] varchar(50),
		[IsActive] bit
	)
	
	SET NOCOUNT ON
	
	INSERT INTO @ttblProjects
	(
		[Description],
		[IsActive]
	)
	
	SELECT DISTINCT
		[Description],
		[IsActive]
	FROM 
		luProjects
	WHERE
		[IsActive] = 1
	ORDER BY 
		[Description]
		
	SELECT 
		[ID],
		[Description],
		[IsActive]
	FROM 
		@ttblProjects
	ORDER BY [Description]
	
	SET NOCOUNT OFF
	
	RETURN
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].[GetluProjects]  TO HistologyUser
GO

--- Create GetluContacts stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetluContacts]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetluContacts]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetluContacts
AS

DECLARE @ttblContacts TABLE
(
	[ID] int IDENTITY(1,1),
	[Description] varchar(50),
	[IsActive] bit
)

SET NOCOUNT ON


INSERT INTO @ttblContacts
(
	[Description] ,
	[IsActive]
)
SELECT DISTINCT
	[Description] ,
	[IsActive]
FROM
	 luContacts
WHERE
	[IsActive] = 1
ORDER BY
	[Description]

SELECT 
	[ID],
	[Description],
	[IsActive]
FROM 
	@ttblContacts
ORDER BY
	[Description]

SET NOCOUNT OFF
	
RETURN
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].[GetluContacts]  TO HistologyUser
GO

--- Create EditAnimalSenderRef stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[EditAnimalSenderRef]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[EditAnimalSenderRef]
GO

CREATE PROCEDURE EditAnimalSenderRef 

	@SenderRef varchar(20),
	@NewSenderRef varchar(20)

 AS
	DECLARE
	@RowCount int

	SELECT 
		SenderRef
	FROM
		Animal
	WHERE
		Animal.SenderRef = @SenderRef

	SET @RowCount = @@ROWCOUNT

	IF @RowCount = 0 BEGIN
		RETURN 1
	END

	IF @RowCount >1 BEGIN
		RETURN 2
	END

	SELECT 
		SenderRef
	FROM
		Animal
	WHERE
		Animal.SenderRef = @NewSenderRef  AND
		Animal.SenderRef <> @SenderRef

	SET @RowCount = @@ROWCOUNT
	
	IF @RowCount > 0 BEGIN
		RETURN 3
	END

	UPDATE Animal SET
		SenderRef = @NewSenderRef
	WHERE 
		SenderRef = @SenderRef
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO
 
GRANT  EXECUTE  ON [dbo].EditAnimalSenderRef  TO HistologyUser
GO

--- Create EditAnimalHistologyref stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[EditAnimalHistologyRef]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[EditAnimalHistologyRef]
GO

CREATE PROCEDURE EditAnimalHistologyRef 

	@SenderRef varchar(20),
	@NewHistologyRef varchar(20)

 AS
	DECLARE
	@RowCount int

	SELECT 
		SenderRef
	FROM
		Animal
	WHERE
		Animal.SenderRef = @SenderRef

	SET @RowCount = @@ROWCOUNT

	IF @RowCount = 0 BEGIN
		RETURN 1
	END

	IF @RowCount >1 BEGIN
		RETURN 2
	END

	IF  @NewHistologyRef <> '' BEGIN

		SELECT 
			HistologyRef
		FROM
			Animal
		WHERE
			Animal.HistologyRef = @NewHistologyRef  AND
			Animal.SenderRef  <> @SenderRef

		SET @RowCount = @@ROWCOUNT
	
		IF @RowCount > 0 BEGIN
			RETURN 3
		END

	UPDATE Animal SET
		HistologyRef = @NewHistologyRef
	WHERE 
		SenderRef = @SenderRef
	END

	IF  @NewHistologyRef = '' BEGIN
		UPDATE Animal SET
			HistologyRef = NULL
		WHERE 
			SenderRef = @SenderRef
	END
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].EditAnimalHistologyRef  TO HistologyUser
GO

--- Create EditHistologyRef stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[EditResetHistologyRef]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[EditResetHistologyRef]
GO

SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO


CREATE PROCEDURE EditResetHistologyRef AS

DECLARE 
	@LastUpdate datetime,
	@CurrentDate datetime,
	@ErrorCode int,
	@Rowcount int

	
	SET @CurrentDate = GETDATE()

	SELECT @LastUpdate = (SELECT TOP 1  HistRefResetDate FROM HistRefResetLog ORDER BY ResetID DESC)


	IF  DATEDIFF(Year,@LastUpdate,@CurrentDate)>0 BEGIN

		UPDATE HistologyRefBackup SET
			HistologyRefBackup.Type = HistologyRef.Type,
			HistologyRefBackup.NextHistologyRef = HistologyRef.NextHistologyRef
		FROM
			HistologyRef
		
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
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].EditResetHistologyRef  TO HistologyUser
GO


--- Update GetAnimalBatchTissues stored procedure to search on tissue code and project description

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetAnimalBatchTissues]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetAnimalBatchTissues]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetAnimalBatchTissues 
	@SenderRef varchar(20),
	@HistologyRef varchar(20),
	@TissueCode varchar(20),
	@ProjectDesc varchar(50)
AS
	IF @SenderRef IS NULL
		SELECT
			[Batch].[ID], 
			CONVERT(varchar(30), [Batch].[BatchDate], 103) AS DateSubmitted,
			CONVERT(varchar(30), [Batch].[DateReceived], 103) AS DateReceived,
			[luTimeReceived].[Description] As TimeReceived,
			CONVERT(varchar(30), [Batch].[DateCompleted], 103) AS DateCompleted,
			CONVERT(varchar(30), [Batch].[CustomerReceivedDate], 103) AS CustomerReceivedDate,
			[luTissueType].[Description] AS TissueDescription,
			[BatchTissues].[NoPieces],
			[Animal].[HistologyRef],
			[Animal].[SenderRef],
			'WT' AS SubmittedAs
		FROM  
			[Batch] INNER JOIN
			[BatchSubmission] ON [Batch].[ID] = [BatchSubmission].[BatchID] INNER JOIN
			[BatchTissues] ON [BatchSubmission].[ID] = [BatchTissues].[BatchSubmissionID] INNER JOIN
			[luTissueType] ON [BatchTissues].[TissueCode] = [luTissueType].[Code] INNER JOIN
			[Animal] ON [BatchSubmission].[AnimalID] = [Animal].[ID] LEFT JOIN
			[luTimeReceived] ON [Batch].[TimeReceived] = [luTimeReceived].[Code] INNER JOIN
			[BatchSubmittedAs] ON [Batch].[ID] = [BatchSubmittedAs].[BatchID] INNER JOIN
			[luProjects] ON  [Batch].[ProjectContractCode] = [luProjects].[ID] 
		WHERE
			[Animal].[HistologyRef]	= @HistologyRef AND
			([luTissueType].[Code] = @TissueCode OR @TissueCode IS NULL) AND
			([luProjects].[Description] = @ProjectDesc OR @ProjectDesc IS NULL)
	ELSE
		SELECT
			[Batch].[ID], 
			CONVERT(varchar(30), [Batch].[BatchDate], 103) AS DateSubmitted,
			CONVERT(varchar(30), [Batch].[DateReceived], 103) AS DateReceived,
			[luTimeReceived].[Description] As TimeReceived, 
			CONVERT(varchar(30), [Batch].[DateCompleted], 103) AS DateCompleted,
			CONVERT(varchar(30), [Batch].[CustomerReceivedDate], 103) AS CustomerReceivedDate,
			[luTissueType].[Description] AS TissueDescription,
			[BatchTissues].[NoPieces],
			[Animal].[HistologyRef],
			[Animal].[SenderRef], 
			'WT' AS SubmittedAs
		FROM  
			[Batch] INNER JOIN
			[BatchSubmission] ON [Batch].[ID] = [BatchSubmission].[BatchID] INNER JOIN
			[BatchTissues] ON [BatchSubmission].[ID] = [BatchTissues].[BatchSubmissionID] INNER JOIN
			[luTissueType] ON [BatchTissues].[TissueCode] = [luTissueType].[Code] INNER JOIN
			[Animal] ON [BatchSubmission].[AnimalID] = [Animal].[ID] LEFT JOIN
			[luTimeReceived] ON [Batch].[TimeReceived] = [luTimeReceived].[Code]INNER JOIN
			[BatchSubmittedAs] ON [Batch].[ID] = [BatchSubmittedAs].[BatchID] INNER JOIN
			[luProjects] ON  [Batch].[ProjectContractCode] = [luProjects].[ID]
		WHERE
			[Animal].[SenderRef]=@SenderRef AND
			([luTissueType].[Code] = @TissueCode OR @TissueCode IS NULL) AND
			([luProjects].[Description] = @ProjectDesc OR @ProjectDesc IS NULL)
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetAnimalBatchTissues  TO HistologyUser
GO

--- Update GetAnimalBlockTissues to search on tissue code and project description

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetAnimalBlockTissues]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetAnimalBlockTissues]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetAnimalBlockTissues 
	@SenderRef varchar(20),
	@HistologyRef varchar(20),
	@TissueCode varchar(20),
	@ProjectDesc varchar(50)
AS

IF @HistologyRef IS NULL
BEGIN
	SELECT 
		[Batch].[ID], 
		CONVERT(varchar(30), [Batch].[BatchDate], 103) AS DateSubmitted,
		CONVERT(varchar(30), [Batch].[DateReceived], 103) AS DateReceived,
		[luTimeReceived].[Description] As TimeReceived,
		CONVERT(varchar(30), [Batch].[DateCompleted], 103) AS DateCompleted,
		CONVERT(varchar(30), [Batch].[CustomerReceivedDate], 103) AS CustomerReceivedDate,
		[BatchBlock].[BlockRef],
		[luTissueType].[Description] AS TissueDescription,
		[BlockTissues].[NoPieces],
		[Animal].[HistologyRef],
		[Animal].[SenderRef],
		CASE [BatchSubmittedAs].[Code] 
			WHEN '1' THEN 'WT'
			WHEN '2' THEN 'WB'
			WHEN '3' THEN 'SS'
			WHEN '4' THEN 'US'
			WHEN '5' THEN 'PC'
		ELSE
			''
		END
		AS SubmittedAs
	FROM  
		[Batch] INNER JOIN
		[BatchBlock] ON [Batch].[ID] = [BatchBlock].[BatchID] INNER JOIN
		[Animal] ON [BatchBlock].[AnimalID] = [Animal].[ID] INNER JOIN
		[BlockTissues] ON [BatchBlock].[ID] = [BlockTissues].[BlockID] INNER JOIN
		[luTissueType] ON [BlockTissues].[TissueCode] = [luTissueType].[Code] LEFT JOIN
		[luTimeReceived] ON [Batch].[TimeReceived] = [luTimeReceived].[Code] INNER JOIN
		[BatchSubmittedAs] ON [Batch].[ID] = [BatchSubmittedAs].[BatchID] INNER JOIN
		[luProjects] ON  [Batch].[ProjectContractCode] = [luProjects].[ID]
	WHERE
		[Animal].[SenderRef] = @SenderRef AND
		([luTissueType].[Code] = @TissueCode OR @TissueCode IS NULL) AND
		([luProjects].[Description] = @ProjectDesc OR @ProjectDesc IS NULL)
END
ELSE
	SELECT 
		[Batch].[ID], 
		CONVERT(varchar(30), [Batch].[BatchDate], 103) AS DateSubmitted,
		CONVERT(varchar(30), [Batch].[DateReceived], 103) AS DateReceived,
		[luTimeReceived].[Description] As TimeReceived,
		CONVERT(varchar(30), [Batch].[DateCompleted], 103) AS DateCompleted,
		CONVERT(varchar(30), [Batch].[CustomerReceivedDate], 103) AS CustomerReceivedDate,
		[BatchBlock].[BlockRef],
		[luTissueType].[Description] AS TissueDescription,
		[BlockTissues].[NoPieces],
		[Animal].[HistologyRef],
		[Animal].[SenderRef],
		CASE [BatchSubmittedAs].[Code] 
			WHEN '1' THEN 'WT'
			WHEN '2' THEN 'WB'
			WHEN '3' THEN 'SS'
			WHEN '4' THEN 'US'
			WHEN '5' THEN 'PC'
		ELSE
			''
		END
		AS SubmittedAs
	FROM  
		[Batch] INNER JOIN
		[BatchBlock] ON [Batch].[ID] = [BatchBlock].[BatchID] INNER JOIN
		[Animal] ON [BatchBlock].[AnimalID] = [Animal].[ID] INNER JOIN
		[BlockTissues] ON [BatchBlock].[ID] = [BlockTissues].[BlockID] INNER JOIN
		[luTissueType] ON [BlockTissues].[TissueCode] = [luTissueType].[Code] LEFT JOIN
		[luTimeReceived] ON [Batch].[TimeReceived] = [luTimeReceived].[Code] INNER JOIN
		[BatchSubmittedAs] ON [Batch].[ID] = [BatchSubmittedAs].[BatchID] INNER JOIN
		[luProjects] ON  [Batch].[ProjectContractCode] = [luProjects].[ID]
	WHERE
		[Animal].[HistologyRef] = @HistologyRef AND
		([luTissueType].[Code] = @TissueCode OR @TissueCode IS NULL) AND
		([luProjects].[Description] = @ProjectDesc OR @ProjectDesc IS NULL)

GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetAnimalBlockTissues  TO HistologyUser
GO

--- Create HistRefResetLog table

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[HistRefResetLog]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[HistRefResetLog]
GO

CREATE TABLE [dbo].[HistRefResetLog] (
	[ResetID] [int] IDENTITY (1, 1) NOT NULL ,
	[HistRefResetDate] [datetime] NOT NULL 
) ON [PRIMARY]
GO

INSERT INTO HistRefResetLog VALUES('01/01/1990')

--- Update GetProjectsArea to order by description
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetProjectsArea]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetProjectsArea]
GO

SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetProjectsArea
	@Area varchar(10)
AS

SELECT 
	[ID],
	[Description],
	[IsActive]
FROM
	luProjects
WHERE 
	[Area]=@Area AND [IsActive] = 1
ORDER BY 
	[Description]
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetProjectsArea  TO HistologyUser
GO

--- Update GetTestRows to take the project description as parameter
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetTestRows]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetTestRows]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetTestRows
	@ProjectContractDesc varchar(50) = null,
	@BatchType bit =0
AS


SELECT dbo.Batch.ProjectContractCode, dbo.Batch.SubmittedArea
FROM  dbo.AntibodiesTCCodes FULL OUTER JOIN
               dbo.BlockAntibodies ON dbo.AntibodiesTCCodes.TestID = dbo.BlockAntibodies.ID FULL OUTER JOIN
               dbo.BatchBlock ON dbo.BlockAntibodies.BlockID = dbo.BatchBlock.ID FULL OUTER JOIN
               dbo.HistologyTCCodes FULL OUTER JOIN
               dbo.BlockHistology ON dbo.HistologyTCCodes.TestID = dbo.BlockHistology.ID ON dbo.BatchBlock.ID = dbo.BlockHistology.BlockID FULL OUTER JOIN
               dbo.BlockStain FULL OUTER JOIN
               dbo.SpecialStainTCCodes ON dbo.BlockStain.ID = dbo.SpecialStainTCCodes.TestID ON dbo.BatchBlock.ID = dbo.BlockStain.BlockID FULL OUTER JOIN
               dbo.Batch ON dbo.BatchBlock.BatchID = dbo.Batch.ID INNER JOIN 
	  dbo.luProjects ON dbo.Batch.ProjectContractCode = dbo.luProjects.[ID]
WHERE    ( (dbo.AntibodiesTCCodes.TestID IS NOT NULL) OR
                      (dbo.HistologyTCCodes.TestID IS NOT NULL) OR
                      (dbo.SpecialStainTCCodes.TestID IS NOT NULL)) AND
			((dbo.BlockAntibodies.dispatched = 1) OR
		      (dbo.BlockHistology.dispatched = 1) OR
			(dbo.BlockStain.dispatched = 1)) AND
    (( dbo.luProjects.[Description] = @ProjectContractDesc OR @ProjectContractDesc IS NULL )) AND
		dbo.batch.batchtype = @BatchType
GROUP BY dbo.Batch.ProjectContractCode, dbo.Batch.SubmittedArea

/*added set of brackets around in the */
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetTestRows  TO HistologyUser
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[UNION_BATCH]') and OBJECTPROPERTY(id, N'IsView') = 1)
drop view [dbo].[UNION_BATCH]
GO

SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO



CREATE VIEW dbo.UNION_BATCH
AS

SELECT DISTINCT
dbo.BatchSubmission.BatchID, dbo.Animal.SenderRef, dbo.Animal.HistologyRef, dbo.Animal.ID, dbo.Animal.PMDate
FROM         dbo.Animal RIGHT OUTER JOIN
                      dbo.BatchSubmission ON dbo.Animal.ID = dbo.BatchSubmission.AnimalID
GROUP BY dbo.BatchSubmission.BatchID, dbo.Animal.SenderRef, dbo.Animal.HistologyRef, dbo.Animal.ID, dbo.Animal.PMDate
HAVING      NOT dbo.BatchSubmission.BatchID IS NULL
UNION
SELECT DISTINCT 
dbo.BatchBlock.BatchID, dbo.Animal.SenderRef, dbo.Animal.HistologyRef, dbo.Animal.ID, dbo.Animal.PMDate
FROM         dbo.Animal RIGHT OUTER JOIN
                      dbo.BatchBlock ON dbo.Animal.ID = dbo.BatchBlock.AnimalID
GROUP BY dbo.BatchBlock.BatchID, dbo.Animal.SenderRef, dbo.Animal.HistologyRef, dbo.Animal.ID, dbo.Animal.PMDate
HAVING      NOT dbo.BatchBlock.BatchID IS NULL

GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

--- Update the GetAnimalsBySenderRef stored procedure to orderby SenderRef
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetAnimalsBySenderRef]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetAnimalsBySenderRef]
GO

SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetAnimalsBySenderRef 
	@SenderRef varchar(20)
AS

SELECT DISTINCT
	[Animal].[ID],
	[Animal].[SenderRef],
	[Animal].[HistologyRef],
	[Animal].[NextBlockRef],
	[Animal].[RowStamp],
	[Animal].[OnHold],
	[Animal].[PMDate]
FROM
	[Animal]	
WHERE
	(SenderRef LIKE '%' + @SenderRef + '%')
ORDER BY [Animal].[SenderRef]
RETURN
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetAnimalsBySenderRef  TO HistologyUser
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[EditluQCCode]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[EditluQCCode]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE EditluQCCode
	@Original_Code varchar(10),
	@Code varchar(10),
	@Description varchar(50),
	@IsActive bit,
	@UserID integer	
AS
    DECLARE 
	@ErrorCode int, 
	@RowsUpdated int,
	@columnName  varchar(50),
	@columnValue varchar(500),
	@columnCount int,
	@oldColumnValue varchar(500), 
	@oldColumnValue1 varchar(500),
	@oldColumnValue2 varchar(500)
	
	SET @oldColumnValue = CONVERT(varchar(500), (SELECT Code  FROM luQCCode WHERE Code=@Code))
	SET @oldColumnValue1 = CONVERT(varchar(500), (SELECT Description  FROM luQCCode WHERE Code=@Code))
	SET @oldColumnValue2 = CONVERT(varchar(500), (SELECT IsActive  FROM luQCCode WHERE Code=@Code))

    UPDATE luQCCode SET
	[Code]=@Code,
	[Description]=@Description,
	[IsActive]=@IsActive
    WHERE
        [Code]=@Original_Code
        
	SET @columnCount = 1

	WHILE @columnCount < 4
	BEGIN
		SET @columnName = CASE @columnCount
			WHEN 1 THEN 'Code'
			WHEN 2 THEN 'Description'
			WHEN 3 THEN 'IsActive'
		END

		SET @columnVALUE = CASE @columnCount 
			WHEN 1 THEN CONVERT(varchar(500), @Code)
			WHEN 2 THEN CONVERT(varchar(500), @Description)
			WHEN 3 THEN CONVERT(varchar(500), @IsActive)
		END

		SET @oldColumnValue = CASE @columnCount
			WHEN 1 THEN @oldColumnValue
			WHEN 2 THEN @oldColumnValue1
			WHEN 3 THEN @oldColumnValue2
		END
		

		IF @oldColumnValue <> @columnValue AND NOT @oldColumnValue IS NULL  BEGIN
			INSERT INTO AuditLog 
				(ID, TableName, FieldName, LogDate, UserID, BeforeValue , AfterValue, Reason)
			VALUES
		        	(@Code, 'luQCCode', @columnName, getDate(), @UserID, @oldColumnValue, @columnValue, 'EditluQCCode')
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
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].EditluQCCode  TO HistologyUser
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[EditluPostFixation]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[EditluPostFixation]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE EditluPostFixation
	@Original_Code varchar(10),
	@Code varchar(10),
	@Description varchar(50),
	@IsActive bit,
	@UserID integer	
AS
    DECLARE 
	@ErrorCode int, 
	@RowsUpdated int,
	@columnName  varchar(50),
	@columnValue varchar(500),
	@columnCount int,
	@oldColumnValue varchar(500), 
	@oldColumnValue1 varchar(500),
	@oldColumnValue2 varchar(500)
	
	SET @oldColumnValue = CONVERT(varchar(500), (SELECT Code  FROM luPostFixation WHERE Code=@Code))
	SET @oldColumnValue1 = CONVERT(varchar(500), (SELECT Description  FROM luPostFixation WHERE Code=@Code))
	SET @oldColumnValue2 = CONVERT(varchar(500), (SELECT IsActive  FROM luPostFixation WHERE Code=@Code))

    UPDATE luPostFixation SET
	[Code]=@Code,
	[Description]=@Description,
	[IsActive]=@IsActive
    WHERE
        [Code]=@Original_Code
        
	SET @columnCount = 1

	WHILE @columnCount < 4
	BEGIN
		SET @columnName = CASE @columnCount
			WHEN 1 THEN 'Code'
			WHEN 2 THEN 'Description'
			WHEN 3 THEN 'IsActive'
		END

		SET @columnVALUE = CASE @columnCount 
			WHEN 1 THEN CONVERT(varchar(500), @Code)
			WHEN 2 THEN CONVERT(varchar(500), @Description)
			WHEN 3 THEN CONVERT(varchar(500), @IsActive)
		END

		SET @oldColumnValue = CASE @columnCount
			WHEN 1 THEN @oldColumnValue
			WHEN 2 THEN @oldColumnValue1
			WHEN 3 THEN @oldColumnValue2
		END
		

		IF @oldColumnValue <> @columnValue AND NOT @oldColumnValue IS NULL  BEGIN
			INSERT INTO AuditLog 
				(ID, TableName, FieldName, LogDate, UserID, BeforeValue , AfterValue, Reason)
			VALUES
		        	(@Code, 'luPostFixation', @columnName, getDate(), @UserID, @oldColumnValue, @columnValue, 'EditluPostFixation')
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
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].EditluPostFixation  TO HistologyUser
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[EditluProjectsExternal]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[EditluProjectsExternal]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE EditluProjectsExternal
	@ID integer,
	@Description varchar(50),
	@IsActive bit,
	@UserID integer	
AS
    DECLARE 
	@ErrorCode int, 
	@RowsUpdated int,
	@columnName  varchar(50),
	@columnValue varchar(500),
	@columnCount int,
	@oldColumnValue varchar(500), 
	@oldColumnValue1 varchar(500),
	@oldColumnValue2 varchar(500)
	
	SET @oldColumnValue = CONVERT(varchar(500), (SELECT [Description]  FROM luProjectsExternal WHERE ID=@ID))
	SET @oldColumnValue1 = CONVERT(varchar(500), (SELECT IsActive  FROM luProjectsExternal WHERE ID=@ID))

	UPDATE luProjectsExternal SET
		[Description]=@Description,
		[IsActive]=@IsActive
	WHERE
		[ID]=@ID
        
	SET @columnCount = 1

	WHILE @columnCount < 3
	BEGIN
		SET @columnName = CASE @columnCount
			WHEN 1 THEN 'Description'
			WHEN 2 THEN 'IsActive'
		END

		SET @columnVALUE = CASE @columnCount 
			WHEN 1 THEN CONVERT(varchar(500), @Description)
			WHEN 2 THEN CONVERT(varchar(500), @IsActive)
		END

		SET @oldColumnValue = CASE @columnCount
			WHEN 1 THEN @oldColumnValue
			WHEN 2 THEN @oldColumnValue1
		END
		

		IF @oldColumnValue <> @columnValue AND NOT @oldColumnValue IS NULL  BEGIN
			INSERT INTO AuditLog 
				(ID, TableName, FieldName, LogDate, UserID, BeforeValue , AfterValue, Reason)
			VALUES
		        	(@ID, 'luProjectsExternal', @columnName, getDate(), @UserID, @oldColumnValue, @columnValue, 'EditluProjectsExternal')
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
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO


GRANT  EXECUTE  ON [dbo].EditluProjectsExternal  TO HistologyUser
GO


if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[EditluProjectsHistopath]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[EditluProjectsHistopath]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE EditluProjectsHistopath
	@ID integer,
	@Description varchar(50),
	@IsActive bit,
	@UserID integer	
AS
    DECLARE 
	@ErrorCode int, 
	@RowsUpdated int,
	@columnName  varchar(50),
	@columnValue varchar(500),
	@columnCount int,
	@oldColumnValue varchar(500), 
	@oldColumnValue1 varchar(500),
	@oldColumnValue2 varchar(500)
	
	SET @oldColumnValue = CONVERT(varchar(500), (SELECT [Description]  FROM luProjectsHistopath WHERE ID=@ID))
	SET @oldColumnValue1 = CONVERT(varchar(500), (SELECT IsActive  FROM luProjectsHistopath WHERE ID=@ID))

	UPDATE luProjectsHistopath SET
		[Description]=@Description,
		[IsActive]=@IsActive
	WHERE
		[ID]=@ID
        
	SET @columnCount = 1

	WHILE @columnCount < 3
	BEGIN
		SET @columnName = CASE @columnCount
			WHEN 1 THEN 'Description'
			WHEN 2 THEN 'IsActive'
		END

		SET @columnVALUE = CASE @columnCount 
			WHEN 1 THEN CONVERT(varchar(500), @Description)
			WHEN 2 THEN CONVERT(varchar(500), @IsActive)
		END

		SET @oldColumnValue = CASE @columnCount
			WHEN 1 THEN @oldColumnValue
			WHEN 2 THEN @oldColumnValue1
		END
		

		IF @oldColumnValue <> @columnValue AND NOT @oldColumnValue IS NULL  BEGIN
			INSERT INTO AuditLog 
				(ID, TableName, FieldName, LogDate, UserID, BeforeValue , AfterValue, Reason)
			VALUES
		        	(@ID, 'luProjectsHistopath', @columnName, getDate(), @UserID, @oldColumnValue, @columnValue, 'EditluProjectsHistopath')
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
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].EditluProjectsHistopath  TO HistologyUser
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[EditluProjectsMouseHouse]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[EditluProjectsMouseHouse]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE EditluProjectsMouseHouse
	@ID integer,
	@Description varchar(50),
	@IsActive bit,
	@UserID integer	
AS
    DECLARE 
	@ErrorCode int, 
	@RowsUpdated int,
	@columnName  varchar(50),
	@columnValue varchar(500),
	@columnCount int,
	@oldColumnValue varchar(500), 
	@oldColumnValue1 varchar(500),
	@oldColumnValue2 varchar(500)
	
	SET @oldColumnValue = CONVERT(varchar(500), (SELECT [Description]  FROM luProjectsMouseHouse WHERE ID=@ID))
	SET @oldColumnValue1 = CONVERT(varchar(500), (SELECT IsActive  FROM luProjectsMouseHouse WHERE ID=@ID))

	UPDATE luProjectsMouseHouse SET
		[Description]=@Description,
		[IsActive]=@IsActive
	WHERE
		[ID]=@ID
        
	SET @columnCount = 1

	WHILE @columnCount < 3
	BEGIN
		SET @columnName = CASE @columnCount
			WHEN 1 THEN 'Description'
			WHEN 2 THEN 'IsActive'
		END

		SET @columnVALUE = CASE @columnCount 
			WHEN 1 THEN CONVERT(varchar(500), @Description)
			WHEN 2 THEN CONVERT(varchar(500), @IsActive)
		END

		SET @oldColumnValue = CASE @columnCount
			WHEN 1 THEN @oldColumnValue
			WHEN 2 THEN @oldColumnValue1
		END
		

		IF @oldColumnValue <> @columnValue AND NOT @oldColumnValue IS NULL  BEGIN
			INSERT INTO AuditLog 
				(ID, TableName, FieldName, LogDate, UserID, BeforeValue , AfterValue, Reason)
			VALUES
		        	(@ID, 'luProjectsMouseHouse', @columnName, getDate(), @UserID, @oldColumnValue, @columnValue, 'EditluProjectsMouseHouse')
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
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].EditluProjectsMouseHouse  TO HistologyUser
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[EditluProjectsOtherVLA]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[EditluProjectsOtherVLA]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE EditluProjectsOtherVLA
	@ID integer,
	@Description varchar(50),
	@IsActive bit,
	@UserID integer	
AS
    DECLARE 
	@ErrorCode int, 
	@RowsUpdated int,
	@columnName  varchar(50),
	@columnValue varchar(500),
	@columnCount int,
	@oldColumnValue varchar(500), 
	@oldColumnValue1 varchar(500),
	@oldColumnValue2 varchar(500)
	
	SET @oldColumnValue = CONVERT(varchar(500), (SELECT [Description]  FROM luProjectsOtherVLA WHERE ID=@ID))
	SET @oldColumnValue1 = CONVERT(varchar(500), (SELECT IsActive  FROM luProjectsOtherVLA WHERE ID=@ID))

	UPDATE luProjectsOtherVLA SET
		[Description]=@Description,
		[IsActive]=@IsActive
	WHERE
		[ID]=@ID
        
	SET @columnCount = 1

	WHILE @columnCount < 3
	BEGIN
		SET @columnName = CASE @columnCount
			WHEN 1 THEN 'Description'
			WHEN 2 THEN 'IsActive'
		END

		SET @columnVALUE = CASE @columnCount 
			WHEN 1 THEN CONVERT(varchar(500), @Description)
			WHEN 2 THEN CONVERT(varchar(500), @IsActive)
		END

		SET @oldColumnValue = CASE @columnCount
			WHEN 1 THEN @oldColumnValue
			WHEN 2 THEN @oldColumnValue1
		END
		

		IF @oldColumnValue <> @columnValue AND NOT @oldColumnValue IS NULL  BEGIN
			INSERT INTO AuditLog 
				(ID, TableName, FieldName, LogDate, UserID, BeforeValue , AfterValue, Reason)
			VALUES
		        	(@ID, 'luProjectsOtherVLA', @columnName, getDate(), @UserID, @oldColumnValue, @columnValue, 'EditluProjectsOtherVLA')
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
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].EditluProjectsOtherVLA  TO HistologyUser
GO

--- Create GetAllImportedData stored procedure

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[GetAllImportedData]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[GetAllImportedData]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS OFF 
GO

CREATE PROCEDURE GetAllImportedData AS

SELECT
 [CPU REF] As HistologyRef,
 CONVERT(varchar(30), [Date sub to CPU],103) AS DateSubmitted,
 [Ext Ref] As SenderRef,
 [Project No] As Project,
 Species,
 Tissue,
 [Block No] As BlockRef,
 Comments
FROM
 IMPORTED2001EXTSUB

UNION

SELECT
 [CPU REF] As HistologyRef,
 CONVERT(varchar(30), [Date sub to CPU],103) AS DateSubmitted,
 [Ext Ref] As SenderRef,
 [Project No] As Project,
 Species,
 Tissue,
 [Block No] As BlockRef,
 Comments
FROM
 IMPORTED2001NEUROSUB

UNION

SELECT
 [CPU ref] As HistologyRef,
 CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
 [External Ref] As SenderRef,
 NULL As Project,
 NULL As Species,
 NULL As Tissue,
 [Block Ref] As BlockRef,
 Comments
FROM
 IMPORTED2002EXTSUB

UNION

SELECT
 NULL As HistologyRef,
 CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
 [External Ref] As SenderRef,
 NULL As Project,
 NULL As Species,
 NULL As Tissue,
 [Block Ref] As BlockRef,
 Comments
FROM
 IMPORTED2002EXTSUBNOCPU

UNION

SELECT
 NULL As HistologyRef,
 CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
 [MC Ref] As SenderRef,
 NULL As Project,
 NULL As Species,
 NULL As Tissue,
 [Block Ref] As BlockRef,
 Comments
FROM
 IMPORTED2002MOUSESUB

UNION

SELECT
 [CPU ref] As HistologyRef,
 CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
 [External ref] As SenderRef,
 NULL As Project,
 NULL As Species,
 NULL As Tissue,
 [Block Ref] As BlockRef,
 Comments
FROM
 IMPORTED2002NEUROSUB


UNION

SELECT
 [CPU ref] As HistologyRef,
 CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
 [External ref] As SenderRef,
 Project,
 Species,
 Tissues AS Tissue,
 [Block Ref] As BlockRef,
 Comments
FROM
 IMPORTED2003EXTSUB

UNION

SELECT
 [CPU ref] As HistologyRef,
 CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
 [MC ref] As SenderRef,
 NULL AS Project,
 NULL AS Species,
 NULL AS Tissue,
 NULL As BlockRef,
 Comments
FROM
 IMPORTED2003MOUSESUB

UNION

SELECT
 [CPU ref] As HistologyRef,
 CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
 [Ext ref] As SenderRef,
 Project AS Project,
 Species AS Species,
 Tissues AS Tissue,
 [Block ref] As BlockRef,
 Comments
FROM
 IMPORTED2003NEUROSUB

UNION

SELECT
 [Histo ref] As HistologyRef,
 CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
 [External ref] As SenderRef,
 Project AS Project,
 Species AS Species,
 Tissues AS Tissue,
 [Block ref] As BlockRef,
 Comments
FROM
 IMPORTED2004EXTSUB

UNION

SELECT
 [Histo ref] As HistologyRef,
 CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
 [Histo ref] As SenderRef,
 NULL AS Project,
 Species AS Species,
 Tissues AS Tissue,
 [Block ref] As BlockRef,
 Comments
FROM
 IMPORTED2004MOUSESUB

UNION

SELECT
 [Histo ref] As HistologyRef,
 CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
 [Ext ref] As SenderRef,
 [Project Number] AS Project,
 Species AS Species,
 Tissues AS Tissue,
 [Block ref] As BlockRef,
 Comments
FROM
 IMPORTED2004NEUROSUB

UNION

SELECT
 [Histo ref] As HistologyRef,
 CONVERT(varchar(30), [Date Submitted],103) AS DateSubmitted,
 [External ref] As SenderRef,
 [Project] AS Project,
 Species AS Species,
 Tissues AS Tissue,
 [Block ref] As BlockRef,
 Comments
FROM
 IMPORTED2005TBDIAGSUB

UNION

SELECT
 [CPU Ref] As HistologyRef,
 CONVERT(varchar(30), [Date Sub to CPU],103) AS DateSubmitted,
 [Ext ref] As SenderRef,
 [Project No] AS Project,
 Species AS Species,
 Tissue AS Tissue,
 [Block No] As BlockRef,
 Comments
FROM
 IMPORTEDICCSUBMI11999TO12JAN2001

UNION

SELECT
 [CPU Ref] As HistologyRef,
 CONVERT(varchar(30), [Date sub to CPU],103) AS DateSubmitted,
 [Ext Ref] As SenderRef,
 [Project No] AS Project,
 Species,
 Tissue,
 [Block No] AS BlockRef,
 Comments
FROM 
 IMPORTEDICCSUBMI1TISSUEONLYTO12THJAN2001
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].GetAllImportedData  TO HistologyUser
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[IMPORTED2001EXTSUB]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[IMPORTED2001EXTSUB]
GO

CREATE TABLE [dbo].[IMPORTED2001EXTSUB] (
	[ID] [float] NULL ,
	[CPU Ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date sub to CPU] [smalldatetime] NULL ,
	[Ext Ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Project No] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Species] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Fixation] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Sender's Loc] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date Demo req] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Wet] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Wax] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Slide] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Tissue] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Block No] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 1] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 2] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 3] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 4] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 5] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 6] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 7] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 8] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 9] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 10] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Comments] [varchar] (500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Demo return] [smalldatetime] NULL ,
	[Date Histo -> Path] [smalldatetime] NULL ,
	[ICC ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date ret from Path] [smalldatetime] NULL ,
	[Date ICC -> Path] [smalldatetime] NULL ,
	[Ext Report Date] [smalldatetime] NULL ,
	[Result] [varchar] (500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Box Number] [float] NULL ,
	[Date Expended/Discarded] [smalldatetime] NULL 
) ON [PRIMARY] 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[IMPORTED2001NEUROSUB]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[IMPORTED2001NEUROSUB]
GO

CREATE TABLE [dbo].[IMPORTED2001NEUROSUB] (
	[ID] [float] NULL ,
	[CPU Ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date sub to CPU] [smalldatetime] NULL ,
	[Ext Ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Project No] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Species] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Fixation] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Sender's Loc] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date Demo req] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Wet] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Wax] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Slide] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Tissue] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Block No] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 1] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 2] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 3] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 4] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 5] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 6] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 7] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 8] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 9] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 10] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Comments] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Demo return] [smalldatetime] NULL ,
	[Date Histo -> Path] [smalldatetime] NULL ,
	[ICC ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date ret from Path] [smalldatetime] NULL ,
	[Date ICC -> Path] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Ext Report Date] [smalldatetime] NULL ,
	[Result] [varchar] (500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Box Number] [float] NULL ,
	[Date Expended/Discarded] [smalldatetime] NULL 
) ON [PRIMARY] 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[IMPORTED2002EXTSUB]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[IMPORTED2002EXTSUB]
GO

CREATE TABLE [dbo].[IMPORTED2002EXTSUB] (
	[Autonumber] [float] NULL ,
	[ID1] [float] NULL ,
	[Entered by:init] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date submitted] [smalldatetime] NULL ,
	[ID] [float] NULL ,
	[ID2] [float] NULL ,
	[Wet] [bit] NOT NULL ,
	[Wax] [bit] NOT NULL ,
	[Slide] [bit] NOT NULL ,
	[External ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[CPU ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Block ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[EO] [bit] NOT NULL ,
	[Embed date] [smalldatetime] NULL ,
	[HE(BSE)] [bit] NOT NULL ,
	[Despatched] [smalldatetime] NULL ,
	[HE] [bit] NOT NULL ,
	[Despatched2] [smalldatetime] NULL ,
	[R145] [bit] NOT NULL ,
	[Despatched3] [smalldatetime] NULL ,
	[486] [bit] NOT NULL ,
	[Despatched4] [smalldatetime] NULL ,
	[Other stain] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Depatched5] [smalldatetime] NULL ,
	[ICC Ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Comments] [varchar] (500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Box Number] [float] NULL ,
	[Date Expended/Discarded] [smalldatetime] NULL 
) ON [PRIMARY] 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[IMPORTED2002EXTSUBNOCPU]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[IMPORTED2002EXTSUBNOCPU]
GO

CREATE TABLE [dbo].[IMPORTED2002EXTSUBNOCPU] (
	[Autonumber] [float] NULL ,
	[Entered by init] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[ID1] [float] NULL ,
	[Date submitted] [smalldatetime] NULL ,
	[ID] [float] NULL ,
	[ID2] [float] NULL ,
	[Wet] [bit] NOT NULL ,
	[Wax] [bit] NOT NULL ,
	[Slide] [bit] NOT NULL ,
	[External ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Block ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[EO] [bit] NOT NULL ,
	[HE(BSE)] [bit] NOT NULL ,
	[Despatched] [smalldatetime] NULL ,
	[HE] [bit] NOT NULL ,
	[Despatched2] [smalldatetime] NULL ,
	[R145] [bit] NOT NULL ,
	[Despatched3] [smalldatetime] NULL ,
	[486] [bit] NOT NULL ,
	[Despatched4] [smalldatetime] NULL ,
	[Other stain] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Depatched5] [smalldatetime] NULL ,
	[ICC Ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Comments] [varchar] (500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Box Number] [float] NULL ,
	[Date Expended/Discarded] [smalldatetime] NULL 
) ON [PRIMARY] 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[IMPORTED2002MOUSESUB]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[IMPORTED2002MOUSESUB]
GO

CREATE TABLE [dbo].[IMPORTED2002MOUSESUB] (
	[Autonumber] [float] NULL ,
	[Entered by init] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Month/year] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[RACK No] [float] NULL ,
	[ID1] [float] NULL ,
	[Date submitted] [smalldatetime] NULL ,
	[ID2] [float] NULL ,
	[Date Cassetted] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date embedded] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Wet] [bit] NOT NULL ,
	[Wax] [bit] NOT NULL ,
	[Slide] [bit] NOT NULL ,
	[External ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[MC ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Block ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[EO] [bit] NOT NULL ,
	[HE(BSE)] [bit] NOT NULL ,
	[Despatched] [smalldatetime] NULL ,
	[HE] [bit] NOT NULL ,
	[Despatched2] [smalldatetime] NULL ,
	[R145] [bit] NOT NULL ,
	[Despatched3] [smalldatetime] NULL ,
	[486] [bit] NOT NULL ,
	[Despatched4] [smalldatetime] NULL ,
	[Special stain1] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Depatched5] [smalldatetime] NULL ,
	[ICC Ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Special stain 2] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Despatch 6] [smalldatetime] NULL ,
	[Special stain3] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Despatch 7] [smalldatetime] NULL ,
	[Comments] [varchar] (500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Box Number] [float] NULL ,
	[Date Expended/Discarded] [smalldatetime] NULL 
) ON [PRIMARY] 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[IMPORTED2002NEUROSUB]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[IMPORTED2002NEUROSUB]
GO

CREATE TABLE [dbo].[IMPORTED2002NEUROSUB] (
	[Autonumber] [float] NULL ,
	[Entered by init] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[ID1] [float] NULL ,
	[Date submitted] [smalldatetime] NULL ,
	[ID] [float] NULL ,
	[ID2] [float] NULL ,
	[Wet] [bit] NOT NULL ,
	[Wax] [bit] NOT NULL ,
	[Slide] [bit] NOT NULL ,
	[External ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[CPU ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Block ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[EO] [bit] NOT NULL ,
	[Date EO] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[HE(BSE)] [bit] NOT NULL ,
	[Despatched] [smalldatetime] NULL ,
	[HE] [bit] NOT NULL ,
	[Despatched2] [smalldatetime] NULL ,
	[R145] [bit] NOT NULL ,
	[Despatched3] [smalldatetime] NULL ,
	[486] [bit] NOT NULL ,
	[Despatched4] [smalldatetime] NULL ,
	[Other stain] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Depatched5] [smalldatetime] NULL ,
	[ICC Ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[datedifference] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Comments] [varchar] (500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Box Number] [float] NULL ,
	[Date Expended/Discarded] [smalldatetime] NULL 
) ON [PRIMARY] 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[IMPORTED2003EXTSUB]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[IMPORTED2003EXTSUB]
GO

CREATE TABLE [dbo].[IMPORTED2003EXTSUB] (
	[ID] [float] NULL ,
	[Project] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Entered by Initials] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date Submitted] [smalldatetime] NULL ,
	[Species] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Tissues] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Wet] [bit] NOT NULL ,
	[Wax] [bit] NOT NULL ,
	[External ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[CPU ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Block ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[E/O] [bit] NOT NULL ,
	[Embed date] [smalldatetime] NULL ,
	[HE(BSE)] [bit] NOT NULL ,
	[Despatched] [smalldatetime] NULL ,
	[HE] [bit] NOT NULL ,
	[Despatched 2] [smalldatetime] NULL ,
	[R145] [bit] NOT NULL ,
	[Despatched 3] [smalldatetime] NULL ,
	[Rb486] [bit] NOT NULL ,
	[Despatched 4] [smalldatetime] NULL ,
	[Other stain] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Despatched 5] [smalldatetime] NULL ,
	[ICC ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Comments] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Box Number] [float] NULL ,
	[Date Expended/Discarded] [smalldatetime] NULL 
) ON [PRIMARY]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[IMPORTED2003MOUSESUB]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[IMPORTED2003MOUSESUB]
GO

CREATE TABLE [dbo].[IMPORTED2003MOUSESUB] (
	[ID] [float] NULL ,
	[Entered by initials] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Rack number] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Project] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date submitted] [smalldatetime] NULL ,
	[Date cassetted] [smalldatetime] NULL ,
	[Date embedded] [smalldatetime] NULL ,
	[Wet] [bit] NOT NULL ,
	[Wax] [bit] NOT NULL ,
	[MC ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[CPU ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[E/O] [bit] NOT NULL ,
	[HE(BSE)] [bit] NOT NULL ,
	[Despatched] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[HE] [bit] NOT NULL ,
	[Despatched 2] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Special stain] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Despatched 5] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Special stain 2] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Despatched 6] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Special 3] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Despatched 7] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Comments] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Box Number] [float] NULL ,
	[Date Expended/Discarded] [smalldatetime] NULL 
) ON [PRIMARY]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[IMPORTED2003NEUROSUB]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[IMPORTED2003NEUROSUB]
GO

CREATE TABLE [dbo].[IMPORTED2003NEUROSUB] (
	[ID] [float] NULL ,
	[Entered by] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Project] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date submitted] [smalldatetime] NULL ,
	[Species] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Field1] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Tissues] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Wet] [bit] NOT NULL ,
	[Wax] [bit] NOT NULL ,
	[CPU ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Block ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[E/O] [bit] NOT NULL ,
	[Date E/O] [smalldatetime] NULL ,
	[HE(BSE)] [bit] NOT NULL ,
	[Despatched] [smalldatetime] NULL ,
	[HE] [bit] NOT NULL ,
	[Despatched 2] [smalldatetime] NULL ,
	[R145] [bit] NOT NULL ,
	[Despatched 3] [smalldatetime] NULL ,
	[Rb486] [bit] NOT NULL ,
	[Despatched 4] [smalldatetime] NULL ,
	[Other stain] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Despatched 5] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[ICC ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Comments] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Report] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Box Number] [float] NULL ,
	[Date Expended/Discarded] [smalldatetime] NULL ,
	[Ext Ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL 
) ON [PRIMARY]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[IMPORTED2004EXTSUB]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[IMPORTED2004EXTSUB]
GO

CREATE TABLE [dbo].[IMPORTED2004EXTSUB] (
	[ID] [float] NULL ,
	[Project] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Entered by] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date Submitted] [smalldatetime] NULL ,
	[Species] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Tissues] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Wet] [bit] NOT NULL ,
	[Wax] [bit] NOT NULL ,
	[Slides] [bit] NOT NULL ,
	[External ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Histo ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Block ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[E/O] [bit] NOT NULL ,
	[Embed date] [smalldatetime] NULL ,
	[HE(BSE)] [bit] NOT NULL ,
	[Despatched] [smalldatetime] NULL ,
	[H&E] [bit] NOT NULL ,
	[Despatched 2] [smalldatetime] NULL ,
	[R145] [bit] NOT NULL ,
	[Despatched 3] [smalldatetime] NULL ,
	[Rb 486] [bit] NOT NULL ,
	[Despatched 4] [smalldatetime] NULL ,
	[Other stain] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Despatched 5] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[ICC ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Comments] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Box Number] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date Expended / Discarded] [smalldatetime] NULL 
) ON [PRIMARY]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[IMPORTED2004NEUROSUB]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[IMPORTED2004NEUROSUB]
GO

CREATE TABLE [dbo].[IMPORTED2004NEUROSUB] (
	[ID] [float] NULL ,
	[Entered by] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Project Number] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date Submitted] [smalldatetime] NULL ,
	[Species] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Tissues] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Wet] [bit] NOT NULL ,
	[Wax] [bit] NOT NULL ,
	[Histo ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Block ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[E/O] [bit] NOT NULL ,
	[Date E/O] [smalldatetime] NULL ,
	[HE(BSE)] [bit] NOT NULL ,
	[Despatched] [smalldatetime] NULL ,
	[H&E] [bit] NOT NULL ,
	[Despatched 2] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[R145] [bit] NOT NULL ,
	[Despatched 3] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Rb 486] [bit] NOT NULL ,
	[Despatched 4] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Other stain] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Despatched 5] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[ICC ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Comments] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Report] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Box number] [float] NULL ,
	[Date Expended/Discarded] [smalldatetime] NULL ,
	[Ext Ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL 
) ON [PRIMARY]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[IMPORTED2005TBDIAGSUB]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[IMPORTED2005TBDIAGSUB]
GO

CREATE TABLE [dbo].[IMPORTED2005TBDIAGSUB] (
	[ID] [float] NULL ,
	[Project] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Entered by] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date Submitted] [smalldatetime] NULL ,
	[Species] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Tissues] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Wet] [bit] NOT NULL ,
	[Wax] [bit] NOT NULL ,
	[Slides] [bit] NOT NULL ,
	[External ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Histo ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Block ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[E/O] [bit] NOT NULL ,
	[Embed date] [smalldatetime] NULL ,
	[HE(BSE)] [bit] NOT NULL ,
	[Despatched] [smalldatetime] NULL ,
	[H&E] [bit] NOT NULL ,
	[Despatched 2] [smalldatetime] NULL ,
	[R145] [bit] NOT NULL ,
	[Despatched 3] [smalldatetime] NULL ,
	[Rb 486] [bit] NOT NULL ,
	[Despatched 4] [smalldatetime] NULL ,
	[Other stain] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Despatched 5] [smalldatetime] NULL ,
	[ICC ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Comments] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Box Number] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date Expended / Discarded] [smalldatetime] NULL 
) ON [PRIMARY]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[IMPORTEDICCSUBMI11999TO12JAN2001]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[IMPORTEDICCSUBMI11999TO12JAN2001]
GO

CREATE TABLE [dbo].[IMPORTEDICCSUBMI11999TO12JAN2001] (
	[ID] [float] NULL ,
	[CPU Ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date sub to CPU] [smalldatetime] NULL ,
	[Ext Ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Project No] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Species] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Fixation] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Sender's Loc] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date Demo req] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Wet] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Wax] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Slide] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Tissue] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Block No] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 1] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 2] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 3] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 4] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 5] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 6] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 7] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 8] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 9] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 10] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Comments] [varchar] (500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Demo return] [smalldatetime] NULL ,
	[Date Histo -> Path] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[ICC ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date ret from Path] [smalldatetime] NULL ,
	[Date ICC -> Path] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Ext Report Date] [smalldatetime] NULL ,
	[Result] [varchar] (500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL 
) ON [PRIMARY] 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[IMPORTEDICCSUBMI1TISSUEONLYTO12THJAN2001]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[IMPORTEDICCSUBMI1TISSUEONLYTO12THJAN2001]
GO

CREATE TABLE [dbo].[IMPORTEDICCSUBMI1TISSUEONLYTO12THJAN2001] (
	[ID] [float] NULL ,
	[CPU Ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date sub to CPU] [smalldatetime] NULL ,
	[Ext Ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Project No] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Species] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Fixation] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Sender's Loc] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date Demo req] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Wet] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Wax] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Slide] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Tissue] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Block No] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 1] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 2] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 3] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 4] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 5] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 6] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 7] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 8] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 9] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Stain 10] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Comments] [varchar] (500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Demo return] [smalldatetime] NULL ,
	[Date Histo -> Path] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[ICC ref] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Date ret from Path] [smalldatetime] NULL ,
	[Date ICC -> Path] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Ext Report Date] [smalldatetime] NULL ,
	[Result] [varchar] (500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[Box Number] [float] NULL ,
	[Date Expended/Discarded] [smalldatetime] NULL 
) ON [PRIMARY] 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[IMPORTED2004MOUSESUB]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[IMPORTED2004MOUSESUB]
GO

CREATE TABLE [dbo].[IMPORTED2004MOUSESUB] (
 [ID] [float] NULL ,
 [Rack] [float] NULL ,
 [Entered by] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
 [Project No] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
 [Date submitted] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
 [Species] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
 [Tissues] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
 [Wet] [bit] NOT NULL ,
 [Wax] [bit] NOT NULL ,
 [Histo ref] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
 [Block ref] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
 [E/O] [bit] NOT NULL ,
 [Date E/O] [smalldatetime] NULL ,
 [HE(BSE)] [bit] NOT NULL ,
 [Despatched] [smalldatetime] NULL ,
 [Rb 486] [bit] NOT NULL ,
 [Despatched 2] [smalldatetime] NULL ,
 [Special stain] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
 [Despatched 3] [smalldatetime] NULL ,
 [Special stain 2] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
 [Despatched 4] [smalldatetime] NULL ,
 [ICC ref] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
 [Comments] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
 [Report] [nvarchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
 [Date Expended/Discarded] [smalldatetime] NULL 
) ON [PRIMARY]
GO


-- Alter the QCNote text field to be varchar(4000)
ALTER TABLE QCNotes
	ALTER COLUMN QCText varchar(4000) NULL;

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[EditQCNote]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[EditQCNote]
GO

SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

CREATE PROCEDURE EditQCNote
	@QCNoteRef integer,
	@QCText varchar(4000),
	@RowStamp timestamp,
	@UserID int
AS
DECLARE 
	@ErrorCode integer,
	@RowsUpdated integer,
	@columnValue varchar(8000),
	@columnCount int,
	@oldColumnValue varchar(8000)

	SET @oldColumnValue = CONVERT(varchar(8000), (SELECT QCText  FROM QCNotes WHERE ID=@QCNoteRef))
	SET @columnValue  = CONVERT(varchar(8000), @QCText)

	UPDATE QCNotes SET
		QCText=@QCText
	WHERE
		ID=@QCNoteRef AND RowStamp=@RowStamp

	SET @RowsUpdated = @@ROWCOUNT

	IF @RowsUpdated = 0 BEGIN
		RETURN 1
	END

	IF @oldColumnValue <> @columnValue  AND NOT @oldColumnValue IS NULL BEGIN
		INSERT INTO AuditLog 
			(ID, TableName, FieldName, LogDate, UserID, BeforeValue , AfterValue, Reason)
		VALUES
	        	(@QCNoteRef, 'QCNotes', 'QCText', getDate(), @UserID, @oldColumnValue, @columnValue, 'EditQCNote')
	END

	SELECT @ErrorCode = @@ERROR

	IF @ErrorCode = 0 BEGIN
		IF @RowsUpdated = 0 BEGIN
		    RETURN -1
	END ELSE BEGIN
		    RETURN 0
	END
	END ELSE BEGIN
		RETURN @ErrorCode
	END
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

GRANT  EXECUTE  ON [dbo].[EditQCNote]  TO HistologyUser
GO

-- Create the Histologyref backup table

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[HistologyRefBackup]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[HistologyRefBackup]
GO

CREATE TABLE [dbo].[HistologyRefBackup] (
	[Type] [int] NOT NULL ,
	[NextHistologyRef] [varchar] (5) COLLATE Latin1_General_CI_AS NOT NULL 
) ON [PRIMARY]
GO

