using System.Data;
using Dapper;
using Histo.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Histo.Reporting.Services;

/// <summary>
/// Assembles the multi-table DataSet consumed by <see cref="Reports.HistologyReportRenderer"/>.
///
/// Legacy equivalent: <c>SubmissionForm.aspx.vb — Page_Load</c>, which populated a
/// DataSet from <c>GetCommonBatchTablesByID</c> and
/// <c>GetBatchSubmissionDetailsByBatchID</c> stored procedures, then performed
/// field-mapping and lookup translation before passing to Crystal Reports.
///
/// This service replicates the same DataSet schema (<c>HistologyReportDataset.xsd</c>)
/// via Dapper + stored procedures, without any Crystal Reports dependency.
///
/// Expected output tables (names match those consumed by the renderer):
/// <list type="bullet">
///   <item><b>Batch</b> — single header row (17 columns)</item>
///   <item><b>BatchPostFixation</b> — single row; Decal/Phenol/Formic/Other columns</item>
///   <item><b>BatchHistology</b> — 0-N rows; BatchID + Code columns</item>
///   <item><b>BatchSubmission</b> — 0-N rows; SenderRef/HistologyRef/BlockRef/TissueDetails/CustomerRef/RepeatBlock</item>
///   <item><b>Version</b> — single row; Version column (sourced from configuration)</item>
/// </list>
/// </summary>
public sealed class HistologyReportDataSetBuilder
{
    private readonly IDbConnectionFactory _db;
    private readonly IConfiguration _config;

    public HistologyReportDataSetBuilder(IDbConnectionFactory db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Builds the report DataSet for the given batch.
    /// </summary>
    /// <param name="batchId">The batch ID sourced from session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Populated DataSet matching <c>HistologyReportDataset.xsd</c>.</returns>
    public async Task<DataSet> BuildAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        // ── 1. Batch header + post-fixation + histology tests ────────────────
        // GetCommonBatchTablesByID returns 6 result sets (clsBatch table-index constants):
        //   [0] BATCH_TABLE          — batch header
        //   [1] BATCH_HISTOLOGY_TABLE — histology test codes
        //   [2] BATCH_ANTIBODIES_TABLE
        //   [3] BATCH_STAIN_TABLE
        //   [4] BATCH_POSTFIXATION_TABLE
        //   [5] BATCH_SUBMITTEDAS_TABLE
        var commonGrid = await conn.QueryMultipleAsync(
            "GetCommonBatchTablesByID",
            new { ID = batchId },
            commandType: CommandType.StoredProcedure);

        var rawBatch      = (await commonGrid.ReadAsync()).Cast<IDictionary<string, object>>().ToList();
        var rawHistology  = (await commonGrid.ReadAsync()).Cast<IDictionary<string, object>>().ToList();
        _                 = await commonGrid.ReadAsync(); // antibodies — not needed for this report
        _                 = await commonGrid.ReadAsync(); // stains     — not needed for this report
        var rawPostFix    = (await commonGrid.ReadAsync()).Cast<IDictionary<string, object>>().ToList();
        var rawSubmittedAs = (await commonGrid.ReadAsync()).Cast<IDictionary<string, object>>().ToList();

        // ── 2. Submission rows ───────────────────────────────────────────────
        // GetBatchSubmissionDetailsByBatchID returns the per-submission detail rows
        // used to build the BatchSubmission table in the legacy SubmissionForm report.
        var rawSubmissions = (await conn.QueryAsync(
            "GetBatchSubmissionDetailsByBatchID",
            new { ID = batchId },
            commandType: CommandType.StoredProcedure))
            .Cast<IDictionary<string, object>>().ToList();

        // ── 3. Assemble the report DataSet ───────────────────────────────────
        var ds = new DataSet("HistologyReport");

        ds.Tables.Add(BuildBatchTable(rawBatch, rawSubmittedAs, batchId));
        ds.Tables.Add(BuildPostFixationTable(rawPostFix, batchId));
        ds.Tables.Add(BuildHistologyTable(rawHistology, batchId));
        ds.Tables.Add(BuildSubmissionTable(rawSubmissions, batchId));
        ds.Tables.Add(BuildVersionTable(rawBatch));

        return ds;
    }

    // ── Table builders ───────────────────────────────────────────────────────

    internal static DataTable BuildBatchTable(
        IList<IDictionary<string, object>> rawBatch,
        IList<IDictionary<string, object>> rawSubmittedAs,
        int batchId)
    {
        var dt = new DataTable("Batch");
        dt.Columns.Add("ProjectContractCode");
        dt.Columns.Add("ContactName");
        dt.Columns.Add("BatchDate");
        dt.Columns.Add("Species");
        dt.Columns.Add("DateReceived");
        dt.Columns.Add("TimeReceived");
        dt.Columns.Add("SafeToHandle", typeof(bool));
        dt.Columns.Add("Comments");
        dt.Columns.Add("Fixation");
        dt.Columns.Add("ID");
        dt.Columns.Add("PostFixationOther");
        dt.Columns.Add("CommentLengthOK");
        dt.Columns.Add("OtherSubmittedBy");
        dt.Columns.Add("NumberSamples");
        dt.Columns.Add("BatchType");
        dt.Columns.Add("SubmittedAs");
        dt.Columns.Add("MoreHistology");

        if (rawBatch.Count == 0)
            return dt;

        var src = rawBatch[0];
        var dr  = dt.NewRow();

        // Fields mapped directly from stored proc result
        dr["ID"]               = Str(src, "ID",               batchId.ToString());
        dr["ProjectContractCode"] = Str(src, "ProjectContractCode");
        dr["ContactName"]      = Str(src, "ContactName");
        dr["BatchDate"]        = Str(src, "BatchDate");
        dr["Species"]          = Str(src, "Species");
        dr["DateReceived"]     = Str(src, "DateReceived");
        dr["TimeReceived"]     = Str(src, "TimeReceived");
        dr["SafeToHandle"]     = Bool(src, "SafeToHandle");
        dr["Comments"]         = Str(src, "Comments");
        dr["Fixation"]         = Str(src, "Fixation");
        dr["PostFixationOther"] = Str(src, "PostFixationOther");
        dr["OtherSubmittedBy"] = Str(src, "OtherSubmittedBy");
        dr["NumberSamples"]    = Str(src, "NumberSamples");
        dr["MoreHistology"]    = string.Empty; // set below if any histology overflows

        // BatchType label — matches legacy "TSE" / "NON TSE" display
        int batchType = src.TryGetValue("BatchType", out var btVal)
            ? Convert.ToInt32(btVal ?? 0) : 0;
        dr["BatchType"] = batchType == 0 ? "TSE" : "NON TSE";

        // SubmittedAs — concatenate all SubmittedAs descriptions for this batch
        var submittedAsCodes = rawSubmittedAs
            .Where(r => r.TryGetValue("BatchID", out var bid) && Convert.ToInt32(bid) == batchId)
            .Select(r => Str(r, "Description"))
            .Where(s => !string.IsNullOrWhiteSpace(s));
        dr["SubmittedAs"] = string.Join(", ", submittedAsCodes);

        // CommentLengthOK — flag if comments exceed the visible limit (150 chars, matches legacy)
        var comments = dr["Comments"]?.ToString() ?? string.Empty;
        dr["CommentLengthOK"] = comments.Length > 150 ? "\u2713" : string.Empty;

        dt.Rows.Add(dr);
        return dt;
    }

    internal static DataTable BuildPostFixationTable(
        IList<IDictionary<string, object>> rawPostFix,
        int batchId)
    {
        var dt = new DataTable("BatchPostFixation");
        dt.Columns.Add("BatchID");
        dt.Columns.Add("Decal");
        dt.Columns.Add("Phenol");
        dt.Columns.Add("Formic");
        dt.Columns.Add("Other");

        var dr = dt.NewRow();
        dr["BatchID"] = batchId.ToString();

        // Legacy: Chr(252) Wingdings tick per post-fixation code row.
        // Modern: renderer substitutes Chr(252) → U+2713; we pass "1" (truthy) here.
        foreach (var row in rawPostFix)
        {
            var code = Str(row, "Code");
            switch (code)
            {
                case "1": dr["Formic"] = "1"; break;
                case "2": dr["Decal"]  = "1"; break;
                case "3": dr["Phenol"] = "1"; break;
                case "Other": dr["Other"] = "1"; break;
            }
        }

        dt.Rows.Add(dr);
        return dt;
    }

    internal static DataTable BuildHistologyTable(
        IList<IDictionary<string, object>> rawHistology,
        int batchId)
    {
        var dt = new DataTable("BatchHistology");
        dt.Columns.Add("BatchID", typeof(int));
        dt.Columns.Add("Code");

        foreach (var row in rawHistology)
        {
            var dr = dt.NewRow();
            dr["BatchID"] = batchId;
            dr["Code"]    = Str(row, "Code");
            dt.Rows.Add(dr);
        }

        return dt;
    }

    internal static DataTable BuildSubmissionTable(
        IList<IDictionary<string, object>> rawSubmissions,
        int batchId)
    {
        var dt = new DataTable("BatchSubmission");
        dt.Columns.Add("BatchID");
        dt.Columns.Add("SenderRef");
        dt.Columns.Add("HistologyRef");
        dt.Columns.Add("BlockRef");
        dt.Columns.Add("TissueDetails");
        dt.Columns.Add("RepeatBlock");
        dt.Columns.Add("CustomerRef");

        foreach (var row in rawSubmissions)
        {
            var dr = dt.NewRow();
            dr["BatchID"]      = batchId.ToString();
            dr["SenderRef"]    = Str(row, "SenderRef");
            dr["HistologyRef"] = Str(row, "HistologyRef");
            dr["BlockRef"]     = Str(row, "BlockRef");
            dr["TissueDetails"] = Str(row, "TissueDetails");
            dr["RepeatBlock"]  = Str(row, "RepeatBlock");
            dr["CustomerRef"]  = Str(row, "CustomerRef");
            dt.Rows.Add(dr);
        }

        return dt;
    }

    private DataTable BuildVersionTable(IList<IDictionary<string, object>> rawBatch)
    {
        var dt = new DataTable("Version");
        dt.Columns.Add("Version");

        // Version sourced from configuration (matches legacy web.config AppSettings).
        // TSE batches use SubmissionFormVersionTSE; Non-TSE use SubmissionFormVersionNonTSE.
        int batchType = rawBatch.Count > 0 && rawBatch[0].TryGetValue("BatchType", out var btv)
            ? Convert.ToInt32(btv ?? 0) : 0;

        var versionKey = batchType == 0
            ? "Reporting:SubmissionFormVersionTSE"
            : "Reporting:SubmissionFormVersionNonTSE";

        var dr = dt.NewRow();
        dr["Version"] = _config[versionKey] ?? string.Empty;
        dt.Rows.Add(dr);

        return dt;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string Str(IDictionary<string, object> row, string key, string fallback = "")
        => row.TryGetValue(key, out var v) && v is not null && v is not DBNull
            ? Convert.ToString(v) ?? fallback
            : fallback;

    private static bool Bool(IDictionary<string, object> row, string key)
        => row.TryGetValue(key, out var v) && v is not null && v is not DBNull && Convert.ToBoolean(v);
}
