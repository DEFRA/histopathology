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
    public string? Status { get; init; }
    public int? SubmittedBy { get; init; }
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

