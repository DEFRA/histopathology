using System.Data;
using Dapper;
using Histo.Infrastructure;

namespace Histo.Reporting.Services;

/// <summary>
/// Assembles the single-table DataSet consumed by <see cref="Reports.QCNoteRenderer"/>.
///
/// Legacy equivalent: <c>clsQCNote.CreateReportDataset</c> (HistopathologyLib) which
/// queried three stored procedures and performed a Projects lookup, then passed the
/// assembled DataSet to Crystal Reports <c>QCNote.rpt</c>.
///
/// This service replicates the same DataSet schema (<c>QCNoteDataset.xsd — "Header" table</c>)
/// via Dapper + stored procedures, without any Crystal Reports dependency.
///
/// Expected output table:
/// <list type="bullet">
///   <item><b>Header</b> — single row: QCNoteRef, SubmissionNumber, Project, Species,
///         StainRef, QCText, CreatedBy, DateCreated</item>
/// </list>
///
/// Stored procedures called (legacy equivalents in <c>clsQCNote.CreateReportDataset</c>):
/// <list type="number">
///   <item><c>GetBatchQCNotes @QCNoteRef</c> → batch/block header row</item>
///   <item><c>GetQCNoteHistStainTestInformation @QCNoteRef</c> → QCText + author + date</item>
///   <item><c>GetQCNoteAntibodiesInformation @QCNoteRef, @SubmissionType</c> → merged into result 2</item>
///   <item><c>GetluProjectsAll</c> → Projects lookup (ID → Description); list type 19</item>
/// </list>
/// </summary>
public sealed class QCNoteDataSetBuilder
{
    private readonly IDbConnectionFactory _db;

    public QCNoteDataSetBuilder(IDbConnectionFactory db) => _db = db;

    /// <summary>
    /// Builds the report DataSet for the given QC Note reference.
    /// </summary>
    /// <param name="qcNoteRef">The QCNote ID sourced from the query string.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Populated DataSet containing a single table named <c>"Header"</c>.</returns>
    public async Task<DataSet> BuildAsync(int qcNoteRef, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        // ── 1. Batch/block header row ────────────────────────────────────────
        // GetBatchQCNotes returns the batch-level row for this QCNoteRef.
        // Columns used: QCNoteRef, ID (→ SubmissionNumber), BatchType, Species,
        //               StainRef, ProjectContractCode.
        var headerRows = (await conn.QueryAsync(
            "GetBatchQCNotes",
            new { QCNoteRef = qcNoteRef },
            commandType: CommandType.StoredProcedure))
            .Cast<IDictionary<string, object>>().ToList();

        string projectCode  = string.Empty;
        string species      = string.Empty;
        string stainRef     = string.Empty;
        string submissionNo = string.Empty;
        string qcNoteRefStr = string.Empty;
        int    batchType    = 0;

        if (headerRows.Count > 0)
        {
            var h = headerRows[0];
            qcNoteRefStr = Str(h, "QCNoteRef");
            submissionNo = Str(h, "ID");
            species      = Str(h, "Species");
            stainRef     = Str(h, "StainRef");
            projectCode  = Str(h, "ProjectContractCode");
            batchType    = h.TryGetValue("BatchType", out var bt) && bt is not null
                ? Convert.ToInt32(bt) : 0;
        }

        // ── 2. QCText + author + date ────────────────────────────────────────
        // GetQCNoteHistStainTestInformation returns rows for histology/stain notes.
        // GetQCNoteAntibodiesInformation returns rows for antibody notes.
        // Both are merged together — legacy clsQCNote.GetQCNoteTestInformation
        // appended antibody rows into the same DataTable.
        // We use only the first merged row for the report header (row 0).
        var histStainRows = (await conn.QueryAsync(
            "GetQCNoteHistStainTestInformation",
            new { QCNoteRef = qcNoteRef },
            commandType: CommandType.StoredProcedure))
            .Cast<IDictionary<string, object>>().ToList();

        var antibodyRows = (await conn.QueryAsync(
            "GetQCNoteAntibodiesInformation",
            new { QCNoteRef = qcNoteRef, SubmissionType = batchType },
            commandType: CommandType.StoredProcedure))
            .Cast<IDictionary<string, object>>().ToList();

        // Merge: histStain first, then antibodies (legacy ImportRow order).
        var allContentRows = histStainRows.Concat(antibodyRows).ToList();

        string qcText     = string.Empty;
        string createdBy  = string.Empty;
        string dateCreated = string.Empty;

        if (allContentRows.Count > 0)
        {
            var c = allContentRows[0];
            qcText      = Str(c, "QCText");
            createdBy   = Str(c, "Name");
            // Format date as "dd MMMM yyyy" to match legacy "Long Date" format.
            var rawDate = Str(c, "DateCreated");
            dateCreated = FormatLongDate(rawDate);
        }

        // ── 3. Projects lookup (list type 19) ───────────────────────────────
        // GetluProjectsAll returns all rows from luProjects (ID, Description).
        // Match ProjectContractCode against ID to get the full project description.
        var projectRows = (await conn.QueryAsync(
            "GetluProjectsAll",
            commandType: CommandType.StoredProcedure))
            .Cast<IDictionary<string, object>>().ToList();

        var project = projectRows
            .FirstOrDefault(r => Str(r, "ID") == projectCode);
        var projectName = project is not null ? Str(project, "Description") : projectCode;

        // ── 4. Assemble "Header" DataTable ──────────────────────────────────
        var header = new DataTable("Header");
        header.Columns.Add("QCNoteRef");
        header.Columns.Add("SubmissionNumber");
        header.Columns.Add("Project");
        header.Columns.Add("Species");
        header.Columns.Add("StainRef");
        header.Columns.Add("QCText");
        header.Columns.Add("CreatedBy");
        header.Columns.Add("DateCreated");

        var row = header.NewRow();
        row["QCNoteRef"]        = qcNoteRefStr;
        row["SubmissionNumber"] = submissionNo;
        row["Project"]          = projectName;
        row["Species"]          = species;
        row["StainRef"]         = stainRef;
        row["QCText"]           = qcText;
        row["CreatedBy"]        = createdBy;
        row["DateCreated"]      = dateCreated;
        header.Rows.Add(row);

        var ds = new DataSet("QCNote");
        ds.Tables.Add(header);
        return ds;
    }

    private static string Str(IDictionary<string, object> row, string key)
    {
        if (!row.TryGetValue(key, out var val) || val is null || val is DBNull)
            return string.Empty;
        return Convert.ToString(val) ?? string.Empty;
    }

    /// <summary>
    /// Formats a raw date string as "dd MMMM yyyy" (UK long date) to match the
    /// legacy Crystal Reports "Long Date" format used in QCNote.rpt.
    /// Returns <paramref name="raw"/> unchanged when it cannot be parsed as a date.
    /// </summary>
    internal static string FormatLongDate(string raw)
    {
        if (DateTime.TryParse(raw, out var parsedDate))
            return parsedDate.ToString("dd MMMM yyyy");
        return raw;
    }
}
