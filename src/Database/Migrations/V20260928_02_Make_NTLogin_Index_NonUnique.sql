-- NTLogin is no longer an identity key going forward — Entra ID Email is now the sole unique
-- identifier for a User (see IX_User_Email). NTLogin is retained only as a legacy display/lookup
-- field, so its uniqueness is no longer enforced at the DB level. Replaces the filtered UNIQUE
-- index from V20260914_01 with a plain (non-unique) index — keeps GetUserByNTLogin's lookup
-- performance without blocking inserts on collisions (blank or otherwise).
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @isConstraint bit = 0;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_User_NTLogin' AND object_id = OBJECT_ID('dbo.[User]') AND is_unique = 1)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'IX_User_NTLogin' AND parent_object_id = OBJECT_ID('dbo.[User]'))
    BEGIN
        SET @isConstraint = 1;
        ALTER TABLE dbo.[User] DROP CONSTRAINT IX_User_NTLogin;
    END
    ELSE
        DROP INDEX IX_User_NTLogin ON dbo.[User];

    CREATE NONCLUSTERED INDEX IX_User_NTLogin ON dbo.[User] (NTLogin);
END;
GO
