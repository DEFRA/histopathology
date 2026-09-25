/****** Object:  StoredProcedure [dbo].[EditUser] ******/
/****** ONE-TIME DEPLOYMENT SCRIPT — Safe to re-run via pipeline ******/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Idempotent: DROP + CREATE ensures no conflicts on re-deployment
IF OBJECT_ID('[dbo].[EditUser]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[EditUser];
GO

CREATE PROCEDURE [dbo].[EditUser]
	@ID integer,
	@NTLogin varchar(25),
	@Name varchar(35),
	@Email varchar(60),
	@UserGroup varchar(10),
	@UserArea varchar(10),
	@Active bit,
	@UserID integer	
AS
    DECLARE 
	@ErrorCode int, 
	@RowsUpdated int,
	@columnName  varchar(50),
	@columnValue varchar(500),
	@columnCount int,
	@oldColumnValue varchar(500), 
	@oldColumnValue1 varchar(500), @oldColumnValue2 varchar(500), @oldColumnValue3 varchar(500), @oldColumnValue4 varchar(500),
	@oldColumnValue5 varchar(500), @oldColumnValue6 varchar(500), @oldColumnValue7 varchar(500)
	
	SET @oldColumnValue2 = CONVERT(varchar(500), (SELECT NTLogin  FROM [User] WHERE ID=@ID))
	SET @oldColumnValue3 = CONVERT(varchar(500), (SELECT [Name]  FROM [User] WHERE ID=@ID))
	SET @oldColumnValue4 = CONVERT(varchar(500), (SELECT Email  FROM [User] WHERE ID=@ID))
	SET @oldColumnValue5 = CONVERT(varchar(500), (SELECT UserGroup  FROM [User] WHERE ID=@ID))
	SET @oldColumnValue6 = CONVERT(varchar(500), (SELECT UserArea  FROM [User] WHERE ID=@ID))
	SET @oldColumnValue7 = CONVERT(varchar(500), (SELECT Active  FROM [User] WHERE ID=@ID))
    
	UPDATE [User] SET
		[NTLogin] = @NTLogin,
		[Name]=@Name,
		[Email]=@Email,
		[UserGroup]=@UserGroup,
		[UserArea]=@UserArea,
		[Active]=@Active
	WHERE
		[ID]=@ID
        
	SET @columnCount = 1

	WHILE @columnCount < 8
	BEGIN
		SET @columnName = CASE @columnCount
			WHEN 1 THEN 'ID'
			WHEN 2 THEN 'NTLogin'
			WHEN 3 THEN 'Name'
			WHEN 4 THEN 'Email'
			WHEN 5 THEN 'UserGroup'
			WHEN 6 THEN 'UserArea'
			WHEN 7 THEN 'Active'
		END

		SET @columnVALUE = CASE @columnCount 
			WHEN 1 THEN CONVERT(varchar(500), @ID)
			WHEN 2 THEN CONVERT(varchar(500), @NTLogin)
			WHEN 3 THEN CONVERT(varchar(500), @Name)
			WHEN 4 THEN CONVERT(varchar(500), @Email)
			WHEN 5 THEN CONVERT(varchar(500), @UserGroup)
			WHEN 6 THEN CONVERT(varchar(500), @UserArea)
			WHEN 7 THEN CONVERT(varchar(500), @Active)
		END

		SET @oldColumnValue = CASE @columnCount
			WHEN 1 THEN CONVERT(varchar(500), @ID)
			WHEN 2 THEN @oldColumnValue2
			WHEN 3 THEN @oldColumnValue3
			WHEN 4 THEN @oldColumnValue4
			WHEN 5 THEN @oldColumnValue5
			WHEN 6 THEN @oldColumnValue6
			WHEN 7 THEN @oldColumnValue7
		END
		
		IF @oldColumnValue <> @columnValue  AND NOT @oldColumnValue IS NULL BEGIN
			INSERT INTO AuditLog 
				(ID, TableName, FieldName, LogDate, UserID, BeforeValue , AfterValue, Reason)
			VALUES
		        	(@ID, 'User', @columnName, getDate(), @UserID, @oldColumnValue, @columnValue, 'EditUser')
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
