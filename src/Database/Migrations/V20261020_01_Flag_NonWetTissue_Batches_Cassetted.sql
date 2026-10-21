-- Legacy flags every submission that is not Wet Tissue as Cassetted: GetBatchesToBeBlocked (Assign blocks) only
-- lists Cassetted = 0, GetBatchesForDispatch (Quality data) lists Received + Cassetted = 1.
-- Block-type submissions created by the migrated app were saved with Cassetted = 0 (only Pre Cassetted set it).
-- Only codes that resolve in luSubmittedAs are touched: an unresolved code is left alone, matching the app, which
-- saves such a batch as not cassetted. Re-runs via Deploy.sql are therefore stable.
UPDATE b
   SET Cassetted = 1
  FROM dbo.Batch b
 WHERE b.Cassetted = 0
   AND EXISTS (SELECT 1
                 FROM dbo.BatchSubmittedAs sa
                 INNER JOIN dbo.luSubmittedAs lu ON RTRIM(lu.Code) = RTRIM(sa.Code)
                WHERE sa.BatchID = b.ID
                  AND RTRIM(lu.Description) <> 'Wet Tissue');
GO
