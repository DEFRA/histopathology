namespace Histo.Submissions.Models;

/// <summary>
/// A single batch-level test selection row stored in
/// <c>tblBatchHistology</c>, <c>tblBatchAntibodies</c>, or <c>tblBatchStain</c>.
///
/// Legacy source: <c>clsBatch.vb</c> — rows from BATCH_HISTOLOGY_TABLE (1),
/// BATCH_ANTIBODIES_TABLE (2), BATCH_STAIN_TABLE (3) returned by
/// <c>GetCommonBatchTablesByID</c>.  Each row carries a pick-list <see cref="Code"/>
/// that matches the Code column of the corresponding lookup table
/// (<c>GetluHistology</c>, <c>GetluTSEAntibodies</c> / <c>GetluNonTSEAntibodies</c>,
/// <c>GetluSpecialStain</c>).
/// </summary>
public sealed class BatchTestSelectionRow
{
    public int    ID      { get; init; }
    public int    BatchID { get; init; }
    public string Code    { get; init; } = string.Empty;
}

/// <summary>
/// Batch-level test type selections: which histology types, antibodies, and
/// special stains apply to this submission as a whole.
///
/// These choices act as a template — each block inherits its available tests
/// from this batch-level selection.  Batch-level selections are stored in three
/// tables (<c>tblBatchHistology</c>, <c>tblBatchAntibodies</c>, <c>tblBatchStain</c>)
/// and returned as result-set indices 1–3 by <c>GetCommonBatchTablesByID</c>.
///
/// Legacy source: <c>BatchDetails.aspx</c> — "Select Histology and required tests"
/// section with three <c>CheckBoxList</c> controls (<c>chkblHistology</c>,
/// <c>chkblAntibodies</c>, <c>chkblSpecialStain</c>).
///
/// Business rules (mirror <c>BatchDetails.aspx.vb::CheckHistology</c>):
/// <list type="bullet">
/// <item>At least one histology type must be selected.</item>
/// <item>If histology code "3" (Special Stain) is selected, at least one stain must be chosen.</item>
/// <item>If histology code "4" (IHC-PrP) or "6" (IHC-Other) is selected, at least one antibody must be chosen.</item>
/// <item>For TSE submissions, histology code "6" (IHC-Other) is not available.</item>
/// <item>For NonTSE submissions, histology codes "4" (IHC-PrP) and "5" (H&amp;E BSE) are not available.</item>
/// </list>
/// </summary>
public sealed class BatchTestSelections
{
    public IReadOnlyList<BatchTestSelectionRow> Histology  { get; init; } = [];
    public IReadOnlyList<BatchTestSelectionRow> Antibodies { get; init; } = [];
    public IReadOnlyList<BatchTestSelectionRow> Stains     { get; init; } = [];

    /// <summary>True when at least one test type has been selected for this batch.</summary>
    public bool HasAny => Histology.Count > 0 || Antibodies.Count > 0 || Stains.Count > 0;
}

/// <summary>
/// Histology code constants that carry business meaning — used to drive
/// conditional display and validation on the Edit Batch Tests page.
///
/// Legacy source: <c>BatchDetails.aspx.vb::CheckHistology</c> and
/// <c>HideOptions</c> — which compare against the literal integer codes
/// stored in the <c>luHistology</c> database table.
/// </summary>
public static class HistologyCode
{
    /// <summary>EO.</summary>
    public const string EO = "1";

    /// <summary>H&amp;E.</summary>
    public const string HAndE = "2";

    /// <summary>Special Stain — selecting this requires at least one stain to be chosen.</summary>
    public const string SpecialStain = "3";

    /// <summary>IHC-PrP — selecting this requires at least one antibody to be chosen. TSE only.</summary>
    public const string IhcPrp = "4";

    /// <summary>H&amp;E (BSE) — available for TSE submissions only.</summary>
    public const string HeBse = "5";

    /// <summary>IHC-Other — selecting this requires at least one antibody to be chosen. NonTSE only.</summary>
    public const string IhcOther = "6";

    /// <summary>Archive — set when the block has been flagged for archiving. Not user-selectable on the block Tests form.</summary>
    public const string Archive = "7";
}
