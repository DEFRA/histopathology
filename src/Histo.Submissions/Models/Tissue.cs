namespace Histo.Submissions.Models;

/// <summary>
/// Represents a tissue record attached to a batch submission or block.
///
/// Legacy source: HistopathologyLib/clsTissue.vb — DataTable columns produced by
/// <c>GetBatchSubmissionDetailsByBatchID</c> (BATCH_TISSUES_TABLE, index 7).
///
/// The <see cref="KeyFieldName"/> indicates whether this tissue belongs to a
/// submission (<c>"BatchSubmissionID"</c>) or a block (<c>"BlockID"</c>) —
/// this maps to the stored procedure selection in the legacy code.
/// </summary>
public sealed class Tissue
{
    public int ID { get; init; }

    /// <summary>
    /// Foreign key to <c>BatchSubmission.ID</c> or <c>Block.ID</c>.
    /// The relevant column is determined by <see cref="TissueOwner"/>.
    /// </summary>
    public int OwnerID { get; init; }

    /// <summary>
    /// Identifies whether this tissue belongs to a submission or a block.
    /// </summary>
    public TissueOwner Owner { get; init; }

    public string TissueCode { get; init; } = string.Empty;
    public short NoPieces { get; init; }
    public string? Comment { get; init; }
    public string? ArchiveLocation { get; init; }
    public DateTime? ArchivedDate { get; init; }
    public string? ArchiveComment { get; init; }
    public byte[]? RowStamp { get; init; }
}

/// <summary>Identifies the owner type of a tissue record.</summary>
public enum TissueOwner
{
    /// <summary>Tissue belongs to a <see cref="BatchSubmission"/>.</summary>
    Submission,

    /// <summary>Tissue belongs to a block.</summary>
    Block,
}
