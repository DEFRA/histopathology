-- =============================================================================
-- LOCAL_DEV_ONLY_alter_getbatchqcnotes.sql
--
-- !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
-- !! WARNING — LOCAL DEV (LocalDB) ONLY — DO NOT DEPLOY TO ANY ENVIRONMENT !!
-- !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
--
-- Purpose : Replaces the production body of GetBatchQCNotes with a local-dev
--           variant that removes the linked-server JOIN to DEFACPVWPSQL001.
--           The production SP joins a remote linked server for species lookups
--           which does not exist in a LocalDB dev instance.  This script makes
--           the SP work locally by returning Batch.Species as a raw numeric
--           code instead of joining the remote lookup.
--
-- DO NOT check this script into the pipeline or apply it to test, staging, or
-- prod.  The production SP must remain unchanged with its linked-server joins.
--
-- Applies : LOCAL DEV ONLY.
-- =============================================================================

USE Histology;
GO

-- SAFETY CHECK: refuse to run outside LocalDB
IF @@SERVERNAME NOT LIKE '%(LocalDB)%' AND @@SERVERNAME NOT LIKE '%LOCALDB%'
BEGIN
    RAISERROR('LOCAL_DEV_ONLY script attempted on a non-LocalDB server.  Aborting.', 16, 1);
    RETURN;
END
GO

ALTER PROCEDURE dbo.GetBatchQCNotes
    @QCNoteRef integer
AS
IF @QCNoteRef IS NULL BEGIN
    SELECT
        [Batch].[ID],
        [Batch].[SubmittedArea],
        [Batch].[BatchType],
        [Batch].[Species],
        [Batch].[ProjectContractCode],
        [BlockAntibodies].[QCNoteRef],
        [BlockAntibodies].[StainRef],
        [luProjects].[Description] AS ProjectDescription
    FROM
        [Batch]
        INNER JOIN [BatchBlock]       ON [BatchBlock].[BatchID]    = [Batch].[ID]
        INNER JOIN [BlockAntibodies]  ON [BlockAntibodies].[BlockID] = [BatchBlock].[ID]
        LEFT  JOIN [luProjects]       ON [luProjects].[ID]         = [Batch].[ProjectContractCode]
    WHERE [BlockAntibodies].[QCNoteRef] IS NOT NULL

    UNION

    SELECT
        [Batch].[ID],
        [Batch].[SubmittedArea],
        [Batch].[BatchType],
        [Batch].[Species],
        [Batch].[ProjectContractCode],
        [BlockHistology].[QCNoteRef],
        [BlockHistology].[StainRef],
        [luProjects].[Description] AS ProjectDescription
    FROM
        [Batch]
        INNER JOIN [BatchBlock]      ON [BatchBlock].[BatchID]   = [Batch].[ID]
        INNER JOIN [BlockHistology]  ON [BlockHistology].[BlockID] = [BatchBlock].[ID]
        LEFT  JOIN [luProjects]      ON [luProjects].[ID]        = [Batch].[ProjectContractCode]
    WHERE [BlockHistology].[QCNoteRef] IS NOT NULL

    UNION

    SELECT
        [Batch].[ID],
        [Batch].[SubmittedArea],
        [Batch].[BatchType],
        [Batch].[Species],
        [Batch].[ProjectContractCode],
        [BlockStain].[QCNoteRef],
        [BlockStain].[StainRef],
        [luProjects].[Description] AS ProjectDescription
    FROM
        [Batch]
        INNER JOIN [BatchBlock]  ON [BatchBlock].[BatchID]  = [Batch].[ID]
        INNER JOIN [BlockStain]  ON [BlockStain].[BlockID]  = [BatchBlock].[ID]
        LEFT  JOIN [luProjects]  ON [luProjects].[ID]       = [Batch].[ProjectContractCode]
    WHERE [BlockStain].[QCNoteRef] IS NOT NULL

    ORDER BY QCNoteRef DESC
END
ELSE BEGIN
    SELECT
        [Batch].[ID],
        [Batch].[SubmittedArea],
        [Batch].[BatchType],
        [Batch].[Species],
        [Batch].[ProjectContractCode],
        [BlockAntibodies].[QCNoteRef],
        [BlockAntibodies].[StainRef],
        [luProjects].[Description] AS ProjectDescription
    FROM
        [Batch]
        INNER JOIN [BatchBlock]       ON [BatchBlock].[BatchID]    = [Batch].[ID]
        INNER JOIN [BlockAntibodies]  ON [BlockAntibodies].[BlockID] = [BatchBlock].[ID]
        LEFT  JOIN [luProjects]       ON [luProjects].[ID]         = [Batch].[ProjectContractCode]
    WHERE [BlockAntibodies].[QCNoteRef] = @QCNoteRef

    UNION

    SELECT
        [Batch].[ID],
        [Batch].[SubmittedArea],
        [Batch].[BatchType],
        [Batch].[Species],
        [Batch].[ProjectContractCode],
        [BlockHistology].[QCNoteRef],
        [BlockHistology].[StainRef],
        [luProjects].[Description] AS ProjectDescription
    FROM
        [Batch]
        INNER JOIN [BatchBlock]      ON [BatchBlock].[BatchID]   = [Batch].[ID]
        INNER JOIN [BlockHistology]  ON [BlockHistology].[BlockID] = [BatchBlock].[ID]
        LEFT  JOIN [luProjects]      ON [luProjects].[ID]        = [Batch].[ProjectContractCode]
    WHERE [BlockHistology].[QCNoteRef] = @QCNoteRef

    UNION

    SELECT
        [Batch].[ID],
        [Batch].[SubmittedArea],
        [Batch].[BatchType],
        [Batch].[Species],
        [Batch].[ProjectContractCode],
        [BlockStain].[QCNoteRef],
        [BlockStain].[StainRef],
        [luProjects].[Description] AS ProjectDescription
    FROM
        [Batch]
        INNER JOIN [BatchBlock]  ON [BatchBlock].[BatchID]  = [Batch].[ID]
        INNER JOIN [BlockStain]  ON [BlockStain].[BlockID]  = [BatchBlock].[ID]
        LEFT  JOIN [luProjects]  ON [luProjects].[ID]       = [Batch].[ProjectContractCode]
    WHERE [BlockStain].[QCNoteRef] = @QCNoteRef

    ORDER BY QCNoteRef DESC
END
GO

PRINT 'LOCAL DEV ONLY: GetBatchQCNotes altered to remove linked-server joins.';
GO
