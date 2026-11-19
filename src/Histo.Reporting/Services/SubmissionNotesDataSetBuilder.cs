using System.Data;
using Dapper;
using Histo.Infrastructure;

namespace Histo.Reporting.Services;

/// <summary>
/// Assembles the six-table DataSet consumed by <see cref="Reports.SubmissionNotesRenderer"/>.
///
/// Legacy equivalent: <c>SubmissionNotes.aspx.vb — CreateReport(iBatchID)</c> which called
/// <c>clsBatch.GetBatchComments</c> (SP: <c>GetAllBatchComments</c>) and then built each
/// table by filtering rows with non-blank Comment or ArchiveComment fields.
///
/// This service replicates the same DataSet schema (<c>SubmissionNotesDataset.xsd</c>)
/// via Dapper + stored procedures, without any Crystal Reports dependency.
///
/// <c>GetAllBatchComments @ID</c> executes 6 child SPs in sequence, returning 6 result sets:
/// <list type="number">
///   <item>[0] <c>GetBatchComments</c>          → Submission header row</item>
///   <item>[1] <c>GetBatchTissuesComments</c>    → SubmissionTissues rows</item>
///   <item>[2] <c>GetBatchBlockComments</c>      → SubmissionBlocks rows</item>
///   <item>[3] <c>GetBatchBlockAntibodiesNotes</c> → BlockAntibodies rows</item>
///   <item>[4] <c>GetBatchBlockHistologyNotes</c>  → BlockHistology rows</item>
///   <item>[5] <c>GetBatchBlockStainNotes</c>    → BlockSpecialStain rows</item>
/// </list>
///
/// Filter rule (legacy equivalent in <c>SubmissionNotes.aspx.vb</c>):
/// Result sets 1-5 include only rows where <c>Comment.Trim() != ""</c> OR
/// <c>ArchiveComment.Trim() != ""</c>.  This filtering is applied here so the
/// renderer receives only meaningful rows.
/// </summary>
public sealed class SubmissionNotesDataSetBuilder
{
    private const string SenderRefColumn = "SenderRef";
    private const string CommentColumn = "Comment";
    private const string ArchiveCommentColumn = "ArchiveComment";
    private const string BlockRefColumn = "BlockRef";
    private const string TestCommentColumn = "TestComment";
    private const string TestArchiveCommentColumn = "TestArchiveComment";
    
    private static readonly string[] NoteTableNames = ["SubmissionTissues", "SubmissionBlocks", "BlockAntibodies", "BlockHistology", "BlockSpecialStain"];

    private readonly IDbConnectionFactory _db;

    public SubmissionNotesDataSetBuilder(IDbConnectionFactory db) => _db = db;

    /// <summary>
    /// Builds the report DataSet for the given batch.
    /// </summary>
    /// <param name="batchId">The batch ID sourced from session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Populated DataSet containing the 6 named tables.</returns>
    public async Task<DataSet> BuildAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        // GetAllBatchComments returns 6 result sets in the order defined by the SP body:
        //   EXEC GetBatchComments @ID                  → [0]
        //   EXEC GetBatchTissuesComments @ID           → [1]
        //   EXEC GetBatchBlockComments @ID             → [2]
        //   EXEC GetBatchBlockAntibodiesNotes @ID      → [3]
        //   EXEC GetBatchBlockHistologyNotes @ID       → [4]
        //   EXEC GetBatchBlockStainNotes @ID           → [5]
        var grid = await conn.QueryMultipleAsync(
            "GetAllBatchComments",
            new { ID = batchId },
            commandType: CommandType.StoredProcedure);

        var rawSubmission = await ReadResultSetAsync(grid);
        var rawTissues    = await ReadResultSetAsync(grid);
        var rawBlocks     = await ReadResultSetAsync(grid);
        var rawAntibodies = await ReadResultSetAsync(grid);
        var rawHistology  = await ReadResultSetAsync(grid);
        var rawStains     = await ReadResultSetAsync(grid);

        var ds = new DataSet("SubmissionNotes");
        ds.Tables.Add(BuildSubmissionTable(rawSubmission));
        ds.Tables.Add(BuildNoteTable("SubmissionTissues", rawTissues,
            [SenderRefColumn, "TissueCode", "TissueComment", "TissueArchiveComment"],
            SenderRefColumn, "TissueCode"));
        ds.Tables.Add(BuildNoteTable("SubmissionBlocks", rawBlocks,
            [SenderRefColumn, BlockRefColumn, "BlockComment", "BlockArchiveComment"],
            SenderRefColumn, BlockRefColumn));
        ds.Tables.Add(BuildTestNoteTable("BlockAntibodies", rawAntibodies));
        ds.Tables.Add(BuildTestNoteTable("BlockHistology", rawHistology));
        ds.Tables.Add(BuildTestNoteTable("BlockSpecialStain", rawStains));

        return ds;
    }

    private static async Task<List<IDictionary<string, object>>> ReadResultSetAsync(SqlMapper.GridReader grid)
        => (await grid.ReadAsync()).Cast<IDictionary<string, object>>().ToList();

    // Result set 0 — single header row, no filter.
    private static DataTable BuildSubmissionTable(List<IDictionary<string, object>> rawSubmission)
    {
        var submission = new DataTable("Submission");
        submission.Columns.Add("SubmissionNumber");
        submission.Columns.Add("SubmissionComments");
        submission.Columns.Add("SubmissionStatusComment");

        if (rawSubmission.Count > 0)
        {
            var h = rawSubmission[0];
            var r = submission.NewRow();
            r["SubmissionNumber"]        = Str(h, "ID");
            r["SubmissionComments"]      = Str(h, "Comments");
            r["SubmissionStatusComment"] = Str(h, "StatusComments");
            submission.Rows.Add(r);
        }
        return submission;
    }

    private static DataTable BuildTestNoteTable(string tableName, List<IDictionary<string, object>> rows)
        => BuildNoteTable(tableName, rows,
            [BlockRefColumn, "Test", TestCommentColumn, TestArchiveCommentColumn],
            BlockRefColumn, "Description");

    /// <summary>
    /// Builds a table of four columns: two copied from <paramref name="firstKey"/>/<paramref name="secondKey"/>
    /// and the trimmed Comment/ArchiveComment; rows with both comments blank are skipped.
    /// </summary>
    private static DataTable BuildNoteTable(
        string tableName,
        List<IDictionary<string, object>> rows,
        string[] columns,
        string firstKey,
        string secondKey)
    {
        var table = new DataTable(tableName);
        foreach (var column in columns)
            table.Columns.Add(column);

        foreach (var src in rows)
        {
            var comment = Str(src, CommentColumn).Trim();
            var archive = Str(src, ArchiveCommentColumn).Trim();
            if (comment == string.Empty && archive == string.Empty) continue;

            var r = table.NewRow();
            r[columns[0]] = Str(src, firstKey);
            r[columns[1]] = Str(src, secondKey);
            r[columns[2]] = comment;
            r[columns[3]] = archive;
            table.Rows.Add(r);
        }
        return table;
    }

    /// <summary>
    /// Lightweight existence check equivalent to legacy <c>EnableSubmissionNotes</c> — true when
    /// the submission has any recorded comment, whether at header (Batch.Comments/StatusComments)
    /// level or on any individual tissue/block/antibody/histology/stain row. Callers that only
    /// checked Batch.Comments/StatusComments missed notes recorded solely at tissue/block/test
    /// level, leaving the "Print submission notes" button disabled even though the report has
    /// content.
    /// </summary>
    public async Task<bool> HasAnyNotesAsync(int batchId, CancellationToken ct = default)
    {
        var ds = await BuildAsync(batchId, ct);

        var header = ds.Tables["Submission"];
        if (header is { Rows.Count: > 0 })
        {
            var row = header.Rows[0];
            if (!string.IsNullOrWhiteSpace(row["SubmissionComments"] as string) ||
                !string.IsNullOrWhiteSpace(row["SubmissionStatusComment"] as string))
                return true;
        }

        return NoteTableNames
            .Any(name => ds.Tables[name] is { Rows.Count: > 0 });
    }

    private static string Str(IDictionary<string, object> row, string key)
    {
        if (!row.TryGetValue(key, out var val) || val is null || val is DBNull)
            return string.Empty;
        return Convert.ToString(val) ?? string.Empty;
    }

    /// <summary>
    /// Returns <see langword="true"/> when at least one of the two comment fields
    /// in <paramref name="src"/> contains a non-whitespace value.
    /// Used to filter result-set rows before adding them to report tables,
    /// matching the legacy <c>SubmissionNotes.aspx.vb</c> filter rule.
    /// </summary>
    internal static bool HasContent(
        IDictionary<string, object> src,
        string commentKey,
        string archiveKey)
    {
        var comment = Str(src, commentKey).Trim();
        var archive = Str(src, archiveKey).Trim();
        return comment != string.Empty || archive != string.Empty;
    }
}
