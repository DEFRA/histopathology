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

        var rawSubmission  = (await grid.ReadAsync()).Cast<IDictionary<string, object>>().ToList();
        var rawTissues     = (await grid.ReadAsync()).Cast<IDictionary<string, object>>().ToList();
        var rawBlocks      = (await grid.ReadAsync()).Cast<IDictionary<string, object>>().ToList();
        var rawAntibodies  = (await grid.ReadAsync()).Cast<IDictionary<string, object>>().ToList();
        var rawHistology   = (await grid.ReadAsync()).Cast<IDictionary<string, object>>().ToList();
        var rawStains      = (await grid.ReadAsync()).Cast<IDictionary<string, object>>().ToList();

        var ds = new DataSet("SubmissionNotes");

        // ── Submission (result set 0) — single header row, no filter ─────────
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
        ds.Tables.Add(submission);

        // ── SubmissionTissues (result set 1) — filter non-blank Comment/ArchiveComment ──
        var tissues = new DataTable("SubmissionTissues");
        tissues.Columns.Add("SenderRef");
        tissues.Columns.Add("TissueCode");
        tissues.Columns.Add("TissueComment");
        tissues.Columns.Add("TissueArchiveComment");

        foreach (var src in rawTissues)
        {
            var comment = Str(src, "Comment").Trim();
            var archive = Str(src, "ArchiveComment").Trim();
            if (comment == string.Empty && archive == string.Empty) continue;

            var r = tissues.NewRow();
            r["SenderRef"]           = Str(src, "SenderRef");
            r["TissueCode"]          = Str(src, "TissueCode");
            r["TissueComment"]       = comment;
            r["TissueArchiveComment"] = archive;
            tissues.Rows.Add(r);
        }
        ds.Tables.Add(tissues);

        // ── SubmissionBlocks (result set 2) ──────────────────────────────────
        var blocks = new DataTable("SubmissionBlocks");
        blocks.Columns.Add("SenderRef");
        blocks.Columns.Add("BlockRef");
        blocks.Columns.Add("BlockComment");
        blocks.Columns.Add("BlockArchiveComment");

        foreach (var src in rawBlocks)
        {
            var comment = Str(src, "Comment").Trim();
            var archive = Str(src, "ArchiveComment").Trim();
            if (comment == string.Empty && archive == string.Empty) continue;

            var r = blocks.NewRow();
            r["SenderRef"]          = Str(src, "SenderRef");
            r["BlockRef"]           = Str(src, "BlockRef");
            r["BlockComment"]       = comment;
            r["BlockArchiveComment"] = archive;
            blocks.Rows.Add(r);
        }
        ds.Tables.Add(blocks);

        // ── BlockAntibodies (result set 3) ───────────────────────────────────
        var antibodies = new DataTable("BlockAntibodies");
        antibodies.Columns.Add("BlockRef");
        antibodies.Columns.Add("Test");
        antibodies.Columns.Add("TestComment");
        antibodies.Columns.Add("TestArchiveComment");

        foreach (var src in rawAntibodies)
        {
            var comment = Str(src, "Comment").Trim();
            var archive = Str(src, "ArchiveComment").Trim();
            if (comment == string.Empty && archive == string.Empty) continue;

            var r = antibodies.NewRow();
            r["BlockRef"]           = Str(src, "BlockRef");
            r["Test"]               = Str(src, "Description");
            r["TestComment"]        = comment;
            r["TestArchiveComment"] = archive;
            antibodies.Rows.Add(r);
        }
        ds.Tables.Add(antibodies);

        // ── BlockHistology (result set 4) ─────────────────────────────────────
        var histology = new DataTable("BlockHistology");
        histology.Columns.Add("BlockRef");
        histology.Columns.Add("Test");
        histology.Columns.Add("TestComment");
        histology.Columns.Add("TestArchiveComment");

        foreach (var src in rawHistology)
        {
            var comment = Str(src, "Comment").Trim();
            var archive = Str(src, "ArchiveComment").Trim();
            if (comment == string.Empty && archive == string.Empty) continue;

            var r = histology.NewRow();
            r["BlockRef"]           = Str(src, "BlockRef");
            r["Test"]               = Str(src, "Description");
            r["TestComment"]        = comment;
            r["TestArchiveComment"] = archive;
            histology.Rows.Add(r);
        }
        ds.Tables.Add(histology);

        // ── BlockSpecialStain (result set 5) ──────────────────────────────────
        var stains = new DataTable("BlockSpecialStain");
        stains.Columns.Add("BlockRef");
        stains.Columns.Add("Test");
        stains.Columns.Add("TestComment");
        stains.Columns.Add("TestArchiveComment");

        foreach (var src in rawStains)
        {
            var comment = Str(src, "Comment").Trim();
            var archive = Str(src, "ArchiveComment").Trim();
            if (comment == string.Empty && archive == string.Empty) continue;

            var r = stains.NewRow();
            r["BlockRef"]           = Str(src, "BlockRef");
            r["Test"]               = Str(src, "Description");
            r["TestComment"]        = comment;
            r["TestArchiveComment"] = archive;
            stains.Rows.Add(r);
        }
        ds.Tables.Add(stains);

        return ds;
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
