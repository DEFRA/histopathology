namespace Histo.Submissions.Models;

/// <summary>
/// One row of legacy imported ICC_Sub data, returned by one of the year/type-specific
/// "Get{Year}{Type}SUB" stored procedures (or <c>GetAllImportedData</c> when no
/// specific table is selected).
///
/// Legacy source: HistopathologyLib/clsAnimal.vb — <c>GetImportedData</c>.
/// Column shape from ViewImportedData.aspx ImportedDataGrid BoundColumns.
/// </summary>
public sealed class ImportedDataRow
{
    public string? SenderRef { get; init; }
    public string? HistologyRef { get; init; }
    public string? BlockRef { get; init; }
    public string? Project { get; init; }
    public DateTime? DateSubmitted { get; init; }
    public string? Species { get; init; }
    public string? Tissue { get; init; }
    public string? Comments { get; init; }
}
