namespace Histo.Submissions.Models;

/// <summary>
/// One result row for the PM Date range search.
///
/// Legacy source: HistopathologyLib/clsAnimal.vb — <c>GetSearchPMDates</c>,
/// column shape from SearchPMDates.aspx grdSearchResults BoundColumns.
/// </summary>
public sealed class PmDateSearchResult
{
    public int ID { get; init; }
    public string? SenderRef { get; init; }
    public DateTime? PMDate { get; init; }
    public DateTime? BatchDate { get; init; }
    public DateTime? DateReceived { get; init; }
    public string? TimeReceived { get; init; }
    public DateTime? CompletedDate { get; init; }
    public DateTime? CustomerReceivedDate { get; init; }
}

/// <summary>
/// One matching animal/sample record for a sender-ref lookup.
///
/// Legacy source: HistopathologyLib/clsAnimal.vb — <c>GetAnimalsBySenderRef</c>,
/// column shape from SearchSample.aspx / SearchSender.aspx grdSenders BoundColumns.
/// </summary>
public sealed class SenderSearchResult
{
    public int ID { get; init; }
    public string? SenderRef { get; init; }
    public string? HistologyRef { get; init; }
}

/// <summary>
/// Search criteria for the multi-field Submission search.
///
/// Legacy source: SearchSubmissions.aspx.vb — private SearchCriteria class,
/// passed to <c>clsBatch.SearchBatchDetails</c>.
/// </summary>
public sealed class BatchSearchCriteria
{
    public int? SubmissionNumber { get; init; }
    public string? Status { get; init; }
    public string? ProjectContractCode { get; init; }
    public string? ContactName { get; init; }
    public string? Species { get; init; }
    public string? Fixation { get; init; }
    public string? SubmittedArea { get; init; }
    public int? SubmittedBy { get; init; }
    public int? EnteredBy { get; init; }
    public string? HistologyRef { get; init; }
    public string? SenderRef { get; init; }
    public DateTime? SubmittedDateFrom { get; init; }
    public DateTime? SubmittedDateTo { get; init; }
    public DateTime? ReceivedDateFrom { get; init; }
    public DateTime? ReceivedDateTo { get; init; }
}

/// <summary>
/// One result row for the status-filtered batch list pages
/// (Batches Received, Not Received, For Editing, For Dispatch, On Hold).
///
/// Legacy source: <c>GetReceivedBatches</c>, <c>GetBatchesNotReceived</c>,
/// <c>GetInProgressBatches</c>, <c>GetBatchesOnHold</c>, <c>GetBatchesForDispatch</c>
/// stored procedures — column shape from BatchesReceived/NotReceived/ForEditing/
/// ForDispatch/SubmissionsOnHold .aspx grdBatches BoundColumns.
/// </summary>
public sealed class BatchListResult
{
    public int ID { get; init; }
    public string? ProjectDescription { get; init; }
    public string? ContactDescription { get; init; }
    public string? Species { get; init; }
    public DateTime? BatchDate { get; init; }
    public DateTime? ReceivedDate { get; init; }
    public DateTime? CompletedDate { get; init; }
    public string? CustomerRef { get; init; }
    public string Status { get; init; } = string.Empty;
    /// <summary>Used by Batches Received only — checkbox column from the SP result set.</summary>
    public bool AllTissuesAssigned { get; init; }
}

/// <summary>
/// One result row of the multi-field Submission search.
///
/// Legacy source: <c>GetSearchBatchDetails</c> stored procedure, column shape
/// from SearchSubmissions.aspx grdSearchResults BoundColumns.
/// </summary>
public sealed class BatchSearchResult
{
    public int ID { get; init; }
    public string? ProjectDescription { get; init; }
    public string? ContactDescription { get; init; }
    public string? Species { get; init; }
    public DateTime? BatchDate { get; init; }
    public DateTime? DateReceived { get; init; }
    public DateTime? DateCompleted { get; init; }
    public DateTime? CustomerReceivedDate { get; init; }
    public string? Status { get; init; }
    /// <summary>
    /// The display name of the user who submitted the batch.
    /// The <c>GetSearchBatchDetails</c> SP returns this as a VARCHAR display name
    /// (e.g. "Gunjan Arya"), not the integer UserID. Hidden column — not rendered
    /// in the results grid but preserved for completeness.
    /// </summary>
    public string? SubmittedBy { get; init; }
}

/// <summary>
/// One row of the simplified "test items by project/date" listing.
///
/// Legacy source: HistopathologyLib/clsBatch.vb — <c>GetTestItemRows</c>
/// (<c>GetTestRows</c> stored procedure). This is a reduced-scope replacement
/// for the full SearchTest.aspx analytics engine — see search module report
/// for details of what was not ported.
/// </summary>
public sealed class TestItemRow
{
    public string? Description { get; init; }
    public int Count { get; init; }
}

/// <summary>
/// One result row for the Tissue Archive search mode of SearchArchiveLocation.
///
/// Legacy source: HistopathologyLib/clsAnimal.vb — <c>GetAnimalTissuesArchiveInformation</c>,
/// column shape from SearchArchiveLocation.aspx grdTissueArchive BoundColumns.
/// </summary>
public sealed class TissueArchiveInfo
{
    public int BatchID { get; init; }
    public string? TissueDescription { get; init; }
    public string? ArchiveLocation { get; init; }
    public DateTime? ArchivedDate { get; init; }
    public short? NoPieces { get; init; }
}

/// <summary>
/// One result row for the legacy <c>ViewSamples.aspx</c> tissue/block search — a standalone
/// (non-batch-scoped) search reached from the Home page, distinct from the in-progress-batch
/// sample list now served by <c>BatchBlockSummary.cshtml</c>.
///
/// Legacy source: HistopathologyLib/clsAnimal.vb — <c>GetAnimalTissues</c> (SP <c>GetAnimalBatchTissues</c>,
/// "Tissue Information" mode) and <c>GetAnimalBlockTissues</c> (SP <c>GetAnimalBlockTissues</c>,
/// "Block Information" mode). Column shape from ViewSamples.aspx grdTissuesGrid / grdResults BoundColumns.
/// <see cref="BlockRef"/> is populated only in Block Information mode (grdResults); it is
/// <c>null</c> for Tissue Information mode rows (grdTissuesGrid, which has no Block Ref column).
/// </summary>
public sealed class AnimalTissueSearchResult
{
    public int ID { get; init; }
    public DateTime? DateSubmitted { get; init; }
    public DateTime? DateReceived { get; init; }
    public string? TimeReceived { get; init; }
    public DateTime? DateCompleted { get; init; }
    public DateTime? CustomerReceivedDate { get; init; }
    public string? SubmittedAs { get; init; }
    public string? BlockRef { get; init; }
    public string? TissueDescription { get; init; }
    public int? NoPieces { get; init; }
}


