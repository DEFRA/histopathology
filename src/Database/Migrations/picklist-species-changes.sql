  alter table [dbo].[tlkpSpecies]
  drop column [AllowRBSE] ,[AllowRSCRAP] ,[AllowSE] ,[ImageID]
  ;

  insert into [dbo].EditableLookup 
	( [ID] ,
	[TableName],
	[Description],
	[SelectStoredProcedure],
	[UpdateStoredProcedure],
	[InsertStoredProcedure],
    [DeleteStoredProcedure] 
	)
	 values ( 20, 
	 'tlkpSpecies', 
	 'Species',
	 'GettlkpSpecies',
	 'EdittlkpSpecies',
	 'AddtlkpSpecies',
	 'DeletetlkpSpecies'
	 )