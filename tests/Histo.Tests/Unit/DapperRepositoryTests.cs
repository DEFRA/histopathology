using System.Data;
using Histo.Core.Domain;
using Histo.Histology.Models;
using Histo.Histology.Repositories;
using Histo.QualityControl.Repositories;
using Histo.Tests.Support;

namespace Histo.Tests.Unit;

/// <summary>Repository SQL-shaping logic, exercised against <see cref="FakeDb"/> instead of a real database.</summary>
public class DapperRepositoryTests
{
    private static readonly string[] HistologyH2 = ["2"];
    private static readonly string[] NoCodes = [];

    private static DataTable Table(string name, params (string Column, Type Type)[] columns)
    {
        var table = new DataTable(name);
        foreach (var (column, type) in columns) table.Columns.Add(column, type);
        return table;
    }

    private static DataSet BatchBlockTests(int[] blockIds, (int BlockID, string Code)[] histology)
    {
        var blocks = Table("blocks", ("BlockID", typeof(int)));
        foreach (var id in blockIds) blocks.Rows.Add(id);

        var hist = Table("histology", ("BlockID", typeof(int)), ("Code", typeof(string)));
        foreach (var (blockId, code) in histology) hist.Rows.Add(blockId, code);

        var ds = new DataSet();
        ds.Tables.Add(blocks);
        ds.Tables.Add(hist);
        ds.Tables.Add(Table("antibodies", ("BlockID", typeof(int)), ("Code", typeof(string))));
        ds.Tables.Add(Table("stains", ("BlockID", typeof(int)), ("Code", typeof(string))));
        return ds;
    }

    [Fact]
    public async Task AddTestsToBatchBlocksAsync_AddsOnlyWhatEachBlockLacks_AndCommits()
    {
        var db = new FakeDb
        {
            OnQuery = c => c.Text == "GetBatchBlockTests" ? BatchBlockTests([1, 2], [(1, "2")]) : null,
        };

        await new BlockTestRepository(db).AddTestsToBatchBlocksAsync(5, HistologyH2, NoCodes, NoCodes, 7);

        var added = db.Commands.Where(c => c.Text == "AddBlockHistology").ToList();
        var only = Assert.Single(added);
        Assert.Equal(2, only.Parameters["BlockID"]);
        Assert.Equal("2", only.Parameters["Code"]);
        Assert.True(db.Committed);
        Assert.False(db.RolledBack);
    }

    [Fact]
    public async Task AddTestsToBatchBlocksAsync_NothingToAdd_TouchesNoDatabase()
    {
        var db = new FakeDb();

        await new BlockTestRepository(db).AddTestsToBatchBlocksAsync(5, NoCodes, NoCodes, NoCodes, 7);

        Assert.Empty(db.Commands);
    }

    [Fact]
    public async Task AddTestsToBatchBlocksAsync_InsertFails_RollsBackAndRethrows()
    {
        var db = new FakeDb
        {
            OnQuery = c => c.Text == "GetBatchBlockTests" ? BatchBlockTests([2], []) : null,
            OnExecute = c => { if (c.Text == "AddBlockHistology") throw new InvalidOperationException("boom"); },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new BlockTestRepository(db).AddTestsToBatchBlocksAsync(5, HistologyH2, NoCodes, NoCodes, 7));

        Assert.True(db.RolledBack);
        Assert.False(db.Committed);
    }

    [Fact]
    public async Task QCNoteGetAllAsync_CollapsesOneRowPerStainRefIntoOneRowPerNote()
    {
        var rows = Table("notes", ("ID", typeof(int)), ("QCNoteRef", typeof(int)), ("ProjectDescription", typeof(string)),
            ("Species", typeof(string)), ("StainRef", typeof(string)));
        rows.Rows.Add(1, 10, "Project A", "Bovine", "S1 ");
        rows.Rows.Add(2, 10, "Project A", "Bovine", "s1");
        rows.Rows.Add(3, 10, "Project A", "Bovine", "S2");
        rows.Rows.Add(4, 11, "Project B", "Ovine", DBNull.Value);
        var db = new FakeDb { OnQuery = _ => new DataSet().Also(ds => ds.Tables.Add(rows)) };

        var notes = await new QCNoteRepository(db).GetAllAsync();

        Assert.Equal(2, notes.Count);
        var first = notes.Single(n => n.QCNoteRef == 10);
        Assert.Equal("Project A", first.ProjectDescription);
        Assert.Equal("S1, S2", first.StainRef);
        Assert.Equal(string.Empty, notes.Single(n => n.QCNoteRef == 11).StainRef);
    }

    [Fact]
    public async Task BlockSaveAsync_NewBlockWhoseRefIsTakenElsewhere_InsertsTheNextFreeRefInOneTransaction()
    {
        var db = new FakeDb
        {
            OnQuery = c => c.Text switch
            {
                "GetAnimalBlockRefsOnOtherBatches" => new DataSet().Also(ds => ds.Tables.Add(RefTable("01"))),
                "GetBatchAnimal" => new DataSet().Also(ds => ds.Tables.Add(
                    Table("animals", ("ID", typeof(int)), ("SenderRef", typeof(string))).Also(t => t.Rows.Add(2, "S1 ")))),
                "GetBlocksForSenderRef" => new DataSet().Also(ds => ds.Tables.Add(
                    Table("held", ("BlockRef", typeof(int))).Also(t => t.Rows.Add(1)))),
                _ => null,
            },
            OnExecute = c =>
            {
                if (c.Text == "sp_getapplock") c.Command.Parameters["RETURN_VALUE"].Value = 0;
                if (c.Text == "AddBlock") c.Command.Parameters["NewID"].Value = 77;
            },
        };
        var block = new Block { ID = 0, BatchID = 5, AnimalID = 2, BlockRef = "01", Status = BlockStatus.Used };

        var id = await new BlockRepository(db).SaveAsync(block, 7);

        Assert.Equal(77, id);
        var insert = Assert.Single(db.Commands, c => c.Text == "AddBlock");
        Assert.Equal("02", insert.Parameters["BlockRef"]);
        Assert.True(db.Committed);
    }

    [Fact]
    public async Task BlockTestRepository_BatchWithNoBlocks_ReturnsNoTests_ForBothReaders()
    {
        var db = new FakeDb
        {
            OnQuery = c =>
            {
                var ds = new DataSet();
                for (var i = 0; i < 10; i++) ds.Tables.Add(Table($"set{i}", ("ID", typeof(int))));
                return c.Text == "GetBatchBlocksByID" ? ds : null;
            },
        };
        var repo = new BlockTestRepository(db);

        Assert.Empty(await repo.GetByBatchAsync(5));
        Assert.Empty(await repo.GetAllSelectionsByBatchAsync(5));
    }

    [Fact]
    public async Task BlockCreatePreBookedBlockAsync_InsertsAPreBookedPlaceholderForTheAnimal()
    {
        var db = new FakeDb();

        await new BlockRepository(db).CreatePreBookedBlockAsync(2, "05");

        var insert = Assert.Single(db.Commands, c => c.Text == "AddBlock");
        Assert.Equal(2, insert.Parameters["AnimalID"]);
        Assert.Equal("05", insert.Parameters["BlockRef"]);
        Assert.Equal(BlockStatus.PreBooked, insert.Parameters["Status"]);
    }

    [Fact]
    public async Task BlockBookPreBookedBlocksAsync_BooksFreeRefsAgainstANewAnimalAndSkipsTakenOnes()
    {
        var db = new FakeDb
        {
            OnQuery = c => c.Text == "GetBlocksForSenderRef"
                ? new DataSet().Also(ds => ds.Tables.Add(Table("taken", ("BlockRef", typeof(int))).Also(t => t.Rows.Add(1))))
                : null,
            OnExecute = c =>
            {
                if (c.Text == "sp_getapplock") c.Command.Parameters["RETURN_VALUE"].Value = 0;
                if (c.Text == "AddAnimal") c.Command.Parameters["NewID"].Value = 9;
            },
        };

        var results = await new BlockRepository(db).BookPreBookedBlocksAsync("S1", [1, 2, 3]);

        Assert.Equal(
            [PreBookedBlockOutcome.AlreadyExists, PreBookedBlockOutcome.Booked, PreBookedBlockOutcome.Booked],
            results.Select(r => r.Outcome));
        Assert.All(db.Commands.Where(c => c.Text == "AddBlock"), c => Assert.Equal(9, c.Parameters["AnimalID"]));
        Assert.Single(db.Commands, c => c.Text == "AddAnimal");
        Assert.True(db.Committed);
    }

    [Fact]
    public async Task BlockBookPreBookedBlocksAsync_AnimalCannotBeCreated_ReportsNoSample()
    {
        var db = new FakeDb
        {
            OnExecute = c => { if (c.Text == "sp_getapplock") c.Command.Parameters["RETURN_VALUE"].Value = 0; },
        };

        var results = await new BlockRepository(db).BookPreBookedBlocksAsync("S1", [4]);

        Assert.Equal(PreBookedBlockOutcome.NoSample, Assert.Single(results).Outcome);
    }

    private static DataTable RefTable(params string[] refs)
    {
        var table = Table("refs", ("BlockRef", typeof(string)));
        foreach (var r in refs) table.Rows.Add(r);
        return table;
    }
}

internal static class DataSetExtensions
{
    public static DataTable Also(this DataTable table, Action<DataTable> configure)
    {
        configure(table);
        return table;
    }

    public static DataSet Also(this DataSet ds, Action<DataSet> configure)
    {
        configure(ds);
        return ds;
    }
}
