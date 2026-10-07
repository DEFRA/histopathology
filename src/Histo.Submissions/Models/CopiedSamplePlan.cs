namespace Histo.Submissions.Models;

/// <summary>
/// One fully-resolved "copy this sample onto the new batch" instruction — used by
/// <c>ISubmissionRepository.CreateBatchWithCopiedSamplesAsync</c> so the whole copy (batch header,
/// test selections, every animal/submission/tissue) can run as a single DB transaction instead of
/// several independent writes that could leave a partially-created batch behind.
/// </summary>
public sealed class CopiedSamplePlan
{
    public required Animal SourceAnimal { get; init; }
    public required string NewSenderRef { get; init; }
    public required BatchSubmission SourceSubmission { get; init; }
    public required IReadOnlyList<Tissue> Tissues { get; init; }
}
