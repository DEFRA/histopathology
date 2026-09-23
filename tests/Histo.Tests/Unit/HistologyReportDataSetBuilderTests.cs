using System.Data;
using Histo.Reporting.Services;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for the internal static table-builder methods of
/// <see cref="HistologyReportDataSetBuilder"/>.
///
/// These methods are called by <c>BuildAsync</c> after the Dapper calls have
/// returned raw dictionaries.  Testing them in isolation lets us verify the
/// business logic (CommentLengthOK threshold, BatchType label mapping,
/// SubmittedAs concatenation, post-fixation code routing) without a database.
/// </summary>
public class HistologyReportDataSetBuilderTests
{
    // ── Factory helpers ──────────────────────────────────────────────────────

    private static Dictionary<string, object> Row(params (string key, object value)[] fields)
    {
        var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in fields)
            d[key] = value;
        return d;
    }

    private static Dictionary<string, object> DefaultBatchRow(
        string comments     = "short",
        int    batchType    = 0,
        bool   safeToHandle = false)
    {
        return Row(
            ("ID",                 "99"),
            ("ProjectContractCode", "TBA"),
            ("ContactName",        "Dr Smith"),
            ("BatchDate",          "2024-01-15"),
            ("Species",            "Bovine"),
            ("DateReceived",       "2024-01-16"),
            ("TimeReceived",       "before 11.00"),
            ("SafeToHandle",       safeToHandle),
            ("Comments",           comments),
            ("Fixation",           "Formalin"),
            ("PostFixationOther",  ""),
            ("OtherSubmittedBy",   "VLA Lasswade"),
            ("NumberSamples",      "3"),
            ("BatchType",          batchType));
    }

    private static Dictionary<string, object> SubmittedAsRow(int batchId, string code)
        => Row(("BatchID", batchId), ("Code", code));

    // ── BuildBatchTable ──────────────────────────────────────────────────────

    [Fact]
    public void BuildBatchTable_EmptyRawBatch_ReturnsTableWithZeroRows()
    {
        var table = HistologyReportDataSetBuilder.BuildBatchTable(
            rawBatch: [],
            rawSubmittedAs: [],
            batchId: 1);

        Assert.Equal(0, table.Rows.Count);
        Assert.Equal("Batch", table.TableName);
    }

    [Fact]
    public void BuildBatchTable_ShortComments_CommentLengthOkIsEmpty()
    {
        // Comments ≤ 150 chars → no overflow flag
        var comments = new string('x', 150);
        var table = HistologyReportDataSetBuilder.BuildBatchTable(
            rawBatch: [DefaultBatchRow(comments: comments)],
            rawSubmittedAs: [],
            batchId: 99);

        Assert.Equal(string.Empty, table.Rows[0]["CommentLengthOK"]);
    }

    [Fact]
    public void BuildBatchTable_LongComments_CommentLengthOkIsCheckmark()
    {
        // Comments > 150 chars → set overflow flag to Unicode checkmark
        var comments = new string('x', 151);
        var table = HistologyReportDataSetBuilder.BuildBatchTable(
            rawBatch: [DefaultBatchRow(comments: comments)],
            rawSubmittedAs: [],
            batchId: 99);

        Assert.Equal("\u2713", table.Rows[0]["CommentLengthOK"]);
    }

    [Fact]
    public void BuildBatchTable_BatchTypeZero_LabelIsTSE()
    {
        var table = HistologyReportDataSetBuilder.BuildBatchTable(
            rawBatch: [DefaultBatchRow(batchType: 0)],
            rawSubmittedAs: [],
            batchId: 99);

        Assert.Equal("TSE", table.Rows[0]["BatchType"]);
    }

    [Fact]
    public void BuildBatchTable_BatchTypeOne_LabelIsNonTSE()
    {
        var table = HistologyReportDataSetBuilder.BuildBatchTable(
            rawBatch: [DefaultBatchRow(batchType: 1)],
            rawSubmittedAs: [],
            batchId: 99);

        Assert.Equal("NON TSE", table.Rows[0]["BatchType"]);
    }

    [Fact]
    public void BuildBatchTable_ProjectCodeNotInLookup_FallsBackToRawCode()
    {
        var projectsById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["19"] = "Defra research" };
        var table = HistologyReportDataSetBuilder.BuildBatchTable(
            rawBatch: [DefaultBatchRow()],
            rawSubmittedAs: [],
            batchId: 99,
            projectsById: projectsById);

        // DefaultBatchRow's ProjectContractCode is "TBA", which isn't in the lookup — should fall back to the raw code.
        Assert.Equal("TBA", table.Rows[0]["ProjectContractCode"]);
    }

    [Fact]
    public void BuildBatchTable_ProjectCodeWithPadding_StillResolvesToName()
    {
        // tblBatch.ProjectContractCode can carry fixed-width padding from the source column —
        // the raw code must be trimmed before the dictionary lookup, or it silently falls back
        // to displaying the padded raw ID instead of the resolved project name.
        var row = DefaultBatchRow();
        row["ProjectContractCode"] = " 19 ";
        var projectsById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["19"] = "Defra research" };

        var table = HistologyReportDataSetBuilder.BuildBatchTable(
            rawBatch: [row],
            rawSubmittedAs: [],
            batchId: 99,
            projectsById: projectsById);

        Assert.Equal("Defra research", table.Rows[0]["ProjectContractCode"]);
    }

    [Fact]
    public void BuildBatchTable_MultipleSubmittedAsRows_ConcatenatesResolvedNames()
    {
        const int batchId = 99;
        var submittedAsByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["2"] = "Wax Block",
            ["6"] = "Fresh Tissue",
        };
        var table = HistologyReportDataSetBuilder.BuildBatchTable(
            rawBatch: [DefaultBatchRow()],
            rawSubmittedAs:
            [
                SubmittedAsRow(batchId, "2"),
                SubmittedAsRow(batchId, "6")
            ],
            batchId: batchId,
            submittedAsByCode: submittedAsByCode);

        Assert.Equal("Wax Block, Fresh Tissue", table.Rows[0]["SubmittedAs"]);
    }

    [Fact]
    public void BuildBatchTable_SubmittedAsRowsForOtherBatch_NotIncluded()
    {
        // Rows belonging to a different batch should be excluded from concatenation
        const int batchId = 99;
        var submittedAsByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["2"] = "Wax Block",
        };
        var table = HistologyReportDataSetBuilder.BuildBatchTable(
            rawBatch: [DefaultBatchRow()],
            rawSubmittedAs:
            [
                SubmittedAsRow(batchId, "2"),
                SubmittedAsRow(999,     "9")
            ],
            batchId: batchId,
            submittedAsByCode: submittedAsByCode);

        Assert.Equal("Wax Block", table.Rows[0]["SubmittedAs"]);
    }

    [Fact]
    public void BuildBatchTable_FixationCode_ResolvesToDisplayName()
    {
        var fixationByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["1"] = "Other" };
        var row = DefaultBatchRow();
        row["Fixation"] = "1";

        var table = HistologyReportDataSetBuilder.BuildBatchTable(
            rawBatch: [row], rawSubmittedAs: [], batchId: 99, fixationByCode: fixationByCode);

        Assert.Equal("Other", table.Rows[0]["Fixation"]);
    }

    [Fact]
    public void BuildBatchTable_TimeReceivedCode_ResolvesToDisplayName()
    {
        var timeReceivedByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["2"] = "before 11.00" };
        var row = DefaultBatchRow();
        row["TimeReceived"] = "2";

        var table = HistologyReportDataSetBuilder.BuildBatchTable(
            rawBatch: [row], rawSubmittedAs: [], batchId: 99, timeReceivedByCode: timeReceivedByCode);

        Assert.Equal("before 11.00", table.Rows[0]["TimeReceived"]);
    }

    [Fact]
    public void BuildBatchTable_UnresolvedFixationCode_FallsBackToRawCode()
    {
        var row = DefaultBatchRow();
        row["Fixation"] = "7";

        var table = HistologyReportDataSetBuilder.BuildBatchTable(
            rawBatch: [row], rawSubmittedAs: [], batchId: 99,
            fixationByCode: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["1"] = "Other" });

        Assert.Equal("7", table.Rows[0]["Fixation"]);
    }

    [Fact]
    public void BuildBatchTable_SampleCount_PopulatesNumberSamples()
    {
        var table = HistologyReportDataSetBuilder.BuildBatchTable(
            rawBatch: [DefaultBatchRow()], rawSubmittedAs: [], batchId: 99, sampleCount: 4);

        Assert.Equal("4", table.Rows[0]["NumberSamples"]);
    }

    [Fact]
    public void BuildBatchTable_SafeToHandleTrue_MapsToBoolTrue()
    {
        var table = HistologyReportDataSetBuilder.BuildBatchTable(
            rawBatch: [DefaultBatchRow(safeToHandle: true)],
            rawSubmittedAs: [],
            batchId: 99);

        Assert.Equal(true, table.Rows[0]["SafeToHandle"]);
    }

    [Fact]
    public void BuildBatchTable_SafeToHandleFalse_MapsToBoolFalse()
    {
        var table = HistologyReportDataSetBuilder.BuildBatchTable(
            rawBatch: [DefaultBatchRow(safeToHandle: false)],
            rawSubmittedAs: [],
            batchId: 99);

        Assert.Equal(false, table.Rows[0]["SafeToHandle"]);
    }

    // ── BuildPostFixationTable ────────────────────────────────────────────────

    [Fact]
    public void BuildPostFixationTable_Code1_SetsFormic()
    {
        var table = HistologyReportDataSetBuilder.BuildPostFixationTable(
            rawPostFix: [Row(("Code", "1"))],
            batchId: 1);

        Assert.Equal("1", table.Rows[0]["Formic"]);
    }

    [Fact]
    public void BuildPostFixationTable_Code2_SetsDecal()
    {
        var table = HistologyReportDataSetBuilder.BuildPostFixationTable(
            rawPostFix: [Row(("Code", "2"))],
            batchId: 1);

        Assert.Equal("1", table.Rows[0]["Decal"]);
    }

    [Fact]
    public void BuildPostFixationTable_Code3_SetsPhenol()
    {
        var table = HistologyReportDataSetBuilder.BuildPostFixationTable(
            rawPostFix: [Row(("Code", "3"))],
            batchId: 1);

        Assert.Equal("1", table.Rows[0]["Phenol"]);
    }

    [Fact]
    public void BuildPostFixationTable_Code1And2_SetsBothFormicAndDecal()
    {
        var table = HistologyReportDataSetBuilder.BuildPostFixationTable(
            rawPostFix: [Row(("Code", "1")), Row(("Code", "2"))],
            batchId: 1);

        var row = table.Rows[0];
        Assert.Equal("1", row["Formic"]);
        Assert.Equal("1", row["Decal"]);
        Assert.True(row["Phenol"] is null or DBNull or "");
    }

    [Fact]
    public void BuildPostFixationTable_EmptyInput_ReturnsOneRowWithAllColumnsEmpty()
    {
        var table = HistologyReportDataSetBuilder.BuildPostFixationTable(
            rawPostFix: [],
            batchId: 5);

        // Always returns exactly one row even with no post-fix codes
        Assert.Equal(1, table.Rows.Count);
        var row = table.Rows[0];
        Assert.True(row["Decal"]  is null or DBNull or "");
        Assert.True(row["Phenol"] is null or DBNull or "");
        Assert.True(row["Formic"] is null or DBNull or "");
        Assert.True(row["Other"]  is null or DBNull or "");
    }

    // ── BuildHistologyTable ───────────────────────────────────────────────────

    [Fact]
    public void BuildHistologyTable_EmptyInput_ReturnsZeroRows()
    {
        var table = HistologyReportDataSetBuilder.BuildHistologyTable(
            rawHistology: [],
            batchId: 1);

        Assert.Equal(0, table.Rows.Count);
        Assert.Equal("BatchHistology", table.TableName);
    }

    [Fact]
    public void BuildHistologyTable_DirectCodes_ResolvedToDescriptions()
    {
        var histologyByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["2"] = "H&E",
            ["7"] = "Archive",
        };
        var table = HistologyReportDataSetBuilder.BuildHistologyTable(
            rawHistology: [Row(("Code", "2")), Row(("Code", "7"))],
            batchId: 1,
            histologyByCode: histologyByCode);

        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("H&E", table.Rows[0]["Code"]);
        Assert.Equal("Archive", table.Rows[1]["Code"]);
    }

    [Fact]
    public void BuildHistologyTable_Code7_ResolvesToArchive()
    {
        var table = HistologyReportDataSetBuilder.BuildHistologyTable(
            rawHistology: [Row(("Code", "7"))],
            batchId: 42,
            histologyByCode: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["7"] = "Archive" });

        Assert.Equal("Archive", table.Rows[0]["Code"]);
        Assert.Equal(42, table.Rows[0]["BatchID"]);
    }

    [Fact]
    public void BuildHistologyTable_SpecialStainFlag_ExpandsStainRows()
    {
        // Histology code 3 (Special Stain) expands into the batch's actual stain rows.
        var table = HistologyReportDataSetBuilder.BuildHistologyTable(
            rawHistology: [Row(("Code", "3"))],
            batchId: 1,
            rawStains: [Row(("Code", "14")), Row(("Code", "Other"))],
            specialStainByCode: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["14"] = "PAS" });

        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("PAS", table.Rows[0]["Code"]);
        Assert.Equal("Special Other", table.Rows[1]["Code"]); // "Other" special-cased by legacy
    }

    [Fact]
    public void BuildHistologyTable_IhcFlagTse_ExpandsTseAntibodyRows()
    {
        // Histology code 4/6 (IHC) expands into the batch's antibody rows; TSE uses table 4.
        var table = HistologyReportDataSetBuilder.BuildHistologyTable(
            rawHistology: [Row(("Code", "4"))],
            batchId: 1,
            rawAntibodies: [Row(("Code", "1")), Row(("Code", "Other"))],
            batchType: 0, // TSE
            tseAntibodiesByCode: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["1"] = "PrP (SAF84)" });

        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("PrP (SAF84)", table.Rows[0]["Code"]);
        Assert.Equal("IHC-PrP Other", table.Rows[1]["Code"]);
    }

    [Fact]
    public void BuildHistologyTable_CappedAtEightRows()
    {
        var codes = Enumerable.Range(0, 12).Select(_ => Row(("Code", "2"))).ToArray();
        var table = HistologyReportDataSetBuilder.BuildHistologyTable(
            rawHistology: codes,
            batchId: 1,
            histologyByCode: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["2"] = "H&E" });

        Assert.Equal(8, table.Rows.Count);
    }

    // ── BuildSubmissionTable ──────────────────────────────────────────────────

    [Fact]
    public void BuildSubmissionTable_EmptyInput_ReturnsZeroRows()
    {
        var table = HistologyReportDataSetBuilder.BuildSubmissionTable(
            rawBatchSubmissions: [],
            rawTissues: [],
            rawAnimals: [],
            batchId: 1);

        Assert.Equal(0, table.Rows.Count);
        Assert.Equal("BatchSubmission", table.TableName);
    }

    [Fact]
    public void BuildSubmissionTable_BlockRef_SequentialPerAnimal()
    {
        var tissue1 = Row(("AnimalID", (object)5), ("ID", (object)10), ("BatchSubmissionID", (object)1), ("TissueCode", "Lung"),  ("Comment", ""));
        var tissue2 = Row(("AnimalID", (object)5), ("ID", (object)11), ("BatchSubmissionID", (object)1), ("TissueCode", "Liver"), ("Comment", ""));
        var animal  = Row(("ID", (object)5), ("SenderRef", "REF-1"), ("HistologyRef", "25/001"));

        var table = HistologyReportDataSetBuilder.BuildSubmissionTable(
            rawBatchSubmissions: [],
            rawTissues: [tissue1, tissue2],
            rawAnimals: [animal],
            batchId: 1);

        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("01", table.Rows[0]["BlockRef"]);
        Assert.Equal("02", table.Rows[1]["BlockRef"]);
    }

    [Fact]
    public void BuildSubmissionTable_AllColumnsPopulated()
    {
        var tissue = Row(("AnimalID", (object)42), ("ID", (object)1), ("BatchSubmissionID", (object)1), ("TissueCode", "Kidney"), ("Comment", "c-ref"));
        var animal = Row(("ID", (object)42), ("SenderRef", "S-001"), ("HistologyRef", "11/999"));

        var table = HistologyReportDataSetBuilder.BuildSubmissionTable(
            rawBatchSubmissions: [],
            rawTissues: [tissue],
            rawAnimals: [animal],
            batchId: 7);

        var row = table.Rows[0];
        Assert.Equal("7",      row["BatchID"]);
        Assert.Equal("S-001",  row["SenderRef"]);
        Assert.Equal("11/999", row["HistologyRef"]);
        Assert.Equal("01",     row["BlockRef"]);
        Assert.Equal("Kidney", row["TissueDetails"]);
        Assert.Equal("",       row["RepeatBlock"]);
        Assert.Equal("c-ref",  row["CustomerRef"]);
    }
}
