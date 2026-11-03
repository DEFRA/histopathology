using System.Data;
using Histo.Histology.Models;
using Histo.Histology.Repositories;
using Histo.Tests.Support;

namespace Histo.Tests.Unit;

/// <summary><see cref="BlockTestRepository"/> reads, updates and code saves against <see cref="FakeDb"/>.</summary>
public class BlockTestRepositoryCoverageTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static DataTable Table(params (string Column, Type Type)[] columns)
    {
        var table = new DataTable();
        foreach (var (column, type) in columns) table.Columns.Add(column, type);
        return table;
    }

    private static DataTable TestRows(Action<DataTable>? fill = null)
    {
        var t = Table(("ID", typeof(int)), ("BlockID", typeof(int)), ("Code", typeof(string)), ("Result", typeof(string)),
            ("QCCode", typeof(string)), ("QCNote", typeof(long)), ("QCNoteRef", typeof(int)), ("StainRef", typeof(string)),
            ("Dispatched", typeof(bool)), ("DispatchedDate", typeof(DateTime)), ("DispatchedBy", typeof(string)),
            ("EnteredBy", typeof(int)), ("PremiumCharge", typeof(string)), ("DispatchedTo", typeof(string)),
            ("Comment", typeof(string)), ("RemedialAction", typeof(string)), ("ArchiveLocation", typeof(string)),
            ("ArchivedDate", typeof(DateTime)), ("ArchiveComment", typeof(string)), ("NumberOfSlides", typeof(int)),
            ("RowStamp", typeof(byte[])));
        fill?.Invoke(t);
        return t;
    }

    private static DataTable TcRows(params (int TestId, int Id, string Code)[] rows)
    {
        var t = Table(("TestID", typeof(int)), ("ID", typeof(int)), ("Code", typeof(string)));
        foreach (var (testId, id, code) in rows) t.Rows.Add(testId, id, code);
        return t;
    }

    private static DataSet FullBatch()
    {
        var details = Table(("ID", typeof(int)), ("BlockRef", typeof(string)), ("AnimalID", typeof(int)), ("Status", typeof(int)));
        details.Rows.Add(1, "01", 2, 1);
        details.Rows.Add(2, "02", 2, 2); // pre-booked => on hold

        var histology = TestRows(t =>
        {
            // Fully populated row: archived, dispatched, non-bool QCNote flag.
            t.Rows.Add(10, 1, "2", "Pos", "Q1", 1L, 5, "SR", true, new DateTime(2026, 1, 2), "Bob",
                9, "PC", "Lab", "cm", "ra", "Shelf", new DateTime(2026, 1, 3), "ac", 4, new byte[] { 1, 2 });
            // Gating code (3) must be hidden from the worklist read; everything nullable is NULL.
            t.Rows.Add(11, 1, "3", DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value,
                DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value,
                DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);
            // Row for the on-hold block, archive location without a date => not archived.
            t.Rows.Add(12, 2, "1", DBNull.Value, DBNull.Value, 0L, DBNull.Value, DBNull.Value,
                false, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value,
                DBNull.Value, DBNull.Value, "Shelf", DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);
        });
        var antibodies = TestRows(t => t.Rows.Add(20, 1, "A1", "Neg", DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value,
            DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value,
            DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value));
        var stains = TestRows(t => t.Rows.Add(30, 1, "S1", DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, "SR2",
            DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value,
            DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value));

        var animals = Table(("ID", typeof(int)), ("HistologyRef", typeof(string)));
        animals.Rows.Add(2, "26/10001");

        var ds = new DataSet();
        ds.Tables.Add(details);
        ds.Tables.Add(Table(("ID", typeof(int))));
        ds.Tables.Add(histology);
        ds.Tables.Add(antibodies);
        ds.Tables.Add(stains);
        ds.Tables.Add(animals);
        ds.Tables.Add(Table(("ID", typeof(int))));
        ds.Tables.Add(TcRows((30, 301, "TC-S")));
        ds.Tables.Add(TcRows((20, 201, "TC-A1"), (20, 202, "TC-A2")));
        ds.Tables.Add(TcRows((10, 101, "TC-H")));
        return ds;
    }

    private static FakeDb BatchDb() => new() { OnQuery = c => c.Text == "GetBatchBlocksByID" ? FullBatch() : null };

    // ---------------- reads ----------------

    [Fact]
    public async Task GetByBatch_MapsAllTestTypes_WorklistFiltersGatingHistologyCodes()
    {
        var db = BatchDb();

        var tests = await new BlockTestRepository(db).GetByBatchAsync(5, Ct);

        Assert.Equal(5, db.Commands[0].Parameters["ID"]);
        Assert.Equal([10, 12, 20, 30], tests.Select(t => t.ID));

        var h = tests[0];
        Assert.Equal((BlockTestType.Histology, "2", "Pos", "Q1", true, 5, "SR"), (h.TestType, h.Code, h.Result, h.QCCode, h.QCNote, h.QCNoteRef, h.StainRef));
        Assert.Equal((true, new DateTime(2026, 1, 2), "Bob", 9, "PC", "Lab"), (h.Dispatched, h.DispatchedDate, h.DispatchedBy, h.EnteredBy, h.PremiumCharge, h.DispatchedTo));
        Assert.Equal(("cm", "ra", "Shelf", new DateTime(2026, 1, 3), "ac", 4), (h.Comment, h.RemedialAction, h.ArchiveLocation, h.ArchivedDate, h.ArchiveComment, h.NumberOfSlides));
        Assert.True(h.Archived);
        Assert.False(h.OnHold);
        Assert.Equal("01", h.BlockRef);
        Assert.Equal([1, 2], h.RowStamp);
        Assert.Equal([new TcCode(101, "TC-H")], h.TCCodes);

        var onHold = tests[1];
        Assert.True(onHold.OnHold);
        Assert.False(onHold.Archived);
        Assert.False(onHold.QCNote);
        Assert.False(onHold.Dispatched);
        Assert.Null(onHold.RowStamp);
        Assert.Empty(onHold.TCCodes);

        Assert.Equal((BlockTestType.Antibodies, "A1", "Neg"), (tests[2].TestType, tests[2].Code, tests[2].Result));
        Assert.Equal(["TC-A1", "TC-A2"], tests[2].TCCodes.Select(t => t.Code));
        Assert.Equal((BlockTestType.Stain, "SR2"), (tests[3].TestType, tests[3].StainRef));
        Assert.Equal("TC-S", Assert.Single(tests[3].TCCodes).Code);
    }

    [Fact]
    public async Task GetByBatch_ResolvesHistologyRefFromBlockAnimal()
    {
        var tests = await new BlockTestRepository(BatchDb()).GetByBatchAsync(5, Ct);

        Assert.All(tests, t => Assert.Equal("26/10001", t.HistologyRef));
    }

    [Fact]
    public async Task GetAllSelectionsByBatch_IncludesGatingHistologyCodes()
    {
        var tests = await new BlockTestRepository(BatchDb()).GetAllSelectionsByBatchAsync(5, Ct);

        Assert.Equal([10, 11, 12, 20, 30], tests.Select(t => t.ID));
        Assert.Null(tests[1].Result);
        Assert.Null(tests[1].QCNoteRef);
        Assert.Null(tests[1].DispatchedDate);
    }

    // ---------------- Update ----------------

    [Theory]
    [InlineData(BlockTestType.Histology, "EditBlockHistology")]
    [InlineData(BlockTestType.Antibodies, "EditBlockAntibodies")]
    [InlineData(BlockTestType.Stain, "EditBlockStain")]
    public async Task Update_DispatchesToProcForTestType_AndStampsSessionUser(string testType, string expectedProc)
    {
        var db = new FakeDb { OnExecute = c => c.Command.Parameters["RETURN_VALUE"].Value = 0 };
        var test = new BlockTest { ID = 4, BlockID = 2, TestType = testType, Code = "X", Result = "R", RowStamp = [7] };

        await new BlockTestRepository(db).UpdateAsync(test, 9, Ct);

        var cmd = Assert.Single(db.Commands);
        Assert.Equal(expectedProc, cmd.Text);
        Assert.Equal((4, 2, "X", "R", 9), ((int)cmd.Parameters["ID"]!, (int)cmd.Parameters["BlockID"]!, (string)cmd.Parameters["Code"]!, (string)cmd.Parameters["Result"]!, (int)cmd.Parameters["UserID"]!));
        Assert.Equal("9", cmd.Parameters["EnteredBy"]);
    }

    [Fact]
    public async Task Update_ReturnValueOne_ThrowsConcurrencyException()
    {
        var db = new FakeDb { OnExecute = c => c.Command.Parameters["RETURN_VALUE"].Value = 1 };
        var test = new BlockTest { ID = 4, TestType = BlockTestType.Stain };

        await Assert.ThrowsAsync<BlockTestConcurrencyException>(() => new BlockTestRepository(db).UpdateAsync(test, 9, Ct));
    }

    [Fact]
    public async Task Update_UnknownTestType_ThrowsWithoutCallingDatabase()
    {
        var db = new FakeDb();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => new BlockTestRepository(db).UpdateAsync(new BlockTest { TestType = "Bogus" }, 9, Ct));

        Assert.Empty(db.Commands);
    }

    // ---------------- SaveTCCodes ----------------

    [Theory]
    [InlineData(BlockTestType.Histology, "AddHistologyTCCode", "DeleteHistologyTCCode")]
    [InlineData(BlockTestType.Antibodies, "AddAntibodiesTCCode", "DeleteAntibodiesTCCode")]
    [InlineData(BlockTestType.Stain, "AddSpecialStainTCCode", "DeleteSpecialStainTCCode")]
    public async Task SaveTCCodes_DeletesDeselectedAndInsertsNewCodes(string testType, string addSp, string deleteSp)
    {
        var db = new FakeDb();
        IReadOnlyList<TcCode> existing = [new TcCode(1, "KEEP"), new TcCode(2, "DROP")];

        await new BlockTestRepository(db).SaveTCCodesAsync(5, 6, testType, existing, ["KEEP", "NEW"], 9, Ct);

        Assert.Equal([deleteSp, addSp], db.Commands.Select(c => c.Text));
        Assert.Equal(2, db.Commands[0].Parameters["ID"]);
        var add = db.Commands[1].Parameters;
        Assert.Equal((6, "NEW", 9, 5), ((int)add["TestID"]!, (string)add["Code"]!, (int)add["UserID"]!, (int)add["BatchID"]!));
    }

    [Fact]
    public async Task SaveTCCodes_NoChange_IssuesNoCommands()
    {
        var db = new FakeDb();

        await new BlockTestRepository(db).SaveTCCodesAsync(5, 6, BlockTestType.Stain, [new TcCode(1, "A")], ["A"], 9, Ct);

        Assert.Empty(db.Commands);
    }

    [Fact]
    public async Task SaveTCCodes_UnknownTestType_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => new BlockTestRepository(new FakeDb()).SaveTCCodesAsync(5, 6, "Bogus", [], [], 9, Ct));
    }

    // ---------------- AddTestsToBatchBlocks ----------------

    [Fact]
    public async Task AddTestsToBatchBlocks_NoCodes_DoesNothing()
    {
        var db = new FakeDb();

        await new BlockTestRepository(db).AddTestsToBatchBlocksAsync(5, [], [], [], 9, Ct);

        Assert.Empty(db.Commands);
        Assert.False(db.Committed);
    }

    private static DataSet BatchBlockTests()
    {
        DataTable Pairs(params (int BlockId, string Code)[] rows)
        {
            var t = Table(("BlockID", typeof(int)), ("Code", typeof(string)));
            foreach (var (b, c) in rows) t.Rows.Add(b, c);
            return t;
        }

        var ids = Table(("ID", typeof(int)));
        ids.Rows.Add(1);
        ids.Rows.Add(2);
        var ds = new DataSet();
        ds.Tables.Add(ids);
        ds.Tables.Add(Pairs((1, "4"), (1, "3")));
        ds.Tables.Add(Pairs());
        ds.Tables.Add(Pairs());
        return ds;
    }

    [Fact]
    public async Task AddTestsToBatchBlocks_InsertsMissingCodesPerBlock_AndCommits()
    {
        var db = new FakeDb { OnQuery = c => c.Text == "GetBatchBlockTests" ? BatchBlockTests() : null };

        await new BlockTestRepository(db).AddTestsToBatchBlocksAsync(5, ["2"], ["A1"], ["S1"], 9, Ct);

        Assert.True(db.Committed);
        var adds = db.Commands.Where(c => c.Text.StartsWith("AddBlock", StringComparison.Ordinal))
            .Select(c => (c.Text, Block: (int)c.Parameters["BlockID"]!, Code: (string)c.Parameters["Code"]!)).ToList();
        // Block 1 holds IHC-PrP and Special Stain, so antibodies and stains are propagated; block 2 holds neither.
        Assert.Equal(
            [("AddBlockHistology", 1, "2"), ("AddBlockAntibodies", 1, "A1"), ("AddBlockStain", 1, "S1"), ("AddBlockHistology", 2, "2")],
            adds);
        Assert.All(db.Commands.Where(c => c.Text.StartsWith("AddBlock", StringComparison.Ordinal)),
            c => Assert.Equal((9, 5), ((int)c.Parameters["UserID"]!, (int)c.Parameters["BatchID"]!)));
    }

    [Fact]
    public async Task AddTestsToBatchBlocks_InsertFails_RollsBackAndRethrows()
    {
        var db = new FakeDb
        {
            OnQuery = c => c.Text == "GetBatchBlockTests" ? BatchBlockTests() : null,
            OnExecute = c => { if (c.Text == "AddBlockAntibodies") throw new InvalidOperationException("boom"); },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new BlockTestRepository(db).AddTestsToBatchBlocksAsync(5, ["2"], ["A1"], [], 9, Ct));

        Assert.True(db.RolledBack);
        Assert.False(db.Committed);
    }
}
