/****** Object:  StoredProcedure [dbo].[GetAnimalSenderRef] ******/
-- ============================================================================
-- dbo.GetAnimalSenderRef
-- ============================================================================
-- Returns one row with the trimmed Sender Ref of an animal.
-- ============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('[dbo].[GetAnimalSenderRef]', 'P') IS NOT NULL
        DROP PROCEDURE [dbo].[GetAnimalSenderRef];
GO

CREATE PROCEDURE [dbo].[GetAnimalSenderRef]
        @AnimalID int
AS
BEGIN
        SET NOCOUNT ON;

        SELECT RTRIM(SenderRef) AS SenderRef
          FROM dbo.Animal
         WHERE ID = @AnimalID;
END
GO

PRINT 'Deployed stored procedure dbo.GetAnimalSenderRef';
GO
