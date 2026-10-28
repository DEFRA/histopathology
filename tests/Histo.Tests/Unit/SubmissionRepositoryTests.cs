using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;
using Histo.Submissions.Models;
using Histo.Submissions.Repositories;
using Histo.Tests.Support;
using Microsoft.Data.SqlClient;

namespace Histo.Tests.Unit;

/// <summary>SubmissionRepository CRUD, transactional copy and mouse-range creation against <see cref="FakeDb"/>.</summary>
public class SubmissionRepositoryTests
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

    private static void Out(FakeDb.ExecutedCommand c, string name, object value)
    {
        if (c.Command.Parameters.Contains(name)) c.Command.Parameters[name].Value = value;
    }

    private static SqlException NewSqlException(int number)
    {
        var errorCtor = typeof(SqlError).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length).First();
        var error = (SqlError)errorCtor.Invoke(errorCtor.GetParameters().Select((p, i) => FillArg(p.ParameterType, i == 0 ? number : 0)).ToArray());

        var errors = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
        typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(errors, [error]);

        var create = typeof(SqlException).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name == "CreateException" && m.GetParameters()[0].ParameterType == typeof(SqlErrorCollection))
            .OrderBy(m => m.GetParameters().Length).First();
        return (SqlException)create.Invoke(null, create.GetParameters().Select((p, i) => i == 0 ? errors : FillArg(p.ParameterType, 0)).ToArray())!;
    }

    private static object? FillArg(Type type, int number) =>
        type == typeof(int) ? number
        : type == typeof(string) ? "x"
        : type.IsValueType ? RuntimeHelpers.GetUninitializedObject(type)
        : null;

    private static DataTable Submissions(params (int Id, int AnimalId, int Order)[] rows)
    {
        var t = Table(("ID", typeof(int)), ("BatchID", typeof(int)), ("AnimalID", typeof(int)), ("SubmissionName", typeof(string)), ("Order", typeof(int)));
        foreach (var (id, animalId, order) in rows) t.Rows.Add(id, 5, animalId, "Default", order);
        return t;
    }

    private static DataTable TissueRows(int submissionId)
    {
        var t = Table(("ID", typeof(int)), ("BatchSubmissionID", typeof(int)), ("TissueCode", typeof(string)), ("NoPieces", typeof(int)),
            ("Comment", typeof(string)), ("ArchiveLocation", typeof(string)), ("ArchivedDate", typeof(string)),
            ("ArchiveComment", typeof(string)), ("RowStamp", typeof(byte[])));
        t.Rows.Add(1, submissionId, "BRAIN ", 2, "left", "Shelf", "2026-01-02", "note", new byte[] { 9 });
        t.Rows.Add(2, submissionId + 1, "LIVER", 1, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);
        return t;
    }

    private static Tissue Tissue(TissueOwner owner = TissueOwner.Submission, string code = "BRAIN") =>
        new() { ID = 4, OwnerID = 11, Owner = owner, TissueCode = code, NoPieces = 3, Comment = "c", ArchiveLocation = "Shelf", ArchivedDate = new DateTime(2026, 1, 2), ArchiveComment = "ac", RowStamp = [1] };

    // ---------------- reads ----------------

    [Fact]
    public async Task GetSubmissionsByBatch_ReadsFirstResultSet()
    {
        var db = new FakeDb { OnQuery = _ => Sets(Submissions((11, 2, 1), (12, 3, 2))) };

        var rows = await new SubmissionRepository(db).GetSubmissionsByBatchAsync(5, Ct);

        Assert.Equal([11, 12], rows.Select(r => r.ID));
        Assert.Equal(5, db.Commands[0].Parameters["ID"]);
    }

    [Fact]
    public async Task AddSubmission_ReturnsNewIdFromOutputParameter()
    {
        var db = new FakeDb { OnExecute = c => Out(c, "NewID", 31) };

        var id = await new SubmissionRepository(db).AddSubmissionAsync(new BatchSubmission { BatchID = 5, AnimalID = 2, Order = 4 }, 7, Ct);

        Assert.Equal(31, id);
        var cmd = Assert.Single(db.Commands, c => c.Text == "AddBatchSubmission");
        Assert.Equal((5, 2, 4), (cmd.Parameters["BatchID"], cmd.Parameters["AnimalID"], cmd.Parameters["Order"]));
    }

    [Fact]
    public async Task GetAnimalsByBatch_CollapsesDuplicatesAndComputesSetFlags()
    {
        var t = Table(("ID", typeof(int)), ("SenderRef", typeof(string)), ("HistologyRef", typeof(string)), ("PMDate", typeof(string)));
        t.Rows.Add(1, "S1", "26/20001", "01/02/2026");
        t.Rows.Add(1, "S1", "26/20001", "01/02/2026");
        t.Rows.Add(2, "S2", DBNull.Value, "  ");
        var db = new FakeDb { OnQuery = _ => Sets(t) };

        var animals = await new SubmissionRepository(db).GetAnimalsByBatchAsync(5, Ct);

        Assert.Equal(2, animals.Count);
        Assert.Equal((true, true), (animals[0].HistoRefSet, animals[0].PMDateSet));
        Assert.Equal((false, false), (animals[1].HistoRefSet, animals[1].PMDateSet));
    }

    [Fact]
    public async Task GetBlockAnimalsByBatch_ReadsResultSetFive_OnePerAnimal()
    {
        var animals = Table(("ID", typeof(int)), ("SenderRef", typeof(string)), ("HistologyRef", typeof(string)));
        animals.Rows.Add(1, "S1", "26/20001");
        animals.Rows.Add(1, "S1", "26/20001");
        var filler = () => Table(("ID", typeof(int)));
        var db = new FakeDb { OnQuery = _ => Sets(filler(), filler(), filler(), filler(), filler(), animals) };

        var result = await new SubmissionRepository(db).GetBlockAnimalsByBatchAsync(5, Ct);

        var animal = Assert.Single(result);
        Assert.True(animal.HistoRefSet);
        Assert.False(animal.PMDateSet);
    }

    [Fact]
    public async Task GetTissuesBySubmission_FiltersToTheSubmission_AndMapsColumns()
    {
        var db = new FakeDb { OnQuery = _ => Sets(TissueRows(11)) };

        var tissues = await new SubmissionRepository(db).GetTissuesBySubmissionAsync(5, 11, Ct);

        var tissue = Assert.Single(tissues);
        Assert.Equal(("BRAIN", (short)2, "left", "Shelf", "note", new DateTime(2026, 1, 2), TissueOwner.Submission, 11),
            (tissue.TissueCode, tissue.NoPieces, tissue.Comment, tissue.ArchiveLocation, tissue.ArchiveComment, tissue.ArchivedDate, tissue.Owner, tissue.OwnerID));
        Assert.Equal(new byte[] { 9 }, tissue.RowStamp);
    }

    [Fact]
    public async Task GetBatchSubmissionTissues_ReadsSecondResultSet_AndAcceptsEitherForeignKeyName()
    {
        var withBsid = Table(("ID", typeof(int)), ("BatchSubmissionID", typeof(int)), ("TissueCode", typeof(string)), ("NoPieces", typeof(int)),
            ("ArchivedDate", typeof(DateTime)), ("RowStamp", typeof(byte[])));
        withBsid.Rows.Add(1, 11, "BRAIN ", 2, new DateTime(2026, 3, 4), new byte[] { 1 });
        withBsid.Rows.Add(2, 12, "LIVER", 1, DBNull.Value, DBNull.Value);
        var withSubmissionId = Table(("ID", typeof(int)), ("SubmissionID", typeof(int)), ("TissueCode", typeof(string)));
        withSubmissionId.Rows.Add(3, 13, "SPLEEN");

        var db1 = new FakeDb { OnQuery = _ => Sets(Submissions(), withBsid) };
        var db2 = new FakeDb { OnQuery = _ => Sets(Submissions(), withSubmissionId) };

        var first = await new SubmissionRepository(db1).GetBatchSubmissionTissuesAsync(5, Ct);
        var second = await new SubmissionRepository(db2).GetBatchSubmissionTissuesAsync(5, Ct);

        Assert.Equal([11, 12], first.Select(t => t.OwnerID));
        Assert.Equal("BRAIN", first[0].TissueCode);
        Assert.Equal(new DateTime(2026, 3, 4), first[0].ArchivedDate);
        Assert.Null(first[1].RowStamp);
        Assert.Equal(13, Assert.Single(second).OwnerID);
        Assert.Equal((short)0, second[0].NoPieces);
    }

    [Fact]
    public async Task GetTissuesByBatch_MapsBlockTissues_AndByBlockFilters()
    {
        var t = Table(("ID", typeof(int)), ("BlockID", typeof(int)), ("TissueCode", typeof(string)), ("NoPieces", typeof(int)),
            ("Comment", typeof(string)), ("RowStamp", typeof(byte[])));
        t.Rows.Add(1, 7, "BRAIN ", 2, "c", new byte[] { 1 });
        t.Rows.Add(2, 8, "LIVER", 1, DBNull.Value, DBNull.Value);
        var db = new FakeDb { OnQuery = _ => Sets(t.Copy()) };
        var repo = new SubmissionRepository(db);

        var all = await repo.GetTissuesByBatchAsync(5, Ct);
        var forBlock = await repo.GetTissuesByBlockAsync(5, 8, Ct);

        Assert.Equal(["BRAIN", "LIVER"], all.Select(x => x.TissueCode));
        Assert.All(all, x => Assert.Equal(TissueOwner.Block, x.Owner));
        Assert.Equal("c", all[0].Comment);
        Assert.Equal(2, Assert.Single(forBlock).ID);
    }

    [Fact]
    public async Task SearchReaders_PassCriteriaAndReturnTypedRows()
    {
        var one = Table(("ID", typeof(int)), ("SenderRef", typeof(string)));
        one.Rows.Add(1, "S1");
        var db = new FakeDb { OnQuery = _ => Sets(one.Copy()) };
        var repo = new SubmissionRepository(db);

        var pm = await repo.GetByPmDateRangeAsync(new DateTime(2026, 1, 1), null, Ct);
        var bySender = await repo.GetAnimalsBySenderRefAsync("S1", Ct);
        var archive = await repo.GetTissueArchiveAsync("S1", "26/20001", "Shelf", "BRAIN", Ct);
        var tissues = await repo.GetAnimalTissuesAsync("S1", null, "BRAIN", null, Ct);
        var blockTissues = await repo.GetAnimalBlockTissuesAsync("S1", null, "BRAIN", "Proj", Ct);

        Assert.Equal(1, Assert.Single(pm).ID);
        Assert.Equal("S1", Assert.Single(bySender).SenderRef);
        Assert.Single(archive);
        Assert.Equal("S1", Assert.Single(tissues).SenderRef);
        Assert.Single(blockTissues);
        Assert.Equal(
            ["GetSearchPMDates", "GetAnimalsBySenderRef", "GetAnimalTissuesArchiveInformation", "GetAnimalBatchTissues", "GetAnimalBlockTissues"],
            db.Commands.Select(c => c.Text));
        Assert.Equal("Proj", db.Commands[4].Parameters["ProjectDesc"]);
    }

    [Fact]
    public async Task GetAnimalBySender_AcceptsHistologyRefOrHistoRefColumn_CaseInsensitively()
    {
        var a = Table(("id", typeof(int)), ("senderref", typeof(string)), ("HistologyRef", typeof(string)));
        a.Rows.Add(1, "S1", "26/20001");
        var b = Table(("ID", typeof(int)), ("SenderRef", typeof(string)), ("HistoRef", typeof(string)));
        b.Rows.Add(2, "S2", "26/20002");
        var c = Table(("ID", typeof(int)));
        c.Rows.Add(3);

        var first = Assert.Single(await new SubmissionRepository(new FakeDb { OnQuery = _ => Sets(a) }).GetAnimalBySenderAsync("S1", Ct));
        var second = Assert.Single(await new SubmissionRepository(new FakeDb { OnQuery = _ => Sets(b) }).GetAnimalBySenderAsync("S2", Ct));
        var third = Assert.Single(await new SubmissionRepository(new FakeDb { OnQuery = _ => Sets(c) }).GetAnimalBySenderAsync("S3", Ct));

        Assert.Equal((1, "S1", "26/20001"), (first.ID, first.SenderRef, first.HistologyRef));
        Assert.Equal((2, "S2", "26/20002"), (second.ID, second.SenderRef, second.HistologyRef));
        Assert.Equal((3, null, null), (third.ID, third.SenderRef, third.HistologyRef));
    }

    [Theory]
    [InlineData(null, 0, null)]
    [InlineData("", 0, null)]
    [InlineData("1", 1, "Get2001EXTSUB")]
    [InlineData("15", 1, "GetICCSUBMI1TISSUEONLYTO12THJAN2001")]
    [InlineData("All", 1, "GetAllImportedData")]
    public async Task GetImportedData_ChoosesTheProcForTheSelectedTable(string? selected, int expectedCommands, string? expectedProc)
    {
        var rows = Table(("ID", typeof(int)));
        rows.Rows.Add(1);
        var db = new FakeDb { OnQuery = _ => Sets(rows) };

        var result = await new SubmissionRepository(db).GetImportedDataAsync(selected, Ct);

        Assert.Equal(expectedCommands, db.Commands.Count);
        Assert.Equal(expectedCommands, result.Count);
        if (expectedProc is not null) Assert.Equal(expectedProc, db.Commands[0].Text);
    }

    [Fact]
    public async Task IsHistologyRefInUse_ReflectsTheCount()
    {
        var db = new FakeDb { ScalarResult = 2 };
        var repo = new SubmissionRepository(db);

        Assert.True(await repo.IsHistologyRefInUseAsync("26/20001", Ct));
        db.ScalarResult = 0;
        Assert.False(await repo.IsHistologyRefInUseAsync("26/20001", Ct));
    }

    // ---------------- animals / tissues writes ----------------

    [Fact]
    public async Task AddAnimal_ReturnsNewId_AndPassesPmDateAsText()
    {
        var db = new FakeDb { OnExecute = c => Out(c, "NewID", 21) };

        var id = await new SubmissionRepository(db).AddAnimalAsync(new Animal { SenderRef = "S1", PMDate = "01/02/2026", NextBlockRef = "03", OnHold = true }, 7, Ct);

        Assert.Equal(21, id);
        var cmd = Assert.Single(db.Commands, c => c.Text == "AddAnimal");
        Assert.Equal(("S1", "01/02/2026", "03", true), (cmd.Parameters["SenderRef"], cmd.Parameters["PMDate"], cmd.Parameters["NextBlockRef"], cmd.Parameters["OnHold"]));
    }

    [Fact]
    public async Task AddAnimal_OutputNotSet_ReturnsZero()
    {
        var db = new FakeDb();

        Assert.Equal(0, await new SubmissionRepository(db).AddAnimalAsync(new Animal { SenderRef = "S1" }, 7, Ct));
    }

    [Fact]
    public async Task UpdateAnimal_Success_ParsesPmDateToDateTime()
    {
        var db = new FakeDb { OnExecute = c => Out(c, "RETURN_VALUE", 0) };

        await new SubmissionRepository(db).UpdateAnimalAsync(new Animal { ID = 3, SenderRef = "S1", PMDate = "01/02/2026", HistologyRef = "26/20001" }, 7, Ct);

        var cmd = Assert.Single(db.Commands, c => c.Text == "EditAnimal");
        Assert.Equal(new DateTime(2026, 2, 1), cmd.Parameters["PMDate"]);
        Assert.Equal("26/20001", cmd.Parameters["HistologyRef"]);
    }

    [Fact]
    public async Task UpdateAnimal_NoPmDateOrHistologyRef_SendsDbNull()
    {
        var db = new FakeDb { OnExecute = c => Out(c, "RETURN_VALUE", 0) };

        await new SubmissionRepository(db).UpdateAnimalAsync(new Animal { ID = 3, SenderRef = "S1" }, 7, Ct);

        var cmd = Assert.Single(db.Commands, c => c.Text == "EditAnimal");
        Assert.Equal(DBNull.Value, cmd.Parameters["PMDate"]);
        Assert.Equal(DBNull.Value, cmd.Parameters["HistologyRef"]);
    }

    [Fact]
    public async Task UpdateAnimal_ReturnOne_ThrowsConcurrencyException()
    {
        var db = new FakeDb { OnExecute = c => Out(c, "RETURN_VALUE", 1) };

        await Assert.ThrowsAsync<AnimalConcurrencyException>(() => new SubmissionRepository(db).UpdateAnimalAsync(new Animal { ID = 3 }, 7, Ct));
    }

    [Fact]
    public async Task UpdateAnimal_ReturnTwo_ThrowsRefUpdateExceptionNamingTheRef()
    {
        var db = new FakeDb { OnExecute = c => Out(c, "RETURN_VALUE", 2) };

        var ex = await Assert.ThrowsAsync<AnimalRefUpdateException>(() =>
            new SubmissionRepository(db).UpdateAnimalAsync(new Animal { ID = 3, HistologyRef = "26/20001" }, 7, Ct));

        Assert.Contains("26/20001", ex.Message);
    }

    [Fact]
    public async Task DeleteAndAdvanceAnimalCommands_CallTheirProcs()
    {
        var db = new FakeDb();
        var repo = new SubmissionRepository(db);

        await repo.DeleteAnimalAsync(3, 7, Ct);
        await repo.AdvanceNextBlockRefAsync(3, "04", Ct);
        await repo.UpdateSubmissionAsync(new BatchSubmission { ID = 11, BatchID = 5, SubmissionName = "Name", Order = 2 }, 7, Ct);

        Assert.Equal(["DeleteAnimal", "AdvanceAnimalNextBlockRef", "EditBatchSubmission"], db.Commands.Select(c => c.Text));
        Assert.Equal(3, db.Commands[0].Parameters["ID"]);
        Assert.Equal("04", db.Commands[1].Parameters["NextBlockRef"]);
        Assert.Equal("Name", db.Commands[2].Parameters["SubmissionName"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteSample_RunsOneTransactionalProc_WithIncludeSubmissionFlag(bool includeSubmission)
    {
        var db = new FakeDb();
        var repo = new SubmissionRepository(db);

        if (includeSubmission) await repo.DeleteSampleSubmissionFromBatchAsync(5, 3, 7, Ct);
        else await repo.DeleteSampleFromBatchAsync(5, 3, 7, Ct);

        var cmd = Assert.Single(db.Commands);
        Assert.Equal("DeleteSampleFromBatch", cmd.Text);
        Assert.Equal(includeSubmission, cmd.Parameters["IncludeSubmission"]);
        Assert.True(db.Committed);
    }

    [Fact]
    public async Task DeleteSample_ProcFails_RollsBackAndRethrows()
    {
        var db = new FakeDb { OnExecute = _ => throw new InvalidOperationException("boom") };

        await Assert.ThrowsAsync<InvalidOperationException>(() => new SubmissionRepository(db).DeleteSampleFromBatchAsync(5, 3, 7, Ct));

        Assert.True(db.RolledBack);
        Assert.False(db.Committed);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(1, "not found")]
    [InlineData(3, "already been used")]
    public async Task UpdateAnimalSenderRef_ReturnCodeDecidesOutcome(int returnValue, string? expectedMessage)
    {
        var db = new FakeDb { OnExecute = c => Out(c, "RETURN_VALUE", returnValue) };
        var act = () => new SubmissionRepository(db).UpdateAnimalSenderRefAsync("S1", "S2", 7, Ct);

        if (expectedMessage is null) await act();
        else Assert.Contains(expectedMessage, (await Assert.ThrowsAsync<AnimalRefUpdateException>(act)).Message);

        Assert.Equal("S2", db.Commands[0].Parameters["NewSenderRef"]);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(1, "not found")]
    [InlineData(3, "already been used")]
    public async Task UpdateAnimalHistologyRef_ReturnCodeDecidesOutcome(int returnValue, string? expectedMessage)
    {
        var db = new FakeDb { OnExecute = c => Out(c, "RETURN_VALUE", returnValue) };
        var act = () => new SubmissionRepository(db).UpdateAnimalHistologyRefAsync("S1", null, 7, Ct);

        if (expectedMessage is null) await act();
        else Assert.Contains(expectedMessage, (await Assert.ThrowsAsync<AnimalRefUpdateException>(act)).Message);

        Assert.Equal(DBNull.Value, db.Commands[0].Parameters["NewHistologyRef"]);
    }

    [Theory]
    [InlineData(TissueOwner.Submission, "AddTissue", "BatchSubmissionID")]
    [InlineData(TissueOwner.Block, "AddBlockTissue", "BlockID")]
    public async Task AddTissue_UsesTheProcAndKeyForTheOwnerType(TissueOwner owner, string proc, string key)
    {
        var db = new FakeDb { OnExecute = c => Out(c, "RETURN_VALUE", 0) };

        var result = await new SubmissionRepository(db).AddTissueAsync(Tissue(owner), 7, Ct);

        Assert.Equal(0, result);
        var cmd = Assert.Single(db.Commands);
        Assert.Equal(proc, cmd.Text);
        Assert.Equal(11, cmd.Parameters[key]);
        Assert.Equal("BRAIN", cmd.Parameters["TissueCode"]);
    }

    [Fact]
    public async Task UpdateTissue_SubmissionTissue_SendsArchiveColumns()
    {
        var db = new FakeDb();

        await new SubmissionRepository(db).UpdateTissueAsync(Tissue(), 7, Ct);

        var cmd = Assert.Single(db.Commands);
        Assert.Equal("EditTissue", cmd.Text);
        Assert.Equal("Shelf", cmd.Parameters["ArchiveLocation"]);
        Assert.Equal(new DateTime(2026, 1, 2), cmd.Parameters["ArchivedDate"]);
    }

    [Fact]
    public async Task UpdateTissue_BlockTissue_OmitsArchiveColumns()
    {
        var db = new FakeDb();

        await new SubmissionRepository(db).UpdateTissueAsync(Tissue(TissueOwner.Block), 7, Ct);

        var cmd = Assert.Single(db.Commands);
        Assert.Equal("EditBlockTissue", cmd.Text);
        Assert.DoesNotContain("ArchiveLocation", cmd.Parameters.Keys);
    }

    [Theory]
    [InlineData(TissueOwner.Submission, "DeleteTissue")]
    [InlineData(TissueOwner.Block, "DeleteBlockTissue")]
    public async Task DeleteTissue_CallsTheProcForTheOwnerType(TissueOwner owner, string proc)
    {
        var db = new FakeDb();

        await new SubmissionRepository(db).DeleteTissueAsync(4, owner, 7, Ct);

        var cmd = Assert.Single(db.Commands);
        Assert.Equal((proc, 4), (cmd.Text, cmd.Parameters["ID"]));
    }

    // ---------------- mouse range ----------------

    private static FakeDb MouseDb(int sourceAnimalId = 5, int existingSubmissionAnimal = 5)
    {
        var animalIds = new Queue<int>([200, 201, 202]);
        var submissionIds = new Queue<int>([300, 301, 302]);
        return new FakeDb
        {
            OnQuery = c => c.Text switch
            {
                "GetBatchSubmissionDetailsByBatchID" => Sets(Submissions((11, existingSubmissionAnimal, 3))),
                "GetBatchTissues" => Sets(TissueRows(11)),
                _ => null,
            },
            OnExecute = c =>
            {
                switch (c.Text)
                {
                    case "AddAnimal": Out(c, "NewID", animalIds.Dequeue()); Out(c, "RETURN_VALUE", 0); break;
                    case "AddBatchSubmission": Out(c, "NewID", submissionIds.Dequeue()); break;
                    case "AddTissue": Out(c, "RETURN_VALUE", 0); break;
                }
            },
        };
    }

    [Theory]
    [InlineData("MC1", "MC000003")]
    [InlineData("MC000003", "MC000001")]
    [InlineData("MC000003", "MC000003")]
    [InlineData("MC000001", "MC002000")]
    public async Task CreateMouseRange_InvalidOrOversizedRange_ReturnsFalseWithoutTouchingTheDatabase(string from, string to)
    {
        var db = new FakeDb();

        Assert.False(await new SubmissionRepository(db).CreateMouseRangeAsync(5, null, from, to, 7, Ct));
        Assert.Empty(db.Commands);
    }

    [Fact]
    public async Task CreateMouseRange_CopiesSourceTissuesOntoEachNewSubmission_AndCommits()
    {
        var db = MouseDb();

        var created = await new SubmissionRepository(db).CreateMouseRangeAsync(5, 5, "MC000001", "MC000002", 7, Ct);

        Assert.True(created);
        Assert.Equal(["MC000001", "MC000002"], db.Commands.Where(c => c.Text == "AddAnimal").Select(c => c.Parameters["SenderRef"]));
        Assert.Equal([4, 5], db.Commands.Where(c => c.Text == "AddBatchSubmission").Select(c => c.Parameters["Order"]));
        var tissueCopies = db.Commands.Where(c => c.Text == "AddTissue").ToList();
        Assert.Equal([300, 301], tissueCopies.Select(c => c.Parameters["BatchSubmissionID"]));
        Assert.All(tissueCopies, c => Assert.Equal("BRAIN", c.Parameters["TissueCode"]));
        Assert.True(db.Committed);
    }

    [Fact]
    public async Task CreateMouseRange_NoSourceAnimal_CreatesSubmissionsWithoutTissues()
    {
        var db = MouseDb();

        Assert.True(await new SubmissionRepository(db).CreateMouseRangeAsync(5, null, "MC000001", "MC000002", 7, Ct));

        Assert.DoesNotContain(db.Commands, c => c.Text == "AddTissue");
        Assert.Equal(2, db.Commands.Count(c => c.Text == "AddBatchSubmission"));
    }

    [Fact]
    public async Task CreateMouseRange_SourceAnimalNotInBatch_CopiesNoTissues()
    {
        var db = MouseDb();

        Assert.True(await new SubmissionRepository(db).CreateMouseRangeAsync(5, 99, "MC000001", "MC000002", 7, Ct));

        Assert.DoesNotContain(db.Commands, c => c.Text == "AddTissue");
    }

    [Fact]
    public async Task CreateMouseRange_AnimalAlreadyOnBatch_RollsBackWithRangeMessage()
    {
        var db = MouseDb(existingSubmissionAnimal: 200);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SubmissionRepository(db).CreateMouseRangeAsync(5, null, "MC000001", "MC000002", 7, Ct));

        Assert.Contains("MC000001", ex.Message);
        Assert.True(db.RolledBack);
        Assert.False(db.Committed);
    }

    [Fact]
    public async Task CreateMouseRange_AnimalCannotBeCreated_Throws()
    {
        var db = MouseDb();
        db.OnExecute = c => { if (c.Text == "AddAnimal") { Out(c, "NewID", 0); Out(c, "RETURN_VALUE", 0); } };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SubmissionRepository(db).CreateMouseRangeAsync(5, null, "MC000001", "MC000002", 7, Ct));

        Assert.Contains("MC000001", ex.Message);
        Assert.True(db.RolledBack);
    }

    // ---------------- CreateBatchWithCopiedSamplesAsync ----------------

    private static CopiedSamplePlan Sample(int? histologyType = null, int blockCount = 1, string senderRef = "S1-copy") => new()
    {
        SourceAnimal = new Animal { NextBlockRef = "03", OnHold = true, PMDate = "01/02/2026", IsPGNumber = true },
        NewSenderRef = senderRef,
        SourceSubmission = new BatchSubmission { SubmissionName = "Source", Order = 2 },
        Tissues = [Tissue()],
        HistologyRefType = histologyType,
        Blocks = Enumerable.Range(1, blockCount).Select(i => new CopiedBlockPlan
        {
            BlockRef = $"0{i}",
            CustomerRef = "cust",
            Comment = "cmt",
            Order = i,
            Tissues = [Tissue(TissueOwner.Block, "LIVER")],
            HistologyCodes = ["1", "1"],
            AntibodyCodes = ["A1"],
            StainCodes = ["S1"],
        }).ToList(),
    };

    private static readonly SelectedTestCodes Codes = new(["1", "2"], ["A1"], ["S1"]);

    private static FakeDb CopyDb(Action<FakeDb.ExecutedCommand>? extra = null) => new()
    {
        OnQuery = c => c.Text == "GetUnusedHistologyRefs" ? Sets(Table(("Ref", typeof(string)))) : null,
        OnExecute = c =>
        {
            extra?.Invoke(c);
            switch (c.Text)
            {
                case "AddBatch": Out(c, "BatchID", 100); break;
                case "AddAnimal": Out(c, "NewID", 200); Out(c, "RETURN_VALUE", 0); break;
                case "AddBatchSubmission": Out(c, "NewID", 300); break;
                case "AddBlock": Out(c, "NewID", 400); break;
                case "GetNextHistologyRef": Out(c, "NextHistologyRef", "10005"); break;
                case "AddTissue" or "AddBlockTissue": Out(c, "RETURN_VALUE", 0); break;
            }
        },
    };

    [Fact]
    public async Task CreateBatchWithCopiedSamples_CopiesEverything_InOneTransaction()
    {
        var db = CopyDb();

        var id = await new SubmissionRepository(db).CreateBatchWithCopiedSamplesAsync(new Batch(), Codes, "5", [Sample(histologyType: 1, blockCount: 2)], 7, Ct);

        Assert.Equal(100, id);
        Assert.True(db.Committed);
        Assert.False(db.RolledBack);
        Assert.Equal(
            ["AddBatch", "AddHistology", "AddHistology", "AddAntibodies", "AddSpecialStain", "AddSubmittedAs",
             "GetUnusedHistologyRefs", "GetNextHistologyRef", "AddAnimal", "AddBatchSubmission", "AddTissue",
             "AddBlock", "AddBlockTissue", "AddBlockHistology", "AddBlockAntibodies", "AddBlockStain",
             "AddBlock", "AddBlockTissue", "AddBlockHistology", "AddBlockAntibodies", "AddBlockStain"],
            db.Commands.Select(c => c.Text));
        var animal = db.Commands.Single(c => c.Text == "AddAnimal");
        Assert.EndsWith("/10005", (string)animal.Parameters["HistologyRef"]!);
        Assert.Equal("S1-copy", animal.Parameters["SenderRef"]);
        Assert.Equal(300, db.Commands.Single(c => c.Text == "AddTissue").Parameters["BatchSubmissionID"]);
        Assert.Equal(400, db.Commands.First(c => c.Text == "AddBlockTissue").Parameters["BlockID"]);
        Assert.Equal(100, db.Commands.First(c => c.Text == "AddBlockHistology").Parameters["BatchID"]);
    }

    [Fact]
    public async Task CreateBatchWithCopiedSamples_NoSubmittedAsCode_SkipsAddSubmittedAs()
    {
        var db = CopyDb();

        await new SubmissionRepository(db).CreateBatchWithCopiedSamplesAsync(new Batch(), Codes, " ", [], 7, Ct);

        Assert.DoesNotContain(db.Commands, c => c.Text == "AddSubmittedAs");
        Assert.True(db.Committed);
    }

    [Fact]
    public async Task CreateBatchWithCopiedSamples_HeaderNotCreated_ThrowsAndRollsBack()
    {
        var db = CopyDb(c => { if (c.Text == "AddBatch") Out(c, "BatchID", 0); });
        var original = db.OnExecute;
        db.OnExecute = c => { original(c); if (c.Text == "AddBatch") Out(c, "BatchID", 0); };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SubmissionRepository(db).CreateBatchWithCopiedSamplesAsync(new Batch(), Codes, null, [], 7, Ct));

        Assert.Contains("new submission header", ex.Message);
        Assert.True(db.RolledBack);
    }

    [Fact]
    public async Task CreateBatchWithCopiedSamples_AddBatchOutputParamRejected_FallsBackToReturnValue()
    {
        var calls = 0;
        var db = CopyDb();
        var original = db.OnExecute;
        db.OnExecute = c =>
        {
            if (c.Text == "AddBatch")
            {
                if (calls++ == 0) throw NewSqlException(8144);
                Out(c, "RETURN_VALUE", 150);
                return;
            }
            original(c);
        };

        var id = await new SubmissionRepository(db).CreateBatchWithCopiedSamplesAsync(new Batch(), Codes, null, [], 7, Ct);

        Assert.Equal(150, id);
        Assert.Equal(2, db.Commands.Count(c => c.Text == "AddBatch"));
    }

    [Fact]
    public async Task CreateBatchWithCopiedSamples_SqlErrorWhileSavingSelections_NamesTheStepAndRollsBack()
    {
        var db = CopyDb(c => { if (c.Text == "AddAntibodies") throw NewSqlException(547); });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SubmissionRepository(db).CreateBatchWithCopiedSamplesAsync(new Batch(), Codes, "5", [Sample()], 7, Ct));

        Assert.Equal("Could not save the addantibodies selections (database error 547). Nothing was saved.", ex.Message);
        Assert.IsType<SqlException>(ex.InnerException);
        Assert.True(db.RolledBack);
        Assert.False(db.Committed);
    }

    [Fact]
    public async Task CreateBatchWithCopiedSamples_SqlErrorWhileCreatingSample_NamesTheSample()
    {
        var db = CopyDb(c => { if (c.Text == "AddAnimal") throw NewSqlException(2627); });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SubmissionRepository(db).CreateBatchWithCopiedSamplesAsync(new Batch(), Codes, null, [Sample()], 7, Ct));

        Assert.Equal("Could not create sample S1-copy (database error 2627). Nothing was saved.", ex.Message);
    }

    [Fact]
    public async Task CreateBatchWithCopiedSamples_SqlErrorWhileSavingSubmissionType_NamesTheStep()
    {
        var db = CopyDb(c => { if (c.Text == "AddSubmittedAs") throw NewSqlException(1); });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SubmissionRepository(db).CreateBatchWithCopiedSamplesAsync(new Batch(), Codes, "5", [], 7, Ct));

        Assert.StartsWith("Could not save the submission type (database error 1)", ex.Message);
    }

    [Fact]
    public async Task CreateBatchWithCopiedSamples_NonSqlFailure_RollsBackAndRethrowsOriginal()
    {
        var db = CopyDb(c => { if (c.Text == "AddBlock") throw new TimeoutException("slow"); });

        await Assert.ThrowsAsync<TimeoutException>(() =>
            new SubmissionRepository(db).CreateBatchWithCopiedSamplesAsync(new Batch(), Codes, null, [Sample()], 7, Ct));

        Assert.True(db.RolledBack);
    }

    [Fact]
    public async Task CreateBatchWithCopiedSamples_HistologyRefAlreadyAssigned_ThrowsNamingTheRef()
    {
        var db = CopyDb();
        var original = db.OnExecute;
        db.OnExecute = c =>
        {
            original(c);
            if (c.Text == "AddAnimal") { Out(c, "NewID", 0); Out(c, "RETURN_VALUE", 2); }
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SubmissionRepository(db).CreateBatchWithCopiedSamplesAsync(new Batch(), Codes, null, [Sample(histologyType: 1)], 7, Ct));

        Assert.Contains("histology reference", ex.Message);
        Assert.Contains("S1-copy", ex.Message);
    }

    [Theory]
    [InlineData("animal", "Could not create sample 'S1-copy'")]
    [InlineData("submission", "Failed to create the submission record for S1-copy")]
    [InlineData("block", "Could not copy block '01' for sample 'S1-copy'")]
    public async Task CreateBatchWithCopiedSamples_InsertReturnsNoId_ThrowsDescriptiveError(string failing, string expected)
    {
        var db = CopyDb();
        var original = db.OnExecute;
        db.OnExecute = c =>
        {
            original(c);
            if (failing == "animal" && c.Text == "AddAnimal") Out(c, "NewID", 0);
            if (failing == "submission" && c.Text == "AddBatchSubmission") Out(c, "NewID", 0);
            if (failing == "block" && c.Text == "AddBlock") Out(c, "NewID", 0);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SubmissionRepository(db).CreateBatchWithCopiedSamplesAsync(new Batch(), Codes, null, [Sample()], 7, Ct));

        Assert.StartsWith(expected, ex.Message);
        Assert.True(db.RolledBack);
    }

    [Fact]
    public async Task CreateBatchWithCopiedSamples_ReusesPooledCurrentYearRefInTypeRange_WithoutDrawingNext()
    {
        var year = DateTime.Now.Year % 100;
        var pool = Table(("Ref", typeof(string)));
        pool.Rows.Add($"{year:D2}/20007");
        pool.Rows.Add($"{year:D2}/20003");
        pool.Rows.Add($"{year:D2}/10003");
        pool.Rows.Add($"{(year + 1) % 100:D2}/20001");
        var db = CopyDb();
        db.OnQuery = c => c.Text == "GetUnusedHistologyRefs" ? Sets(pool) : null;

        await new SubmissionRepository(db).CreateBatchWithCopiedSamplesAsync(new Batch(), Codes, null, [Sample(histologyType: 2)], 7, Ct);

        Assert.DoesNotContain(db.Commands, c => c.Text == "GetNextHistologyRef");
        Assert.Equal($"{year:D2}/20003", db.Commands.Single(c => c.Text == "AddAnimal").Parameters["HistologyRef"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(6)]
    public async Task CreateBatchWithCopiedSamples_NoHistologyRefTypeOrUnknownType_AssignsNoRef(int? histologyType)
    {
        var db = CopyDb();

        await new SubmissionRepository(db).CreateBatchWithCopiedSamplesAsync(new Batch(), Codes, null, [Sample(histologyType)], 7, Ct);

        var animal = db.Commands.Single(c => c.Text == "AddAnimal");
        Assert.Equal(DBNull.Value, animal.Parameters["HistologyRef"]);
        Assert.DoesNotContain(db.Commands, c => c.Text == "GetNextHistologyRef");
    }

    [Theory]
    [InlineData("99999")]
    [InlineData("abc")]
    public async Task CreateBatchWithCopiedSamples_DrawnRefOutsideTypeRangeOrNonNumeric_IsRefused(string drawn)
    {
        var db = CopyDb();
        var original = db.OnExecute;
        db.OnExecute = c =>
        {
            original(c);
            if (c.Text == "GetNextHistologyRef") Out(c, "NextHistologyRef", drawn);
        };

        await new SubmissionRepository(db).CreateBatchWithCopiedSamplesAsync(new Batch(), Codes, null, [Sample(histologyType: 1)], 7, Ct);

        Assert.Equal(DBNull.Value, db.Commands.Single(c => c.Text == "AddAnimal").Parameters["HistologyRef"]);
    }
}
