-- Safe to re-run: each column is only dropped if it still exists, and the EditableLookup
-- row is only inserted if ID 20 isn't already registered.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '--- picklist-species-changes: dropping obsolete tlkpSpecies columns ---';
IF COL_LENGTH('dbo.tlkpSpecies', 'AllowRBSE') IS NOT NULL
  alter table [dbo].[tlkpSpecies] drop column [AllowRBSE];
IF COL_LENGTH('dbo.tlkpSpecies', 'AllowRSCRAP') IS NOT NULL
  alter table [dbo].[tlkpSpecies] drop column [AllowRSCRAP];
IF COL_LENGTH('dbo.tlkpSpecies', 'AllowSE') IS NOT NULL
  alter table [dbo].[tlkpSpecies] drop column [AllowSE];
IF COL_LENGTH('dbo.tlkpSpecies', 'ImageID') IS NOT NULL
  alter table [dbo].[tlkpSpecies] drop column [ImageID];
GO

PRINT '--- picklist-species-changes: registering Species in EditableLookup ---';
IF NOT EXISTS (SELECT 1 FROM [dbo].EditableLookup WHERE [ID] = 20)
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
	 values (20, 
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