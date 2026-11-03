using System.Data;
using Histo.Histology.Models;
using Histo.Histology.Repositories;
using Histo.Tests.Support;

namespace Histo.Tests.Unit;

/// <summary><see cref="BlockRepository"/> reads, guarded saves and archive searches against <see cref="FakeDb"/>.</summary>
public class BlockRepositoryCoverageTests
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
        foreach (var t in tables) ds.Tables.Add(t.Copy());
        return ds;
    }

    private static DataTable Animals(params (int Id, string Sender)[] rows)
    {
        var t = Table(("ID", typeof(int)), ("SenderRef", typeof(string)));
        foreach (var (id, sender) in rows) t.Rows.Add(id, sender);
        return t;
    }

    private static DataTable Strings(params string[] values)
    {
        var t = Table(("BlockRef", typeof(string)));
        foreach (var v in values) t.Rows.Add(v);
        return t;
    }

    private static DataTable Ints(params int[] values)
    {
        var t = Table(("BlockRef", typeof(int)));
        foreach (var v in values) t.Rows.Add(v);
        return t;
    }

    private static void Out(FakeDb.ExecutedCommand c, string name, object value)
    {
        if (c.Command.Parameters.Contains(name)) c.Command.Parameters[name].Value = value;
    }

    private static Block NewBlock(int id = 0, string blockRef = "01", int status = BlockStatus.Used, bool repeat = false) =>
        new() { ID = id, BatchID = 5, AnimalID = 8, BlockRef = blockRef, Status = status, RepeatBlock = repeat, Order = 2, CustomerRef = "C", Comment = "cm" };

    // ---------------- simple reads ----------------

    [Fact]
    public async Task GetByBatch_PassesBatchIdAndMapsBlocks()
    {
        var t = Table(("ID", typeof(int)), ("BlockRef", typeof(string)), ("Status", typeof(int)));
        t.Rows.Add(3, "02", 1);
        var db = new FakeDb { OnQuery = _ => Sets(t) };

        var block = Assert.Single(await new BlockRepository(db).GetByBatchAsync(5, Ct));

        Assert.Equal((3, "02", 1), (block.ID, block.BlockRef, block.Status));
        Assert.Equal("GetBatchBlockDetails", db.Commands[0].Text);
        Assert.Equal(5, db.Commands[0].Parameters["ID"]);
    }

    [Fact]
    public async Task GetBlockRefsOnOtherBatches_PassesAnimalAndExcludedBatch()
    {
        var db = new FakeDb { OnQuery = _ => Sets(Strings("01", "02")) };

        var refs = await new BlockRepository(db).GetBlockRefsOnOtherBatchesAsync(8, 5, Ct);

        Assert.Equal(["01", "02"], refs);
        Assert.Equal((8, 5), ((int)db.Commands[0].Parameters["AnimalID"]!, (int)db.Commands[0].Parameters["BatchID"]!));
    }

    [Fact]
    public async Task GetPreBookedByAnimal_CallsPreBookedProc()
    {
        var t = Table(("ID", typeof(int)), ("BlockRef", typeof(string)));
        t.Rows.Add(9, "07");
        var db = new FakeDb { OnQuery = _ => Sets(t) };

        var block = Assert.Single(await new BlockRepository(db).GetPreBookedByAnimalAsync(8, Ct));

        Assert.Equal("07", block.BlockRef);
        Assert.Equal("GetAnimalPreBookedBlocks", db.Commands[0].Text);
        Assert.Equal(8, db.Commands[0].Parameters["AnimalID"]);
    }

    // ---------------- Save ----------------

    private static FakeDb SaveDb(
        DataSet? batchAnimal = null,
        DataSet? batchBlocks = null,
        DataSet? otherRefs = null,
        DataSet? heldRefs = null,
        int lockResult = 0,
        int newId = 77) =>
        new()
        {
            OnQuery = c => c.Text switch
            {
                "GetBatchAnimal" => batchAnimal,
                "GetBatchBlocksByID" => batchBlocks,
                "GetAnimalBlockRefsOnOtherBatches" => otherRefs,
                "GetBlocksForSenderRef" => heldRefs,
                _ => null,
            },
            OnExecute = c =>
            {
                Out(c, "RETURN_VALUE", lockResult);
                Out(c, "NewID", newId);
            },
        };

    [Fact]
    public async Task Save_NewUsedBlock_SenderOnBatchAndRefFree_InsertsUnderLockAndCommits()
    {
        var db = SaveDb(batchAnimal: Sets(Animals((8, " S1 "))), otherRefs: Sets(Strings("09")));

        var id = await new BlockRepository(db).SaveAsync(NewBlock(blockRef: "01"), 3, ct: Ct);

        Assert.Equal(77, id);
        Assert.True(db.Committed);
        var lockCmd = Assert.Single(db.Commands, c => c.Text == "sp_getapplock");
        Assert.Equal("BookBlockRef:S1", lockCmd.Parameters["Resource"]);
        Assert.Equal("01", Assert.Single(db.Commands, c => c.Text == "AddBlock").Parameters["BlockRef"]);
    }

    [Fact]
    public async Task Save_NewUsedBlock_RefTakenOnOtherSubmission_AllocatesNextFreeRef()
    {
        var db = SaveDb(batchAnimal: Sets(Animals((8, "S1"))), otherRefs: Sets(Strings("01")), heldRefs: Sets(Ints(1, 2)));

        await new BlockRepository(db).SaveAsync(NewBlock(blockRef: "01"), 3, ct: Ct);

        Assert.Contains(db.Commands, c => c.Text == "GetBlocksForSenderRef");
        var inserted = (string)Assert.Single(db.Commands, c => c.Text == "AddBlock").Parameters["BlockRef"]!;
        Assert.NotEqual("01", inserted);
        Assert.NotEqual("02", inserted);
    }

    [Fact]
    public async Task Save_NewUsedBlock_SenderOnlyInBlockAnimalResultSet_LocksOnThatSender()
    {
        var empty = Table(("X", typeof(int)));
        var db = SaveDb(
            batchAnimal: Sets(Animals((99, "OTHER"))),
            batchBlocks: Sets(empty, empty, empty, empty, empty, Animals((8, " LEGACY "))),
            otherRefs: Sets(Strings()));

        await new BlockRepository(db).SaveAsync(NewBlock(), 3, ct: Ct);

        Assert.Equal("BookBlockRef:LEGACY", Assert.Single(db.Commands, c => c.Text == "sp_getapplock").Parameters["Resource"]);
    }

    [Fact]
    public async Task Save_NewUsedBlock_SenderUnknown_SkipsLockAndSkipsHeldRefLookup()
    {
        var empty = Table(("X", typeof(int)));
        var db = SaveDb(
            batchAnimal: Sets(Animals((99, "OTHER"))),
            batchBlocks: Sets(empty, empty, empty, empty, empty, Animals((98, "NOPE"))),
            otherRefs: Sets(Strings("01")));

        await new BlockRepository(db).SaveAsync(NewBlock(blockRef: "01"), 3, ct: Ct);

        Assert.DoesNotContain(db.Commands, c => c.Text == "sp_getapplock");
        Assert.DoesNotContain(db.Commands, c => c.Text == "GetBlocksForSenderRef");
        Assert.True(db.Committed);
    }

    [Fact]
    public async Task Save_NewUsedBlock_LockNotGranted_RollsBackAndThrows()
    {
        var db = SaveDb(batchAnimal: Sets(Animals((8, "S1"))), lockResult: -1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => new BlockRepository(db).SaveAsync(NewBlock(), 3, ct: Ct));

        Assert.True(db.RolledBack);
        Assert.False(db.Committed);
        Assert.DoesNotContain(db.Commands, c => c.Text == "AddBlock");
    }

    [Theory]
    [InlineData(true, BlockStatus.Used, 5, false)]     // keep ref used on other submissions
    [InlineData(false, BlockStatus.PreBooked, 5, false)] // not an ordinary block
    [InlineData(false, BlockStatus.Used, 0, false)]    // no batch
    [InlineData(false, BlockStatus.Used, 5, true)]     // repeat block
    public async Task Save_NewBlockNotSubjectToRefGuard_InsertsWithoutTransaction(bool keepRef, int status, int batchId, bool repeat)
    {
        var db = SaveDb();
        var block = new Block { BatchID = batchId, AnimalID = 8, BlockRef = "04", Status = status, RepeatBlock = repeat };

        var id = await new BlockRepository(db).SaveAsync(block, 3, keepRef, Ct);

        Assert.Equal(77, id);
        Assert.False(db.Committed);
        var cmd = Assert.Single(db.Commands);
        Assert.Equal("AddBlock", cmd.Text);
        Assert.Equal("04", cmd.Parameters["BlockRef"]);
    }

    [Fact]
    public async Task Save_ExistingBlock_UpdatesRowWithArchiveColumnsAsDbNullWhenUnset()
    {
        var db = SaveDb();
        var block = NewBlock(id: 12, blockRef: "03");

        var id = await new BlockRepository(db).SaveAsync(block, 4, ct: Ct);

        Assert.Equal(12, id);
        var cmd = Assert.Single(db.Commands);
        Assert.Equal("EditBlock", cmd.Text);
        Assert.Equal(4, cmd.Parameters["UserID"]);
        Assert.Equal(DBNull.Value, cmd.Parameters["ArchiveLocation"]);
        Assert.Equal(DBNull.Value, cmd.Parameters["ArchivedDate"]);
        Assert.Equal(DBNull.Value, cmd.Parameters["ArchiveComment"]);
    }

    [Fact]
    public async Task Save_ExistingBlock_PassesArchiveValuesThrough()
    {
        var db = SaveDb();
        var date = new DateTime(2026, 2, 3);
        var block = new Block { ID = 12, BatchID = 5, AnimalID = 8, BlockRef = "03", ArchiveLocation = "Loc", ArchivedDate = date, ArchiveComment = "note" };

        await new BlockRepository(db).SaveAsync(block, 4, ct: Ct);

        var p = db.Commands[0].Parameters;
        Assert.Equal(("Loc", date, "note"), ((string)p["ArchiveLocation"]!, (DateTime)p["ArchivedDate"]!, (string)p["ArchiveComment"]!));
    }

    [Fact]
    public async Task Delete_CallsDeleteBlockWithIdOnly()
    {
        var db = new FakeDb();

        await new BlockRepository(db).DeleteAsync(12, 4, Ct);

        var cmd = Assert.Single(db.Commands);
        Assert.Equal("DeleteBlock", cmd.Text);
        Assert.Equal(12, cmd.Parameters["ID"]);
        Assert.DoesNotContain("UserID", cmd.Parameters.Keys);
    }

    [Fact]
    public async Task CreatePreBookedBlock_InsertsPreBookedPlaceholder()
    {
        var db = new FakeDb();

        await new BlockRepository(db).CreatePreBookedBlockAsync(8, "06", Ct);

        var cmd = Assert.Single(db.Commands);
        Assert.Equal("AddBlock", cmd.Text);
        Assert.Equal((8, "06", BlockStatus.PreBooked), ((int)cmd.Parameters["AnimalID"]!, (string)cmd.Parameters["BlockRef"]!, (int)cmd.Parameters["Status"]!));
    }

    // ---------------- BookPreBookedBlocks: animal resolution ----------------

    [Fact]
    public async Task BookPreBookedBlocks_NoExistingAnimal_CreatesOneAndBooksRef()
    {
        var db = SaveDb(newId: 31);

        var result = Assert.Single(await new BlockRepository(db).BookPreBookedBlocksAsync("S9", [4], Ct));

        Assert.Equal(PreBookedBlockOutcome.Booked, result.Outcome);
        var animal = Assert.Single(db.Commands, c => c.Text == "AddAnimal");
        Assert.Equal("S9", animal.Parameters["SenderRef"]);
        Assert.Equal(31, Assert.Single(db.Commands, c => c.Text == "AddBlock").Parameters["AnimalID"]);
    }

    [Fact]
    public async Task BookPreBookedBlocks_ExistingAnimalWithIdColumn_ReusesItAndFlagsTakenRefs()
    {
        var animal = Table(("id", typeof(int)));
        animal.Rows.Add(14);
        var db = new FakeDb
        {
            OnQuery = c => c.Text switch
            {
                "GetBlocksForSenderRef" => Sets(Table(("BlockRef", typeof(int)), ("Status", typeof(int))).Also(t => t.Rows.Add(1, 1))),
                "GetAnimalBySender" => Sets(animal),
                _ => null,
            },
            OnExecute = c => Out(c, "RETURN_VALUE", 0),
        };

        var results = await new BlockRepository(db).BookPreBookedBlocksAsync("S9", [1, 2], Ct);

        Assert.Equal([PreBookedBlockOutcome.AlreadyExists, PreBookedBlockOutcome.Booked], results.Select(r => r.Outcome));
        Assert.DoesNotContain(db.Commands, c => c.Text == "AddAnimal");
        Assert.Equal(14, Assert.Single(db.Commands, c => c.Text == "AddBlock").Parameters["AnimalID"]);
    }

    // ---------------- searches ----------------

    [Fact]
    public async Task GetUsedBlockRefsByHistologyRef_PassesHistologyRef()
    {
        var t = Table(("BlockRef", typeof(int)), ("Status", typeof(int)));
        t.Rows.Add(3, 1);
        var db = new FakeDb { OnQuery = _ => Sets(t) };

        var used = Assert.Single(await new BlockRepository(db).GetUsedBlockRefsByHistologyRefAsync("26/10001", Ct));

        Assert.Equal((3, 1), (used.BlockRef, used.Status));
        Assert.Equal("GetBlocksForHistoRef", db.Commands[0].Text);
        Assert.Equal("26/10001", db.Commands[0].Parameters["HistologyRef"]);
    }

    [Fact]
    public async Task GetUsedBlockRefsBySenderRef_PassesSenderRef()
    {
        var t = Table(("BlockRef", typeof(int)), ("Status", typeof(int)));
        t.Rows.Add(4, 2);
        var db = new FakeDb { OnQuery = _ => Sets(t) };

        var used = Assert.Single(await new BlockRepository(db).GetUsedBlockRefsBySenderRefAsync("S1", Ct));

        Assert.Equal(4, used.BlockRef);
        Assert.Equal("S1", db.Commands[0].Parameters["SenderRef"]);
    }

    [Fact]
    public async Task GetBlockArchive_PassesFiltersAndMapsRows()
    {
        var t = Table(("ID", typeof(int)), ("BlockID", typeof(int)), ("BlockRef", typeof(string)), ("ArchiveLocation", typeof(string)),
            ("ArchivedDate", typeof(DateTime)), ("TissueDescription", typeof(string)), ("NoPieces", typeof(short)), ("ArchiveComment", typeof(string)));
        t.Rows.Add(1, 2, "03", "Shelf", new DateTime(2026, 1, 1), "Brain", (short)4, "ok");
        var db = new FakeDb { OnQuery = _ => Sets(t) };

        var info = Assert.Single(await new BlockRepository(db).GetBlockArchiveAsync("S", "H", "B", "L", Ct));

        Assert.Equal((1, 2, "03", "Shelf", "Brain", (short?)4, "ok"),
            (info.ID, info.BlockID, info.BlockRef, info.ArchiveLocation, info.TissueDescription, info.NoPieces, info.ArchiveComment));
        Assert.Equal(new DateTime(2026, 1, 1), info.ArchivedDate);
        var p = db.Commands[0].Parameters;
        Assert.Equal(("S", "H", "B", "L"), ((string)p["SenderRef"]!, (string)p["HistologyRef"]!, (string)p["BlockRef"]!, (string)p["ArchiveLocation"]!));
    }

    [Fact]
    public async Task GetSlideArchive_MergesStainAntibodyAndHistologyRows_FiltersCoveredDescriptions_AndOrdersByBlockRef()
    {
        static DataTable Slides(params (string BlockRef, string Description)[] rows)
        {
            var t = Table(("BatchID", typeof(int)), ("BlockRef", typeof(string)), ("ArchiveLocation", typeof(string)),
                ("ArchivedDate", typeof(DateTime)), ("Description", typeof(string)), ("TissueDescription", typeof(string)));
            foreach (var (b, d) in rows) t.Rows.Add(1, b, "Loc", new DateTime(2026, 1, 1), d, "T");
            return t;
        }

        var batches = Table(("BatchID", typeof(int)), ("SubmissionType", typeof(int)));
        batches.Rows.Add(10, 2);
        batches.Rows.Add(11, 3);
        var db = new FakeDb
        {
            OnQuery = c => c.Text switch
            {
                "GetAnimalStainArchiveInformation" => Sets(Slides(("03", "Stain A"))),
                "GetAnimalBatches" => Sets(batches),
                "GetAnimalAntibodiesArchiveInformation" => Sets(Slides(("01", "Ab " + c.Parameters["BatchID"]))),
                "GetAnimalHistologyArchiveInformation" => Sets(Slides(("02", "H&E"), ("04", "Special Stain"), ("05", "IHC - PrP"), ("06", "IHC - Other"))),
                _ => null,
            },
        };

        var rows = await new BlockRepository(db).GetSlideArchiveAsync("S", "H", "L", Ct);

        Assert.Equal(["01", "01", "02", "03"], rows.Select(r => r.BlockRef));
        Assert.Equal(2, db.Commands.Count(c => c.Text == "GetAnimalAntibodiesArchiveInformation"));
        var batch11 = db.Commands.Last(c => c.Text == "GetAnimalAntibodiesArchiveInformation");
        Assert.Equal((11, 3), ((int)batch11.Parameters["BatchID"]!, (int)batch11.Parameters["SubmissionType"]!));
    }

    [Fact]
    public async Task GetSlideArchive_NoBatches_SkipsAntibodyLookup()
    {
        var db = new FakeDb();

        Assert.Empty(await new BlockRepository(db).GetSlideArchiveAsync(null, null, null, Ct));
        Assert.DoesNotContain(db.Commands, c => c.Text == "GetAnimalAntibodiesArchiveInformation");
    }
}
