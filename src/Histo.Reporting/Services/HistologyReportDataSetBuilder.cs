using System.Data;
using Dapper;
using Histo.Administration.Interfaces;
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
    // Legacy source: HistopathologySystem/Common.vb — LOOKUP_CONTACTS / LOOKUP_PROJECTS
    private const int LookupContacts = 18;
    private const int LookupProjects = 19;
    private const int LookupFixation = 10;     // LOOKUP_FIXATION — keyed on Code (ddlFixation.DataValueField = "Code")
    private const int LookupTimeReceived = 3;  // LOOKUP_TIME_RECEIVED — keyed on Code
    private const int LookupSubmittedAs = 11;  // LOOKUP_SUBMITTEDAS — keyed on Code (GetBatchSubmittedAs returns Code only, no Description)
    private const int LookupSpecialStain = 6;      // LOOKUP_SPECIAL_STAIN
    private const int LookupTseAntibodies = 4;     // LOOKUP_TSE_ANTIBODIES
    private const int LookupNonTseAntibodies = 5;  // LOOKUP_NONTSE_ANTIBODIES

    private readonly IDbConnectionFactory _db;
    private readonly IConfiguration _config;
    private readonly ILookupService _lookups;
    private readonly IUserService _users;

    public HistologyReportDataSetBuilder(
        IDbConnectionFactory db,
        IConfiguration config,
        ILookupService lookups,
        IUserService users)
    {
        _db = db;
        _config = config;
        _lookups = lookups;
        _users = users;
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
        var rawAntibodies = (await commonGrid.ReadAsync()).Cast<IDictionary<string, object>>().ToList(); // [2] — expanded into the Histology Required section for IHC codes
        var rawStains     = (await commonGrid.ReadAsync()).Cast<IDictionary<string, object>>().ToList(); // [3] — expanded into the Histology Required section for Special Stain code
        var rawPostFix    = (await commonGrid.ReadAsync()).Cast<IDictionary<string, object>>().ToList();
        var rawSubmittedAs = (await commonGrid.ReadAsync()).Cast<IDictionary<string, object>>().ToList();

        // ── 2. Submission rows ───────────────────────────────────────────────
        // GetBatchSubmissionDetailsByBatchID returns 3 result sets:
        //   [0] BatchSubmission join rows (ID, BatchID, AnimalID, Order)
        //   [1] BatchTissues rows         (AnimalID, ID, BatchSubmissionID, TissueCode, Comment, ...)
        //   [2] Animal rows               (ID, SenderRef, HistologyRef, NextBlockRef, ...)
        var submissionGrid = await conn.QueryMultipleAsync(
            "GetBatchSubmissionDetailsByBatchID",
            new { ID = batchId },
            commandType: CommandType.StoredProcedure);

        var rawBatchSubmissions = (await submissionGrid.ReadAsync()).Cast<IDictionary<string, object>>().ToList();
        var rawTissues          = (await submissionGrid.ReadAsync()).Cast<IDictionary<string, object>>().ToList();
        var rawAnimals          = (await submissionGrid.ReadAsync()).Cast<IDictionary<string, object>>().ToList();

        // ── 2a. Block rows — a batch is either Wet Tissue (submission-owned BatchTissues, above)
        // or Cassetted/Wax Block etc. (block-owned BatchBlock/BlockTissues, never both). Without
        // this, every Block-type submission's detail grid (Sender Ref/Histology Ref/Block Ref/
        // Tissue Code) rendered with zero rows, since GetBatchTissues only ever returns
        // submission-owned rows.
        var rawBlocks = (await conn.QueryAsync<dynamic>(
            "GetBatchBlockDetails", new { ID = batchId }, commandType: CommandType.StoredProcedure))
            .Cast<IDictionary<string, object>>().ToList();
        var rawBlockTissues = (await conn.QueryAsync<dynamic>(
            "GetBatchBlockTissues", new { ID = batchId }, commandType: CommandType.StoredProcedure))
            .Cast<IDictionary<string, object>>().ToList();

        // ── 2a. ID→name lookups — tblBatch stores raw codes (Species ID, Project code,
        // Pathologist/Contact ID, Submitted-By user ID), not display names; GetCommonBatchTablesByID
        // does not join them, so resolve here to match legacy's dropdown-bound display.
        var speciesTask   = _lookups.GetSpeciesLookupAsync(ct);
        var projectsTask  = _lookups.GetLookupDataAsync(LookupProjects, includeInactive: true, ct: ct);
        var contactsTask  = _lookups.GetLookupDataAsync(LookupContacts, includeInactive: true, ct: ct);
        var fixationTask  = _lookups.GetLookupDataAsync(LookupFixation, includeInactive: true, ct: ct);
        var timeReceivedTask = _lookups.GetLookupDataAsync(LookupTimeReceived, includeInactive: true, ct: ct);
        var submittedAsTask = _lookups.GetLookupDataAsync(LookupSubmittedAs, includeInactive: true, ct: ct);
        // Histology Required section lookups (legacy GetHistologyListType / GetListType).
        var histologyTask = _lookups.GetHistologyTypesAsync(ct);
        var specialStainTask = _lookups.GetLookupDataAsync(LookupSpecialStain, includeInactive: true, ct: ct);
        var tseAntibodiesTask = _lookups.GetLookupDataAsync(LookupTseAntibodies, includeInactive: true, ct: ct);
        var nonTseAntibodiesTask = _lookups.GetLookupDataAsync(LookupNonTseAntibodies, includeInactive: true, ct: ct);
        var usersTask     = _users.GetAllUsersAsync(ct);
        await Task.WhenAll(speciesTask, projectsTask, contactsTask, fixationTask, timeReceivedTask, submittedAsTask,
            histologyTask, specialStainTask, tseAntibodiesTask, nonTseAntibodiesTask, usersTask);

        var speciesById  = speciesTask.Result.ToDictionary(s => s.ID.ToString(), s => s.Name, StringComparer.OrdinalIgnoreCase);
        var projectsById = projectsTask.Result.ToDictionary(p => p.ID.ToString(), p => p.Name, StringComparer.OrdinalIgnoreCase);
        var contactsById = contactsTask.Result.ToDictionary(c => c.ID.ToString(), c => c.Name, StringComparer.OrdinalIgnoreCase);
        // Fixation/TimeReceived are Code-keyed (not ID) — tblBatch stores the Code, matching
        // how BatchDetails/ReceiveBatch resolve them. GroupBy guards against duplicate/blank codes.
        var fixationByCode = fixationTask.Result
            .Where(f => !string.IsNullOrEmpty(f.Code))
            .GroupBy(f => f.Code!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);
        var timeReceivedByCode = timeReceivedTask.Result
            .Where(t => !string.IsNullOrEmpty(t.Code))
            .GroupBy(t => t.Code!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);
        var submittedAsByCode = submittedAsTask.Result
            .Where(s => !string.IsNullOrEmpty(s.Code))
            .GroupBy(s => s.Code!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);
        var userById      = usersTask.Result.ToDictionary(u => u.UserID, u => u.Name);

        // Histology Required lookups — all keyed on Code (matches legacy DataView.Find on "Code").
        static Dictionary<string, string> ByCode(IReadOnlyList<Administration.Models.LookupItem> items) => items
            .Where(i => !string.IsNullOrEmpty(i.Code))
            .GroupBy(i => i.Code!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);
        var histologyByCode         = ByCode(histologyTask.Result);
        var specialStainByCode      = ByCode(specialStainTask.Result);
        var tseAntibodiesByCode     = ByCode(tseAntibodiesTask.Result);
        var nonTseAntibodiesByCode  = ByCode(nonTseAntibodiesTask.Result);

        int batchType = rawBatch.Count > 0 && rawBatch[0].TryGetValue("BatchType", out var btVal)
            ? Convert.ToInt32(btVal ?? 0) : 0;

        // ── 3. Assemble the report DataSet ───────────────────────────────────
        var ds = new DataSet("HistologyReport");

        // Total Samples = the batch's animal (sample) count — the header SP returns no such
        // column, matching BatchDetails.SampleCount = GetAnimalsByBatchAsync().Count.
        ds.Tables.Add(BuildBatchTable(rawBatch, rawSubmittedAs, batchId, speciesById, projectsById, contactsById, userById, fixationByCode, timeReceivedByCode, submittedAsByCode, rawAnimals.Count));
        ds.Tables.Add(BuildPostFixationTable(rawPostFix, batchId));
        ds.Tables.Add(BuildHistologyTable(rawHistology, batchId, rawAntibodies, rawStains, batchType,
            histologyByCode, specialStainByCode, tseAntibodiesByCode, nonTseAntibodiesByCode));
        // Legacy prints the block listing only once blocks are assigned (IsBlocked) or the batch is
        // Cassetted; otherwise the submission's own tissues.
        var blocksAssigned = rawBatch.Count > 0 && (Bool(rawBatch[0], "IsBlocked") || Bool(rawBatch[0], "Cassetted"));

        ds.Tables.Add(BuildSubmissionTable(rawBatchSubmissions, rawTissues, rawAnimals, rawBlocks, rawBlockTissues, batchId, blocksAssigned));
        ds.Tables.Add(BuildVersionTable(rawBatch));

        return ds;
    }

    // ── Table builders ───────────────────────────────────────────────────────

    internal static DataTable BuildBatchTable(
        IList<IDictionary<string, object>> rawBatch,
        IList<IDictionary<string, object>> rawSubmittedAs,
        int batchId,
        IReadOnlyDictionary<string, string>? speciesById = null,
        IReadOnlyDictionary<string, string>? projectsById = null,
        IReadOnlyDictionary<string, string>? contactsById = null,
        IReadOnlyDictionary<int, string>? userById = null,
        IReadOnlyDictionary<string, string>? fixationByCode = null,
        IReadOnlyDictionary<string, string>? timeReceivedByCode = null,
        IReadOnlyDictionary<string, string>? submittedAsByCode = null,
        int sampleCount = 0)
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

        // Fields mapped directly from stored proc result. tblBatch stores raw codes for
        // ProjectContractCode (project ID), ContactName (pathologist/contact ID), Species
        // (species ID), and OtherSubmittedBy (submitter user ID) — resolve to display names
        // via the supplied lookup dictionaries, matching legacy's dropdown-bound display.
        // Falls back to the raw code when it cannot be resolved or no lookup was supplied.
        dr["ID"]               = Str(src, "ID",               batchId.ToString());
        // Trimmed before lookup — tblBatch.ProjectContractCode can carry fixed-width padding from
        // the source column, which otherwise fails the exact-string dictionary match and silently
        // falls back to displaying the raw (padded) ID instead of the resolved project name.
        var rawProjectCode    = Str(src, "ProjectContractCode").Trim();
        dr["ProjectContractCode"] = projectsById is not null && projectsById.TryGetValue(rawProjectCode, out var pn)
            ? pn : rawProjectCode;
        var rawContactCode    = Str(src, "ContactName");
        dr["ContactName"]      = contactsById is not null && contactsById.TryGetValue(rawContactCode, out var cn)
            ? cn : rawContactCode;
        dr["BatchDate"]        = Str(src, "BatchDate");
        var rawSpecies        = Str(src, "Species");
        dr["Species"]          = speciesById is not null && speciesById.TryGetValue(rawSpecies, out var sn)
            ? sn : rawSpecies;
        dr["DateReceived"]     = Str(src, "DateReceived");
        // TimeReceived is a Code (foreign key to LOOKUP_TIME_RECEIVED = 3) — resolve to its
        // display text (e.g. "before 11.00"); falls back to the raw code when unresolved.
        var rawTimeReceived   = Str(src, "TimeReceived");
        dr["TimeReceived"]     = timeReceivedByCode is not null && timeReceivedByCode.TryGetValue(rawTimeReceived, out var trn)
            ? trn : rawTimeReceived;
        dr["SafeToHandle"]     = Bool(src, "SafeToHandle");
        dr["Comments"]         = Str(src, "Comments");
        // Fixation is a Code (foreign key to LOOKUP_FIXATION = 10, keyed on Code not ID) —
        // resolve to its display text (e.g. "Other"); falls back to the raw code when unresolved.
        var rawFixation       = Str(src, "Fixation");
        dr["Fixation"]         = fixationByCode is not null && fixationByCode.TryGetValue(rawFixation, out var fxn)
            ? fxn : rawFixation;
        dr["PostFixationOther"] = Str(src, "PostFixationOther");
        var rawSubmittedBy     = Str(src, "OtherSubmittedBy");
        dr["OtherSubmittedBy"] = userById is not null
            && int.TryParse(rawSubmittedBy, out var submittedByUserId)
            && userById.TryGetValue(submittedByUserId, out var sbn)
                ? sbn : rawSubmittedBy;
        dr["NumberSamples"]    = sampleCount.ToString();
        dr["MoreHistology"]    = string.Empty; // set below if any histology overflows

        // BatchType label — matches legacy "TSE" / "NON TSE" display
        int batchType = src.TryGetValue("BatchType", out var btVal)
            ? Convert.ToInt32(btVal ?? 0) : 0;
        dr["BatchType"] = batchType == 0 ? "TSE" : "NON TSE";

        // SubmittedAs — GetBatchSubmittedAs returns Code only (no Description); resolve each
        // Code to its display name via LOOKUP_SUBMITTEDAS (table 11), then concatenate.
        var submittedAsNames = rawSubmittedAs
            .Where(r => r.TryGetValue("BatchID", out var bid) && Convert.ToInt32(bid) == batchId)
            .Select(r =>
            {
                var code = Str(r, "Code");
                return submittedAsByCode is not null && submittedAsByCode.TryGetValue(code, out var n) ? n : code;
            })
            .Where(s => !string.IsNullOrWhiteSpace(s));
        dr["SubmittedAs"] = string.Join(", ", submittedAsNames);

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

    /// <summary>
    /// Builds the "Histology Required" sub-report table, replicating legacy
    /// <c>SubmissionForm.aspx.vb::CreateBatchTestTable</c>: each histology <c>Code</c> is
    /// resolved to its display name, and the Special Stain (code 3) and IHC (codes 4/6) flags
    /// are expanded into their actual test rows from the Antibodies/Stains result sets.
    /// The resolved display text is stored in the <c>Code</c> column (as legacy does).
    /// Capped at 8 rows (legacy limit).
    /// </summary>
    /// <param name="batchType">0 = TSE (uses table 4 antibodies), else Non-TSE (table 5).</param>
    internal static DataTable BuildHistologyTable(
        IList<IDictionary<string, object>> rawHistology,
        int batchId,
        IList<IDictionary<string, object>>? rawAntibodies = null,
        IList<IDictionary<string, object>>? rawStains = null,
        int batchType = 0,
        IReadOnlyDictionary<string, string>? histologyByCode = null,
        IReadOnlyDictionary<string, string>? specialStainByCode = null,
        IReadOnlyDictionary<string, string>? tseAntibodiesByCode = null,
        IReadOnlyDictionary<string, string>? nonTseAntibodiesByCode = null)
    {
        const int MaxRows = 8; // legacy caps the Histology Required list at 8 entries
        var dt = new DataTable("BatchHistology");
        dt.Columns.Add("BatchID", typeof(int));
        dt.Columns.Add("Code"); // holds the resolved DESCRIPTION (matches legacy sub-report shape)

        rawAntibodies ??= [];
        rawStains ??= [];

        string ResolveHistology(string code) =>
            histologyByCode is not null && histologyByCode.TryGetValue(code, out var d) ? d : code;
        string ResolveStain(string code) =>
            code == "Other" ? "Special Other"
            : specialStainByCode is not null && specialStainByCode.TryGetValue(code, out var d) ? d : code;
        string ResolveAntibody(string code)
        {
            if (code == "Other") return "IHC-PrP Other";
            var lookup = batchType == 0 ? tseAntibodiesByCode : nonTseAntibodiesByCode;
            return lookup is not null && lookup.TryGetValue(code, out var d) ? d : code;
        }

        void Add(string description)
        {
            var dr = dt.NewRow();
            dr["BatchID"] = batchId;
            dr["Code"]    = description;
            dt.Rows.Add(dr);
        }

        foreach (var hrow in rawHistology)
        {
            if (dt.Rows.Count >= MaxRows) break;
            var code = Str(hrow, "Code");
            switch (code)
            {
                // Direct histology tests (EO/H&E/H&E-BSE/Archive) — resolve to description.
                case "1" or "2" or "5" or "7":
                    Add(ResolveHistology(code));
                    break;

                // Special Stain flag — expand into the batch's actual special-stain rows.
                case "3":
                    foreach (var srow in rawStains)
                    {
                        if (dt.Rows.Count >= MaxRows) break;
                        Add(ResolveStain(Str(srow, "Code")));
                    }
                    break;

                // IHC-PrP / IHC-Other flags — expand into the batch's actual antibody rows.
                case "4" or "6":
                    foreach (var arow in rawAntibodies)
                    {
                        if (dt.Rows.Count >= MaxRows) break;
                        Add(ResolveAntibody(Str(arow, "Code")));
                    }
                    break;
            }
        }

        return dt;
    }

    private const string ColSenderRef = "SenderRef";
    private const string ColHistologyRef = "HistologyRef";
    private const string ColBlockRef = "BlockRef";
    private const string ColRepeatBlock = "RepeatBlock";
    private const string ColCustomerRef = "CustomerRef";
    private const string ColAnimalId = "AnimalID";
    private const string ColBlockId = "BlockID";

    internal static DataTable BuildSubmissionTable(
        IList<IDictionary<string, object>> rawBatchSubmissions,
        IList<IDictionary<string, object>> rawTissues,
        IList<IDictionary<string, object>> rawAnimals,
        IList<IDictionary<string, object>> rawBlocks,
        IList<IDictionary<string, object>> rawBlockTissues,
        int batchId,
        bool blocksAssigned = true)
    {
        var dt = new DataTable("BatchSubmission");
        dt.Columns.Add("BatchID");
        dt.Columns.Add(ColSenderRef);
        dt.Columns.Add(ColHistologyRef);
        dt.Columns.Add(ColBlockRef);
        dt.Columns.Add("TissueDetails");
        dt.Columns.Add(ColRepeatBlock);
        dt.Columns.Add(ColCustomerRef);

        // Build animal lookup: AnimalID → animal row. GetBatchAnimal (the SP behind this result
        // set) joins through BatchSubmission with no DISTINCT, so an animal referenced by more
        // than one BatchSubmission in this batch (e.g. a Pre-Cassetted submission reusing an
        // existing animal's ID) comes back more than once — this is purely a lookup consulted
        // per-tissue-row below (never iterated itself), so keeping the first occurrence per ID
        // is safe and doesn't drop or merge any visible output rows.
        var animalById = rawAnimals
            .Where(a => a.ContainsKey("ID"))
            .GroupBy(a => Convert.ToInt32(a["ID"]))
            .ToDictionary(g => g.Key, g => g.First());

        // Legacy SubmissionForm.aspx.vb picks ONE source: block-owned tissues once blocks are
        // assigned (CreateBatchBlocksDataTable), else submission tissues. Emitting both listed every
        // tissue twice. Falls back to block tissues when there are no submission tissues, so the
        // table is never empty.
        var useBlocks = rawBlockTissues.Count > 0 && (blocksAssigned || rawTissues.Count == 0);

        if (useBlocks)
            AddBlockTissueRows(dt, rawBlocks, rawBlockTissues, animalById, batchId);
        else if (rawTissues.Count > 0)
            AddSubmissionTissueRows(dt, rawTissues, animalById, batchId);

        return dt;
    }

    private static void AddSubmissionTissueRows(
        DataTable dt,
        IList<IDictionary<string, object>> rawTissues,
        Dictionary<int, IDictionary<string, object>> animalById,
        int batchId)
    {
        // Sort tissues by AnimalID then tissue ID for stable ordering
        var sortedTissues = rawTissues
            .OrderBy(t => IntOrZero(t, ColAnimalId))
            .ThenBy(t => IntOrZero(t, "ID"))
            .ToList();

        foreach (var tissue in sortedTissues)
        {
            animalById.TryGetValue(IntOrZero(tissue, ColAnimalId), out var animal);

            // Legacy never sets BlockRef here and lists one row per piece (NoPieces).
            // Legacy source: SubmissionForm.aspx.vb::CreateBatchSubmissionDataTable — the
            // CustomerRef column is declared but never assigned for Wet Tissue rows; it's a
            // Block-only field, so it's always blank here.
            for (var p = 0; p < PieceCount(tissue); p++)
                dt.Rows.Add(NewSubmissionRow(dt, batchId, animal, Str(tissue, "TissueCode"),
                    blockRef: string.Empty, repeatBlock: string.Empty, customerRef: string.Empty));
        }
    }

    private static void AddBlockTissueRows(
        DataTable dt,
        IList<IDictionary<string, object>> rawBlocks,
        IList<IDictionary<string, object>> rawBlockTissues,
        Dictionary<int, IDictionary<string, object>> animalById,
        int batchId)
    {
        // Block-type submission (Cassetted/Wax Block/Stained/Unstained Section) — BlockRef is
        // a real, already-allocated value on the block itself (not a running counter), and the
        // owning animal is resolved via the block's own AnimalID rather than the tissue row.
        var blockById = rawBlocks
            .Where(b => b.ContainsKey("ID"))
            .ToDictionary(b => Convert.ToInt32(b["ID"]), b => b);

        // Group by sample, then block, so a block's tissues are contiguous (legacy sorted animals
        // by Histology Ref and blocks by Block Ref); block IDs alone interleave samples.
        var sortedBlockTissues = rawBlockTissues
            .OrderBy(t => SampleHistologyRef(t, blockById, animalById), StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => BlockAnimalId(FindBlock(t, blockById)))
            .ThenBy(t => FindBlock(t, blockById) is { } blk ? Str(blk, ColBlockRef) : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => IntOrZero(t, ColBlockId))
            .ThenBy(t => IntOrZero(t, "ID"))
            .ToList();

        foreach (var tissue in sortedBlockTissues)
        {
            var block = FindBlock(tissue, blockById);
            animalById.TryGetValue(BlockAnimalId(block), out var animal);

            var blockRef = block is not null ? Str(block, ColBlockRef) : string.Empty;
            var repeatBlock = block is not null && Bool(block, ColRepeatBlock) ? "Yes" : string.Empty;
            // Legacy source: SubmissionForm.aspx.vb::CreateBatchBlocksDataTable —
            // drBatchBlocks("CustomerRef") = drBlock("CustomerRef"); the block's own Customer
            // Reference field, never the tissue's Comment.
            var customerRef = block is not null ? Str(block, ColCustomerRef) : string.Empty;

            // Legacy lists one row per physical piece (NoPieces) under the same block.
            for (var p = 0; p < PieceCount(tissue); p++)
                dt.Rows.Add(NewSubmissionRow(dt, batchId, animal, Str(tissue, "TissueCode"), blockRef, repeatBlock, customerRef));
        }
    }

    private static DataRow NewSubmissionRow(
        DataTable dt, int batchId, IDictionary<string, object>? animal, string tissueDetails,
        string blockRef, string repeatBlock, string customerRef)
    {
        var dr = dt.NewRow();
        dr["BatchID"]       = batchId.ToString();
        dr[ColSenderRef]    = animal is not null ? Str(animal, ColSenderRef) : string.Empty;
        dr[ColHistologyRef] = animal is not null ? Str(animal, ColHistologyRef) : string.Empty;
        dr[ColBlockRef]     = blockRef;
        dr["TissueDetails"] = tissueDetails;
        dr[ColRepeatBlock]  = repeatBlock;
        dr[ColCustomerRef]  = customerRef;
        return dr;
    }

    private static int IntOrZero(IDictionary<string, object> row, string key) =>
        row.TryGetValue(key, out var value) ? Convert.ToInt32(value) : 0;

    private static int PieceCount(IDictionary<string, object> tissue) =>
        tissue.TryGetValue("NoPieces", out var pieces) && pieces is not null and not DBNull
            ? Math.Max(1, Convert.ToInt32(pieces)) : 1;

    private static IDictionary<string, object>? FindBlock(
        IDictionary<string, object> tissue, Dictionary<int, IDictionary<string, object>> blockById) =>
        blockById.GetValueOrDefault(IntOrZero(tissue, ColBlockId));

    private static int BlockAnimalId(IDictionary<string, object>? block) =>
        block is not null && block.TryGetValue(ColAnimalId, out var animalId) ? Convert.ToInt32(animalId) : 0;

    private static string SampleHistologyRef(
        IDictionary<string, object> tissue,
        Dictionary<int, IDictionary<string, object>> blockById,
        Dictionary<int, IDictionary<string, object>> animalById)
    {
        var block = FindBlock(tissue, blockById);
        if (block is null || !block.TryGetValue(ColAnimalId, out var animalId)) return string.Empty;

        return animalById.TryGetValue(Convert.ToInt32(animalId), out var animal) ? Str(animal, ColHistologyRef) : string.Empty;
    }

    private DataTable BuildVersionTable(List<IDictionary<string, object>> rawBatch)
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
