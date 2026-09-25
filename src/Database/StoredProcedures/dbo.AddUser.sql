/****** Object:  StoredProcedure [dbo].[AddUser] ******/
/****** ONE-TIME DEPLOYMENT SCRIPT — Safe to re-run via pipeline ******/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Idempotent: DROP + CREATE ensures no conflicts on re-deployment
IF OBJECT_ID('[dbo].[AddUser]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[AddUser];
GO

CREATE PROCEDURE [dbo].[AddUser]
	@NTLogin varchar(25),
	@Name varchar(35),
	@Email varchar(60),
	@UserGroup varchar(10),
	@UserArea varchar(10),
	@Active bit
AS
	INSERT INTO [User]
        		([NTLogin], [Name], [Email], [UserGroup], [UserArea],  [Active])
	VALUES
		(@NTLogin, @Name, @Email, @UserGroup, @UserArea, @Active)
GO
