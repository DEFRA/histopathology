using System.Data;
using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Reporting.Services;
using Histo.Tests.Support;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>End-to-end <c>BuildAsync</c> / <c>HasAnyNotesAsync</c> for the report builders, with the SQL layer faked and lookups mocked.</summary>
public class ReportDataSetBuilderBuildAsyncTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static DataTable Table(params (string Column, Type Type)[] columns)
    {
        var table = new DataTable();
        foreach (var (column, type) in columns) table.Columns.Add(column, type);
        return table;
    }

    private static DataSet Sets(params DataTable[] tables)
    {
        var ds = new DataSet();
        foreach (var t in tables) ds.Tables.Add(t);
        return ds;
    }

    private static DataTable Codes(params string[] codes)
    {
        var t = Table(("BatchID", typeof(int)), ("Code", typeof(string)));
        foreach (var code in codes) t.Rows.Add(99, code);
        return t;
    }

    private static LookupItem Item(int id, string name, string? code = null) => new() { ID = id, Name = name, Code = code };

    private static (Mock<ILookupService> Lookups, Mock<IUserService> Users, Mock<IConfiguration> Config) Dependencies()
    {
        var lookups = new Mock<ILookupService>();
        lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync([Item(3, "Ovine")]);
        lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([Item(1, "H&E", "2")]);
        lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), true, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        lookups.Setup(l => l.GetLookupDataAsync(19, true, It.IsAny<CancellationToken>())).ReturnsAsync([Item(19, "Defra research")]);
        lookups.Setup(l => l.GetLookupDataAsync(18, true, It.IsAny<CancellationToken>())).ReturnsAsync([Item(8, "Dr Jones")]);
        lookups.Setup(l => l.GetLookupDataAsync(10, true, It.IsAny<CancellationToken>())).ReturnsAsync([Item(1, "Other", "1")]);
        lookups.Setup(l => l.GetLookupDataAsync(3, true, It.IsAny<CancellationToken>())).ReturnsAsync([Item(2, "before 11.00", "2")]);
        lookups.Setup(l => l.GetLookupDataAsync(11, true, It.IsAny<CancellationToken>())).ReturnsAsync([Item(2, "Wax Block", "2")]);
        lookups.Setup(l => l.GetLookupDataAsync(6, true, It.IsAny<CancellationToken>())).ReturnsAsync([Item(1, "Stain One", "S1")]);
        lookups.Setup(l => l.GetLookupDataAsync(4, true, It.IsAny<CancellationToken>())).ReturnsAsync([Item(1, "TSE Ab", "A1")]);
        lookups.Setup(l => l.GetLookupDataAsync(5, true, It.IsAny<CancellationToken>())).ReturnsAsync([Item(1, "Non-TSE Ab", "A1")]);

        var users = new Mock<IUserService>();
        users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync([new User { UserID = 12, Name = "A User" }]);

        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Reporting:SubmissionFormVersionTSE"]).Returns("T-1");
        config.Setup(c => c["Reporting:SubmissionFormVersionNonTSE"]).Returns("NT-2");
        return (lookups, users, config);
    }

    private static DataTable BatchRow(int batchType, bool blocked)
    {
        var t = Table(("ID", typeof(int)), ("BatchType", typeof(int)), ("IsBlocked", typeof(bool)), ("Species", typeof(string)),
            ("ProjectContractCode", typeof(string)), ("ContactName", typeof(string)), ("OtherSubmittedBy", typeof(string)),
            ("Fixation", typeof(string)), ("TimeReceived", typeof(string)));
        t.Rows.Add(99, batchType, blocked, "3", "19", "8", "12", "1", "2");
        return t;
    }

    private static DataTable Animals()
    {
        var t = Table(("ID", typeof(int)), ("SenderRef", typeof(string)), ("HistologyRef", typeof(string)));
        t.Rows.Add(5, "SUB1", "26/10001");
        return t;
    }

    private static FakeDb HistologyDb(int batchType, bool blocked, bool withBlocks) => new()
    {
        OnQuery = c => c.Text switch
        {
            "GetCommonBatchTablesByID" => Sets(BatchRow(batchType, blocked), Codes("2", "3", "4"), Codes("A1"), Codes("S1"), Codes("1", "Other"), Codes("2")),
            "GetBatchSubmissionDetailsByBatchID" => Sets(
                Table(("ID", typeof(int))),
                Table(("ID", typeof(int)), ("AnimalID", typeof(int)), ("TissueCode", typeof(string)), ("NoPieces", typeof(int))).Also(t => t.Rows.Add(1, 5, "BRAIN", 1)),
                Animals()),
            "GetBatchBlockDetails" when withBlocks => Sets(
                Table(("ID", typeof(int)), ("AnimalID", typeof(int)), ("BlockRef", typeof(string)), ("RepeatBlock", typeof(bool)), ("CustomerRef", typeof(string)))
                    .Also(t => t.Rows.Add(40, 7, "01", true, "cust-1"))),
            "GetBatchBlockTissues" when withBlocks => Sets(
                Table(("ID", typeof(int)), ("BlockID", typeof(int)), ("TissueCode", typeof(string)), ("NoPieces", typeof(int)))
                    .Also(t => t.Rows.Add(1, 40, "LIVER", 2))),
            "GetBatchBlockAnimal" when withBlocks => Sets(
                Table(("ID", typeof(int)), ("SenderRef", typeof(string)), ("HistologyRef", typeof(string))).Also(t => t.Rows.Add(7, "BLK1", "26/20001"))),
            _ => Sets(Table(("ID", typeof(int)))),
        },
    };

    [Fact]
    public async Task HistologyReport_BlocksAssigned_ResolvesLookupsAndListsBlockTissues()
    {
        var (lookups, users, config) = Dependencies();
        var builder = new HistologyReportDataSetBuilder(HistologyDb(batchType: 1, blocked: true, withBlocks: true), config.Object, lookups.Object, users.Object);

        var ds = await builder.BuildAsync(99, Ct);

        Assert.Equal(["Batch", "BatchPostFixation", "BatchHistology", "BatchSubmission", "Version"], ds.Tables.Cast<DataTable>().Select(t => t.TableName));
        var batch = ds.Tables["Batch"]!.Rows[0];
        Assert.Equal(("Ovine", "Defra research", "Dr Jones", "A User", "Other", "before 11.00", "NON TSE", "Wax Block", "1"),
            (batch["Species"], batch["ProjectContractCode"], batch["ContactName"], batch["OtherSubmittedBy"], batch["Fixation"],
                batch["TimeReceived"], batch["BatchType"], batch["SubmittedAs"], batch["NumberSamples"]));
        var postFix = ds.Tables["BatchPostFixation"]!.Rows[0];
        Assert.Equal(("1", "1"), (postFix["Formic"], postFix["Other"]));
        Assert.Equal(["H&E", "Stain One", "Non-TSE Ab"], ds.Tables["BatchHistology"]!.Rows.Cast<DataRow>().Select(r => r["Code"]));
        var submissionRows = ds.Tables["BatchSubmission"]!.Rows.Cast<DataRow>().ToList();
        Assert.Equal(2, submissionRows.Count);
        Assert.All(submissionRows, r => Assert.Equal(("BLK1", "26/20001", "01", "Yes", "cust-1", "LIVER"),
            (r["SenderRef"], r["HistologyRef"], r["BlockRef"], r["RepeatBlock"], r["CustomerRef"], r["TissueDetails"])));
        Assert.Equal("NT-2", ds.Tables["Version"]!.Rows[0]["Version"]);
    }

    [Fact]
    public async Task HistologyReport_TseBatchNotYetBlocked_ListsSubmissionTissuesAndTseVersion()
    {
        var (lookups, users, config) = Dependencies();
        var builder = new HistologyReportDataSetBuilder(HistologyDb(batchType: 0, blocked: false, withBlocks: false), config.Object, lookups.Object, users.Object);

        var ds = await builder.BuildAsync(99, Ct);

        Assert.Equal("TSE", ds.Tables["Batch"]!.Rows[0]["BatchType"]);
        Assert.Equal("1", ds.Tables["Batch"]!.Rows[0]["NumberSamples"]);
        Assert.Equal(["H&E", "Stain One", "TSE Ab"], ds.Tables["BatchHistology"]!.Rows.Cast<DataRow>().Select(r => r["Code"]));
        var row = Assert.Single(ds.Tables["BatchSubmission"]!.Rows.Cast<DataRow>());
        Assert.Equal(("SUB1", "26/10001", "BRAIN", string.Empty), (row["SenderRef"], row["HistologyRef"], row["TissueDetails"], row["BlockRef"]));
        Assert.Equal("T-1", ds.Tables["Version"]!.Rows[0]["Version"]);
    }

    [Fact]
    public async Task HistologyReport_BatchNotFound_ReturnsEmptyBatchTableAndDefaultTseVersion()
    {
        var (lookups, users, config) = Dependencies();
        var db = new FakeDb { OnQuery = _ => Sets(Enumerable.Range(0, 6).Select(_ => Table(("ID", typeof(int)))).ToArray()) };
        var builder = new HistologyReportDataSetBuilder(db, config.Object, lookups.Object, users.Object);

        var ds = await builder.BuildAsync(99, Ct);

        Assert.Empty(ds.Tables["Batch"]!.Rows);
        Assert.Equal("T-1", ds.Tables["Version"]!.Rows[0]["Version"]);
    }

    [Fact]
    public void BuildPostFixationTable_OtherCode_SetsOtherColumn()
    {
        var table = HistologyReportDataSetBuilder.BuildPostFixationTable(
            [new Dictionary<string, object> { ["Code"] = "Other" }], 1);

        Assert.Equal("1", table.Rows[0]["Other"]);
    }

    // ---------------- SubmissionNotesDataSetBuilder ----------------

    private static DataTable NoteRows(params (string Comment, string Archive)[] rows)
    {
        var t = Table(("SenderRef", typeof(string)), ("TissueCode", typeof(string)), ("BlockRef", typeof(string)), ("Description", typeof(string)),
            ("Comment", typeof(string)), ("ArchiveComment", typeof(string)));
        foreach (var (comment, archive) in rows) t.Rows.Add("S1", "BRAIN", "01", "H&E", comment, archive);
        return t;
    }

    private static DataTable NotesHeader(string comments, string status)
    {
        var t = Table(("ID", typeof(int)), ("Comments", typeof(string)), ("StatusComments", typeof(string)));
        t.Rows.Add(99, comments, status);
        return t;
    }

    private static FakeDb NotesDb(DataTable header, DataTable? tissues = null, DataTable? blocks = null, DataTable? antibodies = null, DataTable? histology = null, DataTable? stains = null) => new()
    {
        OnQuery = _ => Sets(header, tissues ?? NoteRows(), blocks ?? NoteRows(), antibodies ?? NoteRows(), histology ?? NoteRows(), stains ?? NoteRows()),
    };

    [Fact]
    public async Task SubmissionNotes_BuildAsync_MapsHeaderAndKeepsOnlyRowsWithComments()
    {
        var db = NotesDb(NotesHeader("batch note", "status note"),
            tissues: NoteRows((" tissue note ", ""), ("", ""), ("  ", " ")),
            blocks: NoteRows(("", "block archive")),
            antibodies: NoteRows(("ab note", "")),
            histology: NoteRows(("histology note", "histology archive")),
            stains: NoteRows(("", "")));

        var ds = await new SubmissionNotesDataSetBuilder(db).BuildAsync(99, Ct);

        var header = ds.Tables["Submission"]!.Rows[0];
        Assert.Equal(("99", "batch note", "status note"), (header["SubmissionNumber"], header["SubmissionComments"], header["SubmissionStatusComment"]));
        var tissue = Assert.Single(ds.Tables["SubmissionTissues"]!.Rows.Cast<DataRow>());
        Assert.Equal(("S1", "BRAIN", "tissue note", string.Empty), (tissue["SenderRef"], tissue["TissueCode"], tissue["TissueComment"], tissue["TissueArchiveComment"]));
        var block = Assert.Single(ds.Tables["SubmissionBlocks"]!.Rows.Cast<DataRow>());
        Assert.Equal(("01", "block archive"), (block["BlockRef"], block["BlockArchiveComment"]));
        var antibody = Assert.Single(ds.Tables["BlockAntibodies"]!.Rows.Cast<DataRow>());
        Assert.Equal(("H&E", "ab note"), (antibody["Test"], antibody["TestComment"]));
        Assert.Single(ds.Tables["BlockHistology"]!.Rows);
        Assert.Empty(ds.Tables["BlockSpecialStain"]!.Rows);
    }

    [Fact]
    public async Task SubmissionNotes_BuildAsync_NoBatch_ReturnsEmptyHeaderTable()
    {
        var db = NotesDb(Table(("ID", typeof(int))));

        var ds = await new SubmissionNotesDataSetBuilder(db).BuildAsync(99, Ct);

        Assert.Empty(ds.Tables["Submission"]!.Rows);
    }

    [Theory]
    [InlineData("batch note", "", false, true)]
    [InlineData("  ", "status note", false, true)]
    [InlineData("", "", true, true)]
    [InlineData("", "", false, false)]
    public async Task SubmissionNotes_HasAnyNotes_TrueWhenHeaderOrAnyRowCarriesAComment(string comments, string status, bool tissueNote, bool expected)
    {
        var db = NotesDb(NotesHeader(comments, status), tissues: tissueNote ? NoteRows(("note", "")) : null);

        Assert.Equal(expected, await new SubmissionNotesDataSetBuilder(db).HasAnyNotesAsync(99, Ct));
    }
}
