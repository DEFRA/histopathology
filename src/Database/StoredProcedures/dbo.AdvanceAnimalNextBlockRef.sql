-- Object:  StoredProcedure [dbo].[AdvanceAnimalNextBlockRef]
-- ============================================================================
-- dbo.AdvanceAnimalNextBlockRef
-- ============================================================================
-- Moves Animal.NextBlockRef forward only. It is a high-water mark (legacy never
-- lowered it), so a block ref freed by a delete is not issued again. Unlike
-- EditAnimal it needs no RowStamp, so a stale stamp cannot silently drop it.
-- ============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('[dbo].[AdvanceAnimalNextBlockRef]', 'P') IS NOT NULL
        DROP PROCEDURE [dbo].[AdvanceAnimalNextBlockRef];
GO

CREATE PROCEDURE [dbo].[AdvanceAnimalNextBlockRef]
        @AnimalID int,
        @NextBlockRef varchar(4)
AS
BEGIN
        SET NOCOUNT ON;

        UPDATE dbo.Animal
           SET NextBlockRef = @NextBlockRef
         WHERE ID = @AnimalID
           AND ISNULL(TRY_CAST(NextBlockRef AS INT), 0) < TRY_CAST(@NextBlockRef AS INT);
END
GO

PRINT 'Deployed stored procedure dbo.AdvanceAnimalNextBlockRef';
GO
