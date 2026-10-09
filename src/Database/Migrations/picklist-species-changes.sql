-- Safe to re-run: each column is only dropped if it still exists, and the EditableLookup
-- row is only inserted if ID 20 isn't already registered.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '--- picklist-species-changes: dropping obsolete tlkpSpecies columns ---';
DECLARE @speciesColumns TABLE (ColumnName sysname NOT NULL);
INSERT INTO @speciesColumns (ColumnName)
VALUES ('AllowRBSE'), ('AllowRSCRAP'), ('AllowSE'), ('ImageID');

DECLARE @columnName sysname;
DECLARE columnCursor CURSOR LOCAL FAST_FORWARD FOR
SELECT ColumnName FROM @speciesColumns;

OPEN columnCursor;
FETCH NEXT FROM columnCursor INTO @columnName;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF COL_LENGTH('dbo.tlkpSpecies', @columnName) IS NOT NULL
        EXEC('ALTER TABLE [dbo].[tlkpSpecies] DROP COLUMN [' + @columnName + ']');

    FETCH NEXT FROM columnCursor INTO @columnName;
END
CLOSE columnCursor;
DEALLOCATE columnCursor;
GO

PRINT '--- picklist-species-changes: registering Species in EditableLookup ---';
DECLARE @SpeciesEditableLookupId int = 20;
IF NOT EXISTS (SELECT 1 FROM [dbo].EditableLookup WHERE [ID] = @SpeciesEditableLookupId)
BEGIN
  insert into [dbo].EditableLookup 
	([ID] ,
	[TableName],
	[Description],
	[SelectStoredProcedure],
	[UpdateStoredProcedure],
	[InsertStoredProcedure],
    [DeleteStoredProcedure] 
	)
	 values (@SpeciesEditableLookupId, 
	 'tlkpSpecies', 
	 'Species',
	 'GettlkpSpecies',
	 'EdittlkpSpecies',
	 'AddtlkpSpecies',
	 'DeletetlkpSpecies'
	 )
END
GO
PRINT '--- picklist-species-changes: completed ---';
GO