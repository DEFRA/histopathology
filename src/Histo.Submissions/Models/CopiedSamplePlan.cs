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

    /// <summary>The source sample's blocks, recreated against the new sample.</summary>
    public IReadOnlyList<CopiedBlockPlan> Blocks { get; init; } = [];
}

/// <summary>
/// One of a copied sample's blocks, flattened to the fields the <c>AddBlock</c> SP needs.
/// Deliberately not <c>Histo.Histology.Models.Block</c> — this module does not reference the
/// histology module, and the copy must stay inside the submissions transaction.
/// </summary>
public sealed class CopiedBlockPlan
{
    public required string BlockRef { get; init; }
    public string? CustomerRef { get; init; }
    public string? Comment { get; init; }
    public bool RepeatBlock { get; init; }
    public int Status { get; init; }
    public int Order { get; init; }
    public required IReadOnlyList<Tissue> Tissues { get; init; }
}
