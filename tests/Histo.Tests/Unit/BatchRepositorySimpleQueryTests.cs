using System.Data;
using Histo.Submissions.Repositories;
using Histo.Tests.Support;

namespace Histo.Tests.Unit;

/// <summary>Straight stored-procedure wrappers of <see cref="BatchRepository"/>: reports, dispatch status and completed-date correction.</summary>
public class BatchRepositorySimpleQueryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static DataSet Rows(string column, Type type, params object[] values)
    {
        var table = new DataTable();
        table.Columns.Add(column, type);
        foreach (var v in values) table.Rows.Add(v);
        var ds = new DataSet();
        ds.Tables.Add(table);
        return ds;
    }

    private static DataSet Dispatch(params (bool Dispatched, DateTime? Date)[] rows)
    {
        var table = new DataTable();
        table.Columns.Add("Dispatched", typeof(bool));
        table.Columns.Add("DispatchedDate", typeof(DateTime));
        foreach (var (dispatched, date) in rows) table.Rows.Add(dispatched, date.HasValue ? date.Value : DBNull.Value);
        var ds = new DataSet();
        ds.Tables.Add(table);
        return ds;
    }

    [Fact]
    public async Task GetTestItemRowsAsync_PassesFiltersAndMapsRows()
    {
        var table = new DataTable();
        table.Columns.Add("Description", typeof(string));
        table.Columns.Add("Count", typeof(int));
        table.Rows.Add("H&E", 3);
        var ds = new DataSet();
        ds.Tables.Add(table);
        var db = new FakeDb { OnQuery = c => c.Text == "GetTestRows" ? ds : null };

        var result = await new BatchRepository(db).GetTestItemRowsAsync("PROJ", 1, Ct);

        var row = Assert.Single(result);
        Assert.Equal("H&E", row.Description);
        Assert.Equal(3, row.Count);
        var cmd = Assert.Single(db.Commands, c => c.Text == "GetTestRows");
        Assert.Equal("PROJ", cmd.Parameters["ProjectContractDesc"]);
        Assert.Equal(1, cmd.Parameters["BatchType"]);
    }

    [Fact]
    public async Task GetBatchIdsLinkedToBlocksAsync_ReturnsAllIds()
    {
        var db = new FakeDb { OnQuery = c => c.Text == "GetBatchesLinkedToBlocks" ? Rows("ID", typeof(int), 4, 8) : null };

        var result = await new BatchRepository(db).GetBatchIdsLinkedToBlocksAsync(Ct);

        Assert.Equal([4, 8], result);
    }

    [Fact]
    public async Task GetBatchIdsLinkedToBlocksAsync_NoRows_ReturnsEmpty()
    {
        var result = await new BatchRepository(new FakeDb()).GetBatchIdsLinkedToBlocksAsync(Ct);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("GetHistologyDispatched")]
    [InlineData("GetStainDispatched")]
    [InlineData("GetAntibodiesDispatched")]
    public async Task DispatchStatusQueries_UseTheirOwnProcedureAndMapRows(string procedure)
    {
        var date = new DateTime(2026, 5, 6);
        var db = new FakeDb { OnQuery = c => c.Text == procedure ? Dispatch((true, date), (false, null)) : null };
        var repo = new BatchRepository(db);

        var result = procedure switch
        {
            "GetHistologyDispatched" => await repo.GetHistologyDispatchStatusAsync(42, Ct),
            "GetStainDispatched" => await repo.GetStainDispatchStatusAsync(42, Ct),
            _ => await repo.GetAntibodiesDispatchStatusAsync(42, Ct),
        };

        Assert.Equal(2, result.Count);
        Assert.True(result[0].Dispatched);
        Assert.Equal(date, result[0].DispatchedDate);
        Assert.False(result[1].Dispatched);
        Assert.Null(result[1].DispatchedDate);
        Assert.Equal(42, Assert.Single(db.Commands, c => c.Text == procedure).Parameters["BatchID"]);
    }

    [Fact]
    public async Task UpdateCompletedDateAsync_WritesDateForBatch()
    {
        var db = new FakeDb();
        var date = new DateTime(2026, 7, 8);

        await new BatchRepository(db).UpdateCompletedDateAsync(42, date, Ct);

        var cmd = Assert.Single(db.Commands, c => c.Text == "EditBatchCompletedDate");
        Assert.Equal(date, cmd.Parameters["CompletedDate"]);
        Assert.Equal(42, cmd.Parameters["BatchID"]);
    }
}
