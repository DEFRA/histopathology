-- ============================================================================
-- Deactivates the "Mouse Bioassay" and "Neuropath" user areas (luUserArea).
--
-- Per docs/Mouse-Bioassay-Neuropath-Removal-Analysis.md, section 3, item 1:
-- these areas are soft-deactivated (IsActive = 0), not hard-deleted, so that
-- existing historical data referencing them remains valid and reportable.
-- ============================================================================

BEGIN TRANSACTION;

UPDATE dbo.luUserArea
SET IsActive = 0
WHERE [Description] IN ('Mouse Bioassay', 'Neuropath')
  AND IsActive = 1;

IF EXISTS (
	SELECT 1
	FROM dbo.luUserArea
	WHERE [Description] IN ('Mouse Bioassay', 'Neuropath')
	  AND IsActive = 1
)
BEGIN
	RAISERROR('Failed to deactivate Mouse Bioassay / Neuropath user areas.', 16, 1);
	ROLLBACK TRANSACTION;
	RETURN;
END;

COMMIT TRANSACTION;
