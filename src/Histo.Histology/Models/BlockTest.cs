namespace Histo.Histology.Models;

/// <summary>
/// One test record within a block — a histology, antibodies, or special stain test
/// awaiting or having received quality-control review and dispatch.
///
/// Legacy source: HistopathologyLib/clsBatchSummary.vb — <c>CreateTestSummaryData</c>
/// (in-memory grid shape) and HistopathologyLib/clsCheckBoxData.vb —
/// <c>UpdateBlockTablesDetails</c> (persistence, per <see cref="TestType"/>).
///
/// SIMPLIFIED: the legacy grid additionally supports selecting several test rows and
/// bulk-applying one set of field values to all of them in a single save. This model
/// and its supporting page only support editing one test at a time. Premium-charge
/// ("TC code") checkboxes per test are not ported — see <c>QualityData.cshtml.cs</c>.
/// </summary>
public sealed class BlockTest
{
    public int ID { get; init; }
    public int BlockID { get; init; }
    public string BlockRef { get; init; } = string.Empty;
    public string? HistologyRef { get; init; }

    /// <summary>One of the <see cref="TestType"/> constants.</summary>
    public string TestType { get; init; } = string.Empty;

    /// <summary>The test's pick-list code (e.g. the stain or antibody code).</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Display description of the test, for the worklist grid.</summary>
    public string? TestDetails { get; init; }

    /// <summary>"1" = passed, "2" = failed, null/empty = not yet tested.</summary>
    public string? Result { get; init; }

    public string? QCCode { get; init; }
    public bool QCNote { get; init; }
    public int? QCNoteRef { get; init; }
    public string? StainRef { get; init; }
    public bool Dispatched { get; init; }
    public DateTime? DispatchedDate { get; init; }
    public string? DispatchedBy { get; init; }
    public string? DispatchedTo { get; init; }
    public string? Comment { get; init; }
    public string? RemedialAction { get; init; }
    public string? ArchiveLocation { get; init; }
    public DateTime? ArchivedDate { get; init; }
    public string? ArchiveComment { get; init; }
    public int? NumberOfSlides { get; init; }
    public bool OnHold { get; init; }
    public bool Archived { get; init; }

    /// <summary>SQL Server rowversion — used for optimistic concurrency on update.</summary>
    public byte[]? RowStamp { get; init; }
}

/// <summary>Test type discriminator values. Legacy source: clsBatch.vb table constants.</summary>
public static class BlockTestType
{
    public const string Histology  = "Histology";
    public const string Antibodies = "Antibodies";
    public const string Stain      = "Stain";
}

/// <summary>Test result values. Legacy source: QualityData.aspx.vb ddlTestResult items.</summary>
public static class BlockTestResult
{
    public const string Passed = "1";
    public const string Failed = "2";
}
