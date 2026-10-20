-- Legacy flags every submission that is not Wet Tissue as Cassetted: GetBatchesToBeBlocked (Assign blocks) only
-- lists Cassetted = 0, GetBatchesForDispatch (Quality data) lists Received + Cassetted = 1.
-- Block-type submissions created by the migrated app were saved with Cassetted = 0 (only Pre Cassetted set it).
-- Idempotent.
UPDATE b
   SET Cassetted = 1
  FROM dbo.Batch b
 WHERE b.Cassetted = 0
   AND EXISTS (SELECT 1 FROM dbo.BatchSubmittedAs sa WHERE sa.BatchID = b.ID AND sa.Code <> '1');
GO
