using System.Data;
using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Reporting.Services;
using Histo.Tests.Support;
using Moq;

namespace Histo.Tests.Unit;

/// <summary><see cref="QCNoteDataSetBuilder.BuildAsync"/> against <see cref="FakeDb"/> with a mocked species lookup.</summary>
public class QCNoteDataSetBuilderBuildAsyncTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly string[] BlankColumns =
        ["QCNoteRef", "SubmissionNumber", "Project", "Species", "StainRef", "QCText", "CreatedBy", "DateCreated"];

    private static DataTable Table(params (string Column, Type Type)[] columns)
    {
        var table = new DataTable();
        foreach (var (column, type) in columns) table.Columns.Add(column, type);
        return table;
    }

    private static DataSet One(DataTable t) => new DataSet().Also(ds => ds.Tables.Add(t));

    private static DataTable Header(string project = "P1", string species = "3")
    {
        var t = Table(("QCNoteRef", typeof(int)), ("ID", typeof(int)), ("Species", typeof(string)), ("StainRef", typeof(string)),
            ("ProjectContractCode", typeof(string)), ("BatchType", typeof(int)));
        t.Rows.Add(7, 1234, species, "ST1", project, 2);
        return t;
    }

    private static DataTable Content(string text, string name, string date, params (string Sender, string Histo, string Block, string Desc)[] rows)
    {
        var t = Table(("QCText", typeof(string)), ("Name", typeof(string)), ("DateCreated", typeof(string)),
            ("SenderRef", typeof(string)), ("HistologyRef", typeof(string)), ("BlockRef", typeof(string)), ("Description", typeof(string)));
        if (rows.Length == 0) t.Rows.Add(text, name, date, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);
        foreach (var (s, h, b, d) in rows) t.Rows.Add(text, name, date, s, h, b, d);
        return t;
    }

    private static DataTable Projects(params (string Id, string Description)[] rows)
    {
        var t = Table(("ID", typeof(string)), ("Description", typeof(string)));
        foreach (var (id, d) in rows) t.Rows.Add(id, d);
        return t;
    }

    private static QCNoteDataSetBuilder Builder(FakeDb db, params LookupItem[] species)
    {
        var lookups = new Mock<ILookupService>();
        lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync(species);
        return new QCNoteDataSetBuilder(db, lookups.Object);
    }

    private static DataRow Row(DataSet ds) => Assert.Single(Assert.Single(ds.Tables.Cast<DataTable>()).Rows.Cast<DataRow>());

    [Fact]
    public async Task BuildAsync_AssemblesHeaderFromAllProcsAndLookups()
    {
        var db = new FakeDb
        {
            OnQuery = c => c.Text switch
            {
                "GetBatchQCNotes" => One(Header()),
                "GetQCNoteHistStainTestInformation" => One(Content("Note text", "Alice", "2026-03-05", ("S1", "26/10001", "01", "H&E"))),
                "GetQCNoteAntibodiesInformation" => One(Content("ignored", "Bob", "x", ("S1", "26/10001", "02", "IHC"))),
                "GetluProjectsAll" => One(Projects(("P1", "Defra research"), ("P2", "Other"))),
                _ => null,
            },
        };

        var ds = await Builder(db, new LookupItem { ID = 3, Name = "Ovine" }).BuildAsync(7, Ct);

        Assert.Equal("QCNote", ds.DataSetName);
        var row = Row(ds);
        Assert.Equal("Header", row.Table.TableName);
        Assert.Equal(("7", "1234", "Defra research", "Ovine", "ST1"),
            ((string)row["QCNoteRef"], (string)row["SubmissionNumber"], (string)row["Project"], (string)row["Species"], (string)row["StainRef"]));
        Assert.Equal(("Note text", "Alice", "05 March 2026"), ((string)row["QCText"], (string)row["CreatedBy"], (string)row["DateCreated"]));

        var summary = (string)row["TestSummary"];
        Assert.StartsWith("Sender Ref", summary);
        Assert.Contains("H&E", summary);
        Assert.Contains("IHC", summary);
        Assert.Equal(3, summary.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length);

        var antibodies = Assert.Single(db.Commands, c => c.Text == "GetQCNoteAntibodiesInformation");
        Assert.Equal((7, 2), ((int)antibodies.Parameters["QCNoteRef"]!, (int)antibodies.Parameters["SubmissionType"]!));
    }

    [Fact]
    public async Task BuildAsync_UnknownProjectAndSpecies_FallBackToRawCodes()
    {
        var db = new FakeDb
        {
            OnQuery = c => c.Text switch
            {
                "GetBatchQCNotes" => One(Header(project: "ZZ", species: "9")),
                "GetluProjectsAll" => One(Projects(("P1", "Defra research"))),
                _ => null,
            },
        };

        var row = Row(await Builder(db, new LookupItem { ID = 3, Name = "Ovine" }).BuildAsync(7, Ct));

        Assert.Equal("ZZ", row["Project"]);
        Assert.Equal("9", row["Species"]);
    }

    [Fact]
    public async Task BuildAsync_NoRowsFromAnyProc_ProducesBlankHeaderWithHeadingsOnlySummary()
    {
        var db = new FakeDb();

        var row = Row(await Builder(db).BuildAsync(7, Ct));

        Assert.All(BlankColumns, col => Assert.Equal(string.Empty, row[col]));
        var summary = ((string)row["TestSummary"]).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var heading = Assert.Single(summary);
        Assert.Contains("Block Ref", heading);
        Assert.Equal(0, Assert.Single(db.Commands, c => c.Text == "GetQCNoteAntibodiesInformation").Parameters["SubmissionType"]);
    }

    [Fact]
    public async Task BuildAsync_OnlyAntibodyRows_UsesFirstAntibodyRowAndKeepsUnparseableDateRaw()
    {
        var db = new FakeDb
        {
            OnQuery = c => c.Text switch
            {
                "GetBatchQCNotes" => One(Header()),
                "GetQCNoteAntibodiesInformation" => One(Content("Ab note", "Carol", "not a date")),
                _ => null,
            },
        };

        var row = Row(await Builder(db).BuildAsync(7, Ct));

        Assert.Equal(("Ab note", "Carol", "not a date"), ((string)row["QCText"], (string)row["CreatedBy"], (string)row["DateCreated"]));
    }

    [Fact]
    public async Task BuildAsync_HeaderWithNullBatchTypeAndNullColumns_DefaultsToZeroAndBlank()
    {
        var header = Table(("QCNoteRef", typeof(int)), ("ID", typeof(int)), ("BatchType", typeof(int)), ("Species", typeof(string)));
        header.Rows.Add(5, 10, DBNull.Value, DBNull.Value);
        var db = new FakeDb { OnQuery = c => c.Text == "GetBatchQCNotes" ? One(header) : null };

        var row = Row(await Builder(db).BuildAsync(5, Ct));

        Assert.Equal("10", row["SubmissionNumber"]);
        Assert.Equal(string.Empty, row["Species"]);
    }
}
