using Histo.Histology.Interfaces;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;

namespace Histo.Web.Pages;

/// <summary>
/// Detects whether a submission has any recorded comment/note, at header, block, tissue or
/// individual-test level — used to gate the "Print submission notes" button on
/// <c>PrintSubmission</c> and <c>ViewSubmissions</c>.
///
/// Replaces <see cref="Histo.Reporting.Services.SubmissionNotesDataSetBuilder.HasAnyNotesAsync"/>,
/// which called <c>GetAllBatchComments</c> and five child stored procedures that do not exist in
/// either the legacy database script or this repository's own <c>src/Database</c> project — the
/// same "invented stored procedure" defect class already found and fixed for tissue loading
/// elsewhere (see <c>docs/run-log-v2.md</c> — <c>GetTissuesByBlockID</c>/<c>GetTissuesBySubmissionID</c>).
/// This instead composes <see cref="IBatchService.GetByIdAsync"/>, <see cref="IBlockService.GetByBatchAsync"/>,
/// <see cref="Histo.Submissions.Interfaces.ISubmissionService.GetBatchSubmissionTissuesAsync"/> and
/// <see cref="Histo.Histology.Interfaces.IBlockTestService.GetByBatchAsync"/> — methods already
/// proven working elsewhere in this app — deliberately matching the EXACT scope of legacy's real
/// <c>GetAllBatchComments</c> (confirmed live via <c>sp_helptext</c>): header comments, block
/// HEADER comments only (not per-tissue-within-a-block comments — legacy's own
/// <c>GetBatchBlockComments</c> never covered that), submission-owned (Wet Tissue) tissue
/// comments, and antibody/histology/stain per-test comments (via
/// <see cref="Histo.Histology.Models.BlockTest"/>'s shared Comment/ArchiveComment fields).
/// </summary>
public static class SubmissionNotesHelper
{
    public static async Task<bool> HasAnyNotesAsync(
        int batchId,
        IBatchService batches,
        IBlockService blocks,
        ISubmissionService submissions,
        IBlockTestService tests,
        CancellationToken ct = default)
    {
        var batch = await batches.GetByIdAsync(batchId, ct);
        if (batch is not null && (!string.IsNullOrWhiteSpace(batch.Comments) || !string.IsNullOrWhiteSpace(batch.StatusComments)))
            return true;

        var blocksTask = blocks.GetByBatchAsync(batchId, ct);
        // Submission-owned (Wet Tissue) tissue comments only — legacy's GetAllBatchComments has no
        // SP for per-tissue comments WITHIN a block (GetBatchBlockComments only covers the block's
        // own header Comment/ArchiveComment), so block-owned tissue comments are deliberately not
        // checked here, matching legacy's real behaviour exactly.
        var submissionTissuesTask = submissions.GetBatchSubmissionTissuesAsync(batchId, ct);
        var testsTask = tests.GetByBatchAsync(batchId, ct);
        await Task.WhenAll(blocksTask, submissionTissuesTask, testsTask);

        return blocksTask.Result.Any(HasComment)
            || submissionTissuesTask.Result.Any(HasComment)
            || testsTask.Result.Any(HasComment);
    }

    private static bool HasComment(Histo.Histology.Models.Block b) =>
        !string.IsNullOrWhiteSpace(b.Comment) || !string.IsNullOrWhiteSpace(b.ArchiveComment);

    private static bool HasComment(Tissue t) =>
        !string.IsNullOrWhiteSpace(t.Comment) || !string.IsNullOrWhiteSpace(t.ArchiveComment);

    private static bool HasComment(Histo.Histology.Models.BlockTest t) =>
        !string.IsNullOrWhiteSpace(t.Comment) || !string.IsNullOrWhiteSpace(t.ArchiveComment);
}
