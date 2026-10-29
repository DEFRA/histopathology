using System.Data;
using Histo.Histology.Repositories;
using Histo.Tests.Support;

namespace Histo.Tests.Unit;

/// <summary><see cref="HistologyRepository"/> stored-procedure wrappers against <see cref="FakeDb"/>.</summary>
public class HistologyRepositoryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static DataSet Single(DataTable table) => new DataSet().Also(ds => ds.Tables.Add(table));

    private static DataTable UnusedPool(params (object? Ref, object? Sender)[] rows)
    {
        var t = new DataTable();
        t.Columns.Add("HistologyRef", typeof(string));
        t.Columns.Add("SenderRef", typeof(string));
        foreach (var (r, s) in rows) t.Rows.Add(r ?? DBNull.Value, s ?? DBNull.Value);
        return t;
    }

    [Fact]
    public async Task GetUnusedRefs_KeepsOnlyRefsInTheRequestedTypeRange_AndMapsFields()
    {
        var db = new FakeDb
        {
            OnQuery = _ => Single(UnusedPool(
                ("26/10001 ", "S1"),    // Neuropath
                ("26/20001", null),     // AbattoirSurvey
                ("HP", "S3"),           // unclassifiable
                (null, "S4"))),         // blank ref
        };

        var result = await new HistologyRepository(db).GetUnusedRefsAsync(1, Ct);

        var item = Assert.Single(result);
        Assert.Equal("26/10001", item.Ref);
        Assert.Equal(1, item.HistologyType);
        Assert.Equal("S1", item.SenderRef);
        Assert.Equal("GetUnusedHistologyRefs", db.Commands[0].Text);
        Assert.Empty(db.Commands[0].Parameters);
    }

    [Fact]
    public async Task GetUnusedRefs_NullSenderRef_MapsToEmptyString()
    {
        var db = new FakeDb { OnQuery = _ => Single(UnusedPool(("26/20001", null))) };

        var item = Assert.Single(await new HistologyRepository(db).GetUnusedRefsAsync(2, Ct));

        Assert.Equal(string.Empty, item.SenderRef);
    }

    [Fact]
    public async Task GetUsedRefsByBatch_PassesBatchIdAndMapsRows()
    {
        var t = new DataTable();
        t.Columns.Add("Ref", typeof(string));
        t.Columns.Add("HistologyType", typeof(int));
        t.Rows.Add("26/10001", 1);
        var db = new FakeDb { OnQuery = _ => Single(t) };

        var item = Assert.Single(await new HistologyRepository(db).GetUsedRefsByBatchAsync(12, Ct));

        Assert.Equal("26/10001", item.Ref);
        Assert.Equal(1, item.HistologyType);
        Assert.Equal("GetUsedHistologyRefsByBatchID", db.Commands[0].Text);
        Assert.Equal(12, db.Commands[0].Parameters["ID"]);
    }

    [Fact]
    public async Task GetUnusedBookedRefs_CallsBookedProc()
    {
        var t = new DataTable();
        t.Columns.Add("Ref", typeof(string));
        t.Rows.Add("26/40001");
        var db = new FakeDb { OnQuery = _ => Single(t) };

        var item = Assert.Single(await new HistologyRepository(db).GetUnusedBookedRefsAsync(Ct));

        Assert.Equal("26/40001", item.Ref);
        Assert.Equal("GetUnUsedBookedHistologyRefs", db.Commands[0].Text);
    }

    [Fact]
    public async Task GetAllUnusedRefs_TrimsAndOrdersOrdinally_AndBlanksNullRefs()
    {
        var db = new FakeDb
        {
            OnQuery = _ => Single(UnusedPool(("26/20002 ", null), ("26/10001", null), (null, null))),
        };

        var result = await new HistologyRepository(db).GetAllUnusedRefsAsync(Ct);

        Assert.Equal(["", "26/10001", "26/20002"], result.Select(r => r.Ref));
    }

    [Fact]
    public async Task GetCounters_MapsCounterRows()
    {
        var t = new DataTable();
        t.Columns.Add("Type", typeof(int));
        t.Columns.Add("Description", typeof(string));
        t.Columns.Add("NextHistologyRef", typeof(string));
        t.Rows.Add(3, "TB", "30001");
        var db = new FakeDb { OnQuery = _ => Single(t) };

        var counter = Assert.Single(await new HistologyRepository(db).GetCountersAsync(Ct));

        Assert.Equal((3, "TB", "30001"), (counter.Type, counter.Description, counter.NextHistologyRef));
        Assert.Equal("GetHistologyRefs", db.Commands[0].Text);
    }

    [Fact]
    public async Task UpdateCounter_PassesTypeNextRefAndRowStamp()
    {
        var db = new FakeDb();
        var stamp = new byte[] { 1, 2 };

        await new HistologyRepository(db).UpdateCounterAsync(3, "30002", stamp, Ct);

        var cmd = Assert.Single(db.Commands);
        Assert.Equal("EditHistologyRef", cmd.Text);
        Assert.Equal(3, cmd.Parameters["Type"]);
        Assert.Equal("30002", cmd.Parameters["NextHistologyRef"]);
        Assert.Equal(stamp, cmd.Parameters["RowStamp"]);
    }

    [Fact]
    public async Task DrawNextRef_UnusedNumber_ReturnsTrimmedDrawnRef()
    {
        var db = new FakeDb
        {
            OnExecute = c => { if (c.Text == "GetNextHistologyRef") c.Command.Parameters["NextHistologyRef"].Value = "40001 "; },
            ScalarResult = 0,
        };

        var drawn = await new HistologyRepository(db).DrawNextRefAsync(4, Ct);

        Assert.Equal("40001", drawn);
        Assert.Equal(4, db.Commands[0].Parameters["Type"]);
        Assert.Equal("CountAnimalsByHistologyRef", db.Commands[1].Text);
        Assert.EndsWith("/40001", (string)db.Commands[1].Parameters["HistologyRef"]!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DrawNextRef_ProcReturnsNoRef_ReturnsNull(string? next)
    {
        var db = new FakeDb
        {
            OnExecute = c => { if (c.Text == "GetNextHistologyRef") c.Command.Parameters["NextHistologyRef"].Value = next; },
        };

        Assert.Null(await new HistologyRepository(db).DrawNextRefAsync(4, Ct));
        Assert.Single(db.Commands);
    }

    [Fact]
    public async Task DrawNextRef_EveryNumberAlreadyTaken_GivesUpAfterMaxAttempts()
    {
        var db = new FakeDb
        {
            OnExecute = c => { if (c.Text == "GetNextHistologyRef") c.Command.Parameters["NextHistologyRef"].Value = "40001"; },
            ScalarResult = 1,
        };

        Assert.Null(await new HistologyRepository(db).DrawNextRefAsync(4, Ct));
        Assert.Equal(2000, db.Commands.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task DrawNextRefRun_NonPositiveCount_ReturnsEmptyWithoutQuerying(int count)
    {
        var db = new FakeDb();

        Assert.Empty(await new HistologyRepository(db).DrawNextRefRunAsync(5, count, 60000, 90000, Ct));
        Assert.Empty(db.Commands);
    }

    [Fact]
    public async Task DrawNextRefRun_ReturnsZeroPaddedConsecutiveNumbers()
    {
        var db = new FakeDb { ScalarResult = 60007 };

        var run = await new HistologyRepository(db).DrawNextRefRunAsync(5, 3, 60000, 90000, Ct);

        Assert.Equal(["60007", "60008", "60009"], run);
        var p = db.Commands[0].Parameters;
        Assert.Equal((5, 3, 60000, 90000), ((int)p["Type"]!, (int)p["Count"]!, (int)p["Lower"]!, (int)p["Upper"]!));
        Assert.Equal(50, p["MaxAttempts"]);
    }

    [Fact]
    public async Task DrawNextRefRun_NoRunAvailable_ReturnsEmpty()
    {
        var db = new FakeDb { ScalarResult = null };

        Assert.Empty(await new HistologyRepository(db).DrawNextRefRunAsync(5, 3, 60000, 90000, Ct));
    }
}
