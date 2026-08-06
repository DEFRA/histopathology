/* Update Histopathology_Update_13042005.sql, to increased the site of the BeforeValue and AfterValue columns */
/* in the AuditLog table so that it can handle QC note changes that have varchar(8000) size.*/

ALTER TABLE AuditLog 
	ALTER COLUMN BeforeValue varchar(8000);
GO

ALTER TABLE AuditLog
	ALTER COLUMN AfterValue varchar(8000);
GO