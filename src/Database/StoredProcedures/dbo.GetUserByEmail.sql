/****** Object:  StoredProcedure [dbo].[GetUserByEmail]    Script Date: 04/09/2026 18:03:46 ******/
/****** ONE-TIME DEPLOYMENT SCRIPT — Safe to re-run via pipeline ******/

SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER ON
GO

-- Idempotent: DROP + CREATE ensures no conflicts on re-deployment
IF OBJECT_ID('[dbo].[GetUserByEmail]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[GetUserByEmail];
GO

CREATE PROCEDURE [dbo].[GetUserByEmail]
	@email varchar(60)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT
		 [User].[ID],
		 [User].[Name],
		 [User].[UserGroup],
		 [luUserGroup].[Description] AS GroupName,
		 [User].[Email],
		 [User].[UserArea],
		 [luUserArea].[Description] AS AreaName,
		 [User].[Active]
	FROM
		 [User] 
		 INNER JOIN [luUserGroup] ON [User].[UserGroup] = [luUserGroup].[Code]
		 INNER JOIN [luUserArea] ON [User].[UserArea] = [luUserArea].[Code]
	WHERE
		 [User].[Email] = @email;
END
GO