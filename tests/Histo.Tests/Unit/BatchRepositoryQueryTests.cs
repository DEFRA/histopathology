using System.Data;
using Histo.Submissions.Models;
using Histo.Submissions.Repositories;
using Histo.Tests.Support;

namespace Histo.Tests.Unit;

/// <summary>BatchRepository readers, search mapping, add and test-selection deltas against <see cref="FakeDb"/>.</summary>
public class BatchRepositoryQueryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static DataTable Table(params (string Column, Type Type)[] columns)
    {
        var table = new DataTable();
        foreach (var (column, type) in columns) table.Columns.Add(column, type);
        return table;
    }

    private static DataTable Selections(params (int Id, string Code)[] rows)
    {
        var t = Table(("ID", typeof(int)), ("BatchID", typeof(int)), ("Code", typeof(string)));
        foreach (var (id, code) in rows) t.Rows.Add(id, 5, code);
        return t;
    }

    private static DataSet Sets(params DataTable[] tables)
    {
        var ds = new DataSet();
        foreach (var t in tables) ds.Tables.Add(t);
        return ds;
    }

    private static DataTable Header()
    {
        var t = Table(("ID", typeof(int)), ("BatchStatus", typeof(int)));
        t.Rows.Add(5, 1);
        return t;
    }

    private static DataTable Filler() => Table(("ID", typeof(int)));

    private static DataTable SubmittedAs(string code)
    {
        var t = Table(("Code", typeof(string)));
        t.Rows.Add(code);
        return t;
    }

    [Fact]
    public async Task GetById_SubmittedAsCodeFive_MarksBatchPreCassetted()
    {
        var db = new FakeDb
        {
            OnQuery = _ => Sets(Header(), Filler(), Filler(), Filler(), Filler(), SubmittedAs("5 ")),
        };

        var batch = await new BatchRepository(db).GetByIdAsync(5, Ct);

        Assert.NotNull(batch);
        Assert.True(batch.IsPreCassetted);
    }

    [Fact]
    public async Task StatusListReaders_CallTheirProcWithTheMatchingBatchStatus()
    {
        var rows = Table(("ID", typeof(int)));
        rows.Rows.Add(1);
        var db = new FakeDb { OnQuery = _ => Sets(rows.Copy()) };
        var repo = new BatchRepository(db);

        var results = new[]
        {
            await repo.GetReceivedAsync(Ct),
            await repo.GetInProgressAsync(Ct),
            await repo.GetNotReceivedAsync(Ct),
            await repo.GetAllBatchesAsync(Ct),
            await repo.GetOnHoldAsync(Ct),
            await repo.GetCompletedAsync(Ct),
            await repo.GetForDispatchAsync(Ct),
        };

        Assert.All(results, r => Assert.Equal(1, Assert.Single(r).ID));
        Assert.Equal(
            ["GetBatchesToBeBlocked", "GetBatchesWithStatus", "GetBatchesWithStatus", "GetBatchesWithStatus", "GetBatchesWithStatus", "GetBatchesWithStatus", "GetBatchesForDispatch"],
            db.Commands.Select(c => c.Text));
        Assert.Equal([6, 1, 0, 5, 4], db.Commands.Where(c => c.Parameters.ContainsKey("BatchStatus")).Select(c => c.Parameters["BatchStatus"]));
    }

    [Fact]
    public async Task AddAsync_OutputBatchIdReturned_AndDefaultsAppliedToTheParameters()
    {
        var db = new FakeDb { OnExecute = c => c.Command.Parameters["BatchID"].Value = 77 };

        var id = await new BatchRepository(db).AddAsync(new Batch { Status = "1" }, 7, Ct);

        Assert.Equal(77, id);
        var add = Assert.Single(db.Commands, c => c.Text == "AddBatch");
        Assert.Equal(7, add.Parameters["SubmittedBy"]);
        Assert.False(Assert.IsType<bool>(add.Parameters["SafeToHandle"]));
        Assert.Equal(1, add.Parameters["BatchStatus"]);
        Assert.Equal(string.Empty, add.Parameters["Species"]);
        Assert.Equal(0, add.Parameters["OtherSubmittedBy"]);
        Assert.Equal(DBNull.Value, add.Parameters["SubmittedArea"]);
    }

    [Fact]
    public async Task AddAsync_BatchCarriesSubmitterAndArea_UsesThemInsteadOfTheDefaults()
    {
        var db = new FakeDb { OnExecute = c => c.Command.Parameters["BatchID"].Value = 78 };
        var batch = new Batch { SubmittedByUserID = 9, UserAreaCode = 4, SafeToHandle = true, Species = "Bovine", OtherSubmittedBy = 3 };

        await new BatchRepository(db).AddAsync(batch, 7, Ct);

        var add = Assert.Single(db.Commands, c => c.Text == "AddBatch");
        Assert.Equal(9, add.Parameters["SubmittedBy"]);
        Assert.Equal("4", add.Parameters["SubmittedArea"]);
        Assert.True(Assert.IsType<bool>(add.Parameters["SafeToHandle"]));
        Assert.Equal("Bovine", add.Parameters["Species"]);
        Assert.Equal(3, add.Parameters["OtherSubmittedBy"]);
    }

    [Fact]
    public async Task AddAsync_StoredProcedureReportsNoId_ReturnsZero()
    {
        var db = new FakeDb { OnExecute = c => c.Command.Parameters["BatchID"].Value = 0 };

        Assert.Equal(0, await new BatchRepository(db).AddAsync(new Batch(), 7, Ct));
    }

    [Fact]
    public async Task GetCommentsAsync_ReturnsEveryCommentForTheBatch()
    {
        var t = Table(("Comment", typeof(string)));
        t.Rows.Add("first");
        t.Rows.Add("second");
        var db = new FakeDb { OnQuery = _ => Sets(t) };

        var comments = await new BatchRepository(db).GetCommentsAsync(5, Ct);

        Assert.Equal(["first", "second"], comments);
        Assert.Equal(5, db.Commands[0].Parameters["ID"]);
    }

    [Fact]
    public async Task SearchAsync_MapsColumns_ParsesTextDates_AndPrefersNumericStatus()
    {
        var t = Table(("ID", typeof(int)), ("ProjectDescription", typeof(string)), ("ContactDescription", typeof(string)), ("Species", typeof(string)),
            ("BatchDate", typeof(string)), ("DateReceived", typeof(DateTime)), ("DateCompleted", typeof(string)), ("CustomerReceivedDate", typeof(string)),
            ("BatchStatus", typeof(int)), ("Status", typeof(string)), ("SubmittedBy", typeof(string)), ("BatchType", typeof(string)),
            ("SafeToHandle", typeof(string)), ("ReceivedTime", typeof(string)), ("ReceivedBy", typeof(string)),
            ("OtherSubmittedBy", typeof(string)), ("Comments", typeof(string)));
        t.Rows.Add(1, "Proj", "Contact", "Bovine", "04/03/2026", new DateTime(2026, 3, 5), "", "06/03/2026 10:30:00",
            4, "Ignored", "Ann", "TSE", "Yes", "09:00", "Bob", "Cust", "Note");
        t.Rows.Add(2, DBNull.Value, DBNull.Value, DBNull.Value, "not a date", DBNull.Value, DBNull.Value, DBNull.Value,
            0, "Submitted", DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);
        var db = new FakeDb { OnQuery = _ => Sets(t) };

        var results = await new BatchRepository(db).SearchAsync(new BatchSearchCriteria(), Ct);

        var first = results[0];
        Assert.Equal(("Proj", "Contact", "Bovine", "4", "Ann", "TSE", "Yes", "09:00", "Bob", "Cust", "Note"),
            (first.ProjectDescription, first.ContactDescription, first.Species, first.Status, first.SubmittedBy, first.BatchType,
                first.SafeToHandle, first.ReceivedTime, first.ReceivedBy, first.OtherSubmittedBy, first.Comments));
        Assert.Equal(new DateTime(2026, 3, 4), first.BatchDate);
        Assert.Equal(new DateTime(2026, 3, 5), first.DateReceived);
        Assert.Null(first.DateCompleted);
        Assert.Equal(new DateTime(2026, 3, 6, 10, 30, 0), first.CustomerReceivedDate);
        Assert.Equal("Submitted", results[1].Status);
        Assert.Null(results[1].BatchDate);
        Assert.Null(results[1].Species);
    }

    [Fact]
    public async Task GetBatchTestSelectionsAsync_SplitsHistologyAntibodiesAndStains()
    {
        var db = new FakeDb
        {
            OnQuery = _ => Sets(Header(), Selections((1, "H1")), Selections((2, "A1"), (3, "A2")), Selections((4, "S1"))),
        };

        var selections = await new BatchRepository(db).GetBatchTestSelectionsAsync(5, Ct);

        Assert.Equal(["H1"], selections.Histology.Select(r => r.Code));
        Assert.Equal(["A1", "A2"], selections.Antibodies.Select(r => r.Code));
        Assert.Equal(["S1"], selections.Stains.Select(r => r.Code));
    }

    [Fact]
    public async Task GetSubmittedAsCodeAsync_ReturnsTrimmedCode_OrNullWhenMissingOrUnreadable()
    {
        var withCode = new FakeDb { OnQuery = _ => Sets(Header(), Filler(), Filler(), Filler(), Filler(), SubmittedAs("5  ")) };
        var emptySet = new FakeDb { OnQuery = _ => Sets(Header(), Filler(), Filler(), Filler(), Filler(), SubmittedAs("x").Clone()) };
        var tooFewSets = new FakeDb { OnQuery = _ => Sets(Header()) };

        Assert.Equal("5", await new BatchRepository(withCode).GetSubmittedAsCodeAsync(5, Ct));
        Assert.Null(await new BatchRepository(emptySet).GetSubmittedAsCodeAsync(5, Ct));
        Assert.Null(await new BatchRepository(tooFewSets).GetSubmittedAsCodeAsync(5, Ct));
    }

    [Fact]
    public async Task SaveSubmittedAsAsync_CallsAddSubmittedAsWithCode()
    {
        var db = new FakeDb();

        await new BatchRepository(db).SaveSubmittedAsAsync(5, "3", 7, Ct);

        var cmd = Assert.Single(db.Commands, c => c.Text == "AddSubmittedAs");
        Assert.Equal(("3", 7, 5), (cmd.Parameters["Code"], cmd.Parameters["UserID"], cmd.Parameters["BatchID"]));
    }

    [Fact]
    public async Task SaveBatchTestSelectionsAsync_DeletesRemovedCodes_AndAddsEachNewCodeOnce()
    {
        var db = new FakeDb
        {
            OnQuery = _ => Sets(Header(), Selections((1, "H1"), (2, "H2")), Selections(), Selections((3, "S1"))),
        };

        await new BatchRepository(db).SaveBatchTestSelectionsAsync(5, ["h1", "H3"], ["A1", "a1"], [], 7, Ct);

        var changes = db.Commands.Where(c => c.Text != "GetCommonBatchTablesByID").ToList();
        Assert.Equal(["DeleteHistology", "AddHistology", "AddAntibodies", "DeleteSpecialStain"], changes.Select(c => c.Text));
        Assert.Equal(2, changes[0].Parameters["ID"]);
        Assert.Equal("H3", changes[1].Parameters["Code"]);
        Assert.Equal("A1", changes[2].Parameters["Code"]);
        Assert.Equal(3, changes[3].Parameters["ID"]);
    }

    [Fact]
    public async Task PostFixationCodes_AreReadFromResultSetFour_AndSavedAsADelta()
    {
        var db = new FakeDb
        {
            OnQuery = _ => Sets(Header(), Filler(), Filler(), Filler(), Selections((1, "P1"), (2, "P2"))),
        };
        var repo = new BatchRepository(db);

        var codes = await repo.GetPostFixationCodesAsync(5, Ct);
        await repo.SavePostFixationCodesAsync(5, ["P2", "P3"], 7, Ct);

        Assert.Equal(["P1", "P2"], codes);
        var changes = db.Commands.Where(c => c.Text is "AddPostFixation" or "DeletePostFixation").ToList();
        Assert.Equal(["DeletePostFixation", "AddPostFixation"], changes.Select(c => c.Text));
        Assert.Equal(1, changes[0].Parameters["ID"]);
        Assert.Equal("P3", changes[1].Parameters["Code"]);
    }
}
