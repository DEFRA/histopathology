$procs = @('GetSearchBatchDetails','GetAnimalBatchTissues','GetAnimalBlockTissues','GetUnUsedHistologyRefs','GetAllImportedData','GetluArchiveLocationAll','GetTestRows')
foreach ($p in $procs) {
  Write-Host "==== $p ====" -ForegroundColor Cyan
  sqlcmd -S "(localdb)\MSSQLLocalDB" -d histo_bacpack_03Aug2026 -E -W -Q "SELECT p.name, t.name as type, p.max_length, p.is_output FROM sys.parameters p JOIN sys.types t ON p.user_type_id = t.user_type_id WHERE p.object_id = OBJECT_ID(''dbo.$p'') ORDER BY p.parameter_id"
}
