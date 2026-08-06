
Set ANSI_NULLS ON
Go
Create Proc Test as

IF NOT EXISTS (SELECT * FROM master..sysservers WHERE srvname = 'ORANGE')
BEGIN
	EXEC sp_addlinkedserver @server = 'ORANGE'

	EXEC sp_addlinkedsrvlogin 'ORANGE', 'false', NULL, 'HistologyUser', 'pass'
END

SELECT * FROM ORANGE.VLA_Histology.dbo.Batch

EXEC sp_droplinkedsrvlogin 'ORANGE', NULL

EXEC sp_dropserver @server = 'ORANGE'




