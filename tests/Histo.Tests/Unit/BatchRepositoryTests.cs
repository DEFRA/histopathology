using System.Data;
using Histo.Submissions.Repositories;
using Histo.Tests.Support;

namespace Histo.Tests.Unit;

/// <summary>Batch status transitions and the unassigned-tissue check, exercised against <see cref="FakeDb"/>.</summary>
public class BatchRepositoryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static DataTable Rows(string name, (string Column, Type Type)[] columns, params object[][] rows)
    {
        var table = new DataTable(name);
        foreach (var (column, type) in columns) table.Columns.Add(column, type);
        foreach (var row in rows) table.Rows.Add(row);
        return table;
    }

    private static DataSet BatchHeader(int batchStatus, string comments, DateTime? completed)
    {
        var header = Rows("batch",
            [("ID", typeof(int)), ("BatchStatus", typeof(int)), ("Comments", typeof(string)), ("DateCompleted", typeof(DateTime))],
            [42, batchStatus, comments, completed ?? (object)DBNull.Value]);
        var ds = new DataSet();
        ds.Tables.Add(header);
        return ds;
    }

    private static DataSet Single(DataTable table)
    {
        var ds = new DataSet();
        ds.Tables.Add(table);
        return ds;
    }

    private static DataSet NoBatch() => Single(Rows("batch", [("ID", typeof(int))]));

    [Fact]
    public async Task SetCompletedAsync_WritesCompletedStatusAndDate_KeepingTheOtherFields()
    {
        var completedOn = new DateTime(2026, 10, 19);
        var db = new FakeDb { OnQuery = c => c.Text == "GetCommonBatchTablesByID" ? BatchHeader(2, "keep me", null) : null };

        await new BatchRepository(db).SetCompletedAsync(42, completedOn, 7, Ct);

        var edit = Assert.Single(db.Commands, c => c.Text == "EditBatch");
        Assert.Equal(4, edit.Parameters["BatchStatus"]);
        Assert.Equal(completedOn, edit.Parameters["DateCompleted"]);
        Assert.Equal("keep me", edit.Parameters["Comments"]);
        Assert.Equal(7, edit.Parameters["UserID"]);
    }

    [Fact]
    public async Task SetCompletedAsync_BatchNotFound_WritesNothing()
    {
        var db = new FakeDb { OnQuery = _ => NoBatch() };

        await new BatchRepository(db).SetCompletedAsync(42, new DateTime(2026, 10, 19), 7, Ct);

        Assert.DoesNotContain(db.Commands, c => c.Text == "EditBatch");
    }

    [Fact]
    public async Task SetInProgressAsync_WritesInProgressStatus_KeepingTheExistingCompletedDate()
    {
        var existingCompleted = new DateTime(2026, 9, 1);
        var db = new FakeDb { OnQuery = c => c.Text == "GetCommonBatchTablesByID" ? BatchHeader(4, "note", existingCompleted) : null };

        await new BatchRepository(db).SetInProgressAsync(42, 7, Ct);

        var edit = Assert.Single(db.Commands, c => c.Text == "EditBatch");
        Assert.Equal(6, edit.Parameters["BatchStatus"]);
        Assert.Equal(existingCompleted, edit.Parameters["DateCompleted"]);
        Assert.Equal("note", edit.Parameters["Comments"]);
    }

    [Fact]
    public async Task SetInProgressAsync_BatchNotFound_WritesNothing()
    {
        var db = new FakeDb { OnQuery = _ => NoBatch() };

        await new BatchRepository(db).SetInProgressAsync(42, 7, Ct);

        Assert.DoesNotContain(db.Commands, c => c.Text == "EditBatch");
    }

    [Fact]
    public async Task GetAnimalsWithUnassignedTissuesAsync_ReturnsAnimalsWhoseTissueHasNoBlockTissue()
    {
        var db = new FakeDb
        {
            OnQuery = c => c.Text switch
            {
                // Block 10 belongs to animal 1, block 20 to animal 2; row without an animal is ignored.
                "GetBatchBlockDetails" => Single(Rows("blocks", [("ID", typeof(int)), ("AnimalID", typeof(int))],
                    [10, 1], [20, 2], [30, 0])),
                // Animal 1 has brain assigned (case/whitespace differ); animal 2 has nothing assigned; block 99 is unknown.
                "GetBatchBlockTissues" => Single(Rows("assigned", [("BlockID", typeof(int)), ("TissueCode", typeof(string))],
                    [10, " brain "], [10, DBNull.Value], [99, "liver"])),
                "GetBatchTissues" => Single(Rows("tissues", [("AnimalID", typeof(int)), ("TissueCode", typeof(string))],
                    [1, "BRAIN"], [1, "spleen"], [2, "brain"], [3, ""], [0, "brain"])),
                _ => null,
            },
        };

        var result = await new BatchRepository(db).GetAnimalsWithUnassignedTissuesAsync(5, Ct);

        Assert.Equal([1, 2], result.Order());
    }

    [Fact]
    public async Task GetAnimalsWithUnassignedTissuesAsync_EveryTissueAssigned_ReturnsNone()
    {
        var db = new FakeDb
        {
            OnQuery = c => c.Text switch
            {
                "GetBatchBlockDetails" => Single(Rows("blocks", [("ID", typeof(int)), ("AnimalID", typeof(int))], [10, 1])),
                "GetBatchBlockTissues" => Single(Rows("assigned", [("BlockID", typeof(int)), ("TissueCode", typeof(string))], [10, "brain"])),
                "GetBatchTissues" => Single(Rows("tissues", [("AnimalID", typeof(int)), ("TissueCode", typeof(string))], [1, "brain"])),
                _ => null,
            },
        };

        Assert.Empty(await new BatchRepository(db).GetAnimalsWithUnassignedTissuesAsync(5, Ct));
    }
}
