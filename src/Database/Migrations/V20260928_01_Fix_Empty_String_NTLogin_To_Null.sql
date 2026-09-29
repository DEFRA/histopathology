-- AddUser/EditUser used to persist a blank NTLogin as '' rather than NULL. IX_User_NTLogin is a
-- filtered unique index (WHERE NTLogin IS NOT NULL), so '' is NOT excluded from the uniqueness
-- check the way a true NULL is — every second user added with a blank NTLogin collided on
-- INSERT ("Cannot insert duplicate key row ... IX_User_NTLogin ... duplicate key value is ()").
-- Safe to re-run: only touches rows that still have the empty-string value.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '--- V20260928_01: fixing empty-string NTLogin rows to NULL ---';
UPDATE dbo.[User]
SET NTLogin = NULL
WHERE NTLogin = '';
GO
PRINT '--- V20260928_01: completed ---';
GO
