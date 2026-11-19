using System.Data;
using Histo.Administration.Models;
using Histo.Administration.Repositories;
using Histo.Histology.Repositories;
using Histo.QualityControl.Models;
using Histo.QualityControl.Repositories;
using Histo.Tests.Support;

namespace Histo.Tests.Unit;

/// <summary>Lookup, user, QC note and block-test repositories against <see cref="FakeDb"/>.</summary>
public class AdminAndQcRepositoryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static DataTable Table(params (string Column, Type Type)[] columns)
    {
        var table = new DataTable();
        foreach (var (column, type) in columns) table.Columns.Add(column, type);
        return table;
    }

    private static DataSet One(DataTable table) => new DataSet().Also(ds => ds.Tables.Add(table.Copy()));

    private static DataTable Procs(string select, string update, string insert)
    {
        var t = Table(("SelectStoredProcedure", typeof(string)), ("UpdateStoredProcedure", typeof(string)),
            ("InsertStoredProcedure", typeof(string)), ("DeleteStoredProcedure", typeof(string)));
        t.Rows.Add(select, update, insert, "DeleteluThing");
        return t;
    }

    private static FakeDb LookupDb(DataTable rows, string select = "GetluThing", string update = "EditluThing", string insert = "AddluThing") =>
        new() { OnQuery = c => One(c.Text == "GetEditableLookupProcs" ? Procs(select, update, insert) : rows) };

    private static DataTable IdDescription(params (int Id, string Description, bool Active)[] items)
    {
        var t = Table(("ID", typeof(int)), ("Description", typeof(string)), ("IsActive", typeof(bool)));
        foreach (var (id, description, active) in items) t.Rows.Add(id, description, active);
        return t;
    }

    // ---------------- LookupRepository ----------------

    [Fact]
    public async Task Lookup_GetLookupData_UsesResolvedSelectProc_AndMapsRows()
    {
        var rows = Table(("ID", typeof(int)), ("Description", typeof(string)), ("IsActive", typeof(bool)),
            ("Code", typeof(string)), ("Area", typeof(string)));
        rows.Rows.Add(3, "Alpha", false, "A1  ", " N ");
        var db = LookupDb(rows);

        var items = await new LookupRepository(db).GetLookupDataAsync(4, ct: Ct);

        var item = Assert.Single(items);
        Assert.Equal((3, "Alpha", false, "A1", "N"), (item.ID, item.Name, item.Active, item.Code, item.Area));
        Assert.Equal(4, db.Commands[0].Parameters["ID"]);
        Assert.Equal("GetluThing", db.Commands[1].Text);
    }

    [Fact]
    public async Task Lookup_GetLookupData_IncludeInactive_AppendsAllToProcName()
    {
        var db = LookupDb(IdDescription((1, "One", true)));

        await new LookupRepository(db).GetLookupDataAsync(4, includeInactive: true, ct: Ct);

        Assert.Equal("GetluThingAll", db.Commands[1].Text);
    }

    [Fact]
    public async Task Lookup_GetLookupData_MissingSelectProc_Throws()
    {
        var db = LookupDb(IdDescription(), select: "");

        await Assert.ThrowsAsync<InvalidOperationException>(() => new LookupRepository(db).GetLookupDataAsync(9, ct: Ct));
    }

    [Fact]
    public async Task Lookup_GetLookupData_RowWithoutIsActiveColumn_DefaultsToActive()
    {
        var rows = Table(("ID", typeof(string)), ("Description", typeof(string)));
        rows.Rows.Add("not-a-number", "Beta");

        var item = Assert.Single(await new LookupRepository(LookupDb(rows)).GetLookupDataAsync(1, ct: Ct));

        Assert.True(item.Active);
        Assert.Equal(0, item.ID);
        Assert.Null(item.Code);
    }

    [Fact]
    public async Task Lookup_GetUserAreaData_PassesUserAreaToResolvedProc()
    {
        var db = LookupDb(IdDescription((1, "One", true)));

        var items = await new LookupRepository(db).GetUserAreaDataAsync(4, "NI", Ct);

        Assert.Single(items);
        Assert.Equal("NI", db.Commands[1].Parameters["UserArea"]);
    }

    [Fact]
    public async Task Lookup_ListEditableLookups_MapsStoredProcedureNames()
    {
        var t = Table(("ID", typeof(int)), ("Description", typeof(string)), ("SelectStoredProcedure", typeof(string)),
            ("UpdateStoredProcedure", typeof(string)), ("InsertStoredProcedure", typeof(string)), ("DeleteStoredProcedure", typeof(string)));
        t.Rows.Add(2, "Fixation", "GetluFixation", "EditluFixation", "AddluFixation", "DeleteluFixation");
        var db = new FakeDb { OnQuery = _ => One(t) };

        var lookup = Assert.Single(await new LookupRepository(db).ListEditableLookupsAsync(Ct));

        Assert.Equal(
            (2, "Fixation", "GetluFixation", "EditluFixation", "AddluFixation", "DeleteluFixation"),
            (lookup.ID, lookup.TableName, lookup.SelectStoredProcedure, lookup.UpdateStoredProcedure, lookup.InsertStoredProcedure, lookup.DeleteStoredProcedure));
    }

    [Fact]
    public async Task Lookup_GetContactsAndProjectsByArea_CallAreaProcsWithArea()
    {
        var db = new FakeDb { OnQuery = _ => One(IdDescription((1, "Contact", true))) };
        var repo = new LookupRepository(db);

        Assert.Single(await repo.GetContactsByAreaAsync("NI", Ct));
        Assert.Single(await repo.GetProjectsByAreaAsync("GB", Ct));

        Assert.Equal(["GetContactsArea", "GetProjectsArea"], db.Commands.Select(c => c.Text));
        Assert.Equal(["NI", "GB"], db.Commands.Select(c => c.Parameters["Area"]));
    }

    [Fact]
    public async Task Lookup_CodeDescriptionReaders_MapCodeToId_AndAreAlwaysActive()
    {
        var t = Table(("Code", typeof(string)), ("Description", typeof(string)));
        t.Rows.Add("12", "Group");
        t.Rows.Add("", "Blank code");
        var db = new FakeDb { OnQuery = _ => One(t) };
        var repo = new LookupRepository(db);

        var groups = await repo.GetUserGroupsAsync(Ct);
        var refTypes = await repo.GetHistologyRefTypesAsync(Ct);
        var areas = await repo.GetUserAreasAsync(ct: Ct);
        var allAreas = await repo.GetUserAreasAsync(includeInactive: true, ct: Ct);

        Assert.Equal([12, 0], groups.Select(g => g.ID));
        Assert.All(groups.Concat(refTypes).Concat(areas).Concat(allAreas), g => Assert.True(g.Active));
        Assert.Equal(["GetluUserGroup", "GetluHistologyRefType", "GetluUserArea", "GetluUserAreaAll"], db.Commands.Select(c => c.Text));
    }

    [Fact]
    public async Task Lookup_GetSpeciesLookup_MapsSpeciesIdAndName()
    {
        var t = Table(("SpeciesID", typeof(int)), ("Species", typeof(string)));
        t.Rows.Add(5, "Bovine");
        t.Rows.Add(6, DBNull.Value);
        var db = new FakeDb { OnQuery = _ => One(t) };

        var items = await new LookupRepository(db).GetSpeciesLookupAsync(Ct);

        Assert.Equal([(5, "Bovine"), (6, "")], items.Select(i => (i.ID, i.Name)));
    }

    [Fact]
    public async Task Lookup_LookupItemReaders_PreferNameThenDescription_AndActiveThenIsActive()
    {
        var t = Table(("ID", typeof(int)), ("Name", typeof(string)), ("Description", typeof(string)),
            ("Active", typeof(bool)), ("IsActive", typeof(bool)), ("Code", typeof(string)), ("Area", typeof(string)));
        t.Rows.Add(1, "FromName", "FromDescription", true, false, "C1", "NI");
        t.Rows.Add(2, DBNull.Value, "FromDescription", false, DBNull.Value, DBNull.Value, DBNull.Value);
        t.Rows.Add(3, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);
        var db = new FakeDb { OnQuery = _ => One(t) };
        var repo = new LookupRepository(db);

        var histology = (await repo.GetHistologyTypesAsync(Ct)).ToList();
        var premium = await repo.GetPremiumChargesAsync(Ct);

        Assert.Equal(["FromName", "FromDescription", ""], histology.Select(i => i.Name));
        Assert.Equal([false, false, true], histology.Select(i => i.Active));
        Assert.Equal(("C1", "NI"), (histology[0].Code, histology[0].Area));
        Assert.Null(histology[1].Code);
        Assert.Equal(3, premium.Count);
        Assert.Equal(["GetluHistology", "GetluPremiumCharges"], db.Commands.Select(c => c.Text));
    }

    [Fact]
    public async Task Lookup_ActiveColumnWithoutIsActive_IsUsedForMapping()
    {
        var t = Table(("ID", typeof(int)), ("Active", typeof(bool)));
        t.Rows.Add(1, false);
        var db = new FakeDb { OnQuery = _ => One(t) };

        var item = Assert.Single(await new LookupRepository(db).GetPremiumChargesAsync(Ct));

        Assert.False(item.Active);
    }

    [Fact]
    public async Task Lookup_GetSpeciesItems_MapsTypedRows()
    {
        var t = Table(("SpeciesID", typeof(int)), ("Species", typeof(string)), ("CommonName", typeof(string)));
        t.Rows.Add(1, "Bos taurus", "Cow");
        var db = new FakeDb { OnQuery = _ => One(t) };

        var item = Assert.Single(await new LookupRepository(db).GetSpeciesItemsAsync(Ct));

        Assert.Equal((1, "Bos taurus", "Cow"), (item.SpeciesID, item.Species, item.CommonName));
    }

    [Fact]
    public async Task Lookup_AddAndUpdateSpeciesItem_CallDedicatedProcs()
    {
        var db = new FakeDb();
        var repo = new LookupRepository(db);

        await repo.AddSpeciesItemAsync(1, "Bos taurus", "Cow", Ct);
        await repo.UpdateSpeciesItemAsync(1, "Bos taurus", null, 7, Ct);

        Assert.Equal(["AddtlkpSpecies", "EdittlkpSpecies"], db.Commands.Select(c => c.Text));
        Assert.Equal("Cow", db.Commands[0].Parameters["CommonName"]);
        Assert.Equal(7, db.Commands[1].Parameters["UserID"]);
    }

    [Fact]
    public async Task Lookup_GetImportedTables_MapsIdAndName()
    {
        var t = Table(("ID", typeof(string)), ("Name", typeof(string)));
        t.Rows.Add("4", "Antibodies");
        var db = new FakeDb { OnQuery = _ => One(t) };

        var item = Assert.Single(await new LookupRepository(db).GetImportedTablesAsync(Ct));

        Assert.Equal((4, "Antibodies"), (item.ID, item.Name));
    }

    [Fact]
    public async Task Lookup_CreateLookupItem_CodeKeyedWithArea_SendsCodeAndArea_NotId()
    {
        var db = LookupDb(IdDescription());

        await new LookupRepository(db).CreateLookupItemAsync(16, new LookupItem { Name = "Shelf", Active = true, Code = "A1", Area = "NI" }, 7, Ct);

        var insert = db.Commands.Single(c => c.Text == "AddluThing");
        Assert.Equal("A1", insert.Parameters["Code"]);
        Assert.Equal("NI", insert.Parameters["Area"]);
        Assert.DoesNotContain("ID", insert.Parameters.Keys);
    }

    [Fact]
    public async Task Lookup_CreateLookupItem_IdKeyed_SendsId_NotCodeOrArea()
    {
        var db = LookupDb(IdDescription());

        await new LookupRepository(db).CreateLookupItemAsync(18, new LookupItem { ID = 0, Name = "Contact", Active = true }, 7, Ct);

        var insert = db.Commands.Single(c => c.Text == "AddluThing");
        Assert.Equal(0, insert.Parameters["ID"]);
        Assert.DoesNotContain("Code", insert.Parameters.Keys);
        Assert.DoesNotContain("Area", insert.Parameters.Keys);
    }

    [Fact]
    public async Task Lookup_CreateLookupItem_MissingInsertProc_Throws()
    {
        var db = LookupDb(IdDescription(), insert: " ");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LookupRepository(db).CreateLookupItemAsync(18, new LookupItem { Name = "X" }, 7, Ct));
    }

    [Fact]
    public async Task Lookup_UpdateLookupItem_WithOriginalCode_SendsOriginalCodeAndNewCode()
    {
        var db = LookupDb(IdDescription());

        await new LookupRepository(db).UpdateLookupItemAsync(16, new LookupItem { Name = "Shelf", Active = false, Code = "B2" }, 7, "A1", Ct);

        var update = db.Commands.Single(c => c.Text == "EditluThing");
        Assert.Equal("A1", update.Parameters["Original_Code"]);
        Assert.Equal("B2", update.Parameters["Code"]);
        Assert.Equal(7, update.Parameters["UserID"]);
        Assert.DoesNotContain("ID", update.Parameters.Keys);
    }

    [Fact]
    public async Task Lookup_UpdateLookupItem_WithoutOriginalCode_SendsId()
    {
        var db = LookupDb(IdDescription());

        await new LookupRepository(db).UpdateLookupItemAsync(18, new LookupItem { ID = 5, Name = "Contact" }, 7, ct: Ct);

        var update = db.Commands.Single(c => c.Text == "EditluThing");
        Assert.Equal(5, update.Parameters["ID"]);
        Assert.DoesNotContain("Original_Code", update.Parameters.Keys);
    }

    [Fact]
    public async Task Lookup_UpdateLookupItem_MissingUpdateProc_Throws()
    {
        var db = LookupDb(IdDescription(), update: "");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LookupRepository(db).UpdateLookupItemAsync(18, new LookupItem { Name = "X" }, 7, ct: Ct));
    }

    // ---------------- UserRepository ----------------

    private static DataTable UserRows()
    {
        var t = Table(("ID", typeof(int)), ("Name", typeof(string)), ("NTLogin", typeof(string)), ("UserGroup", typeof(string)),
            ("GroupName", typeof(string)), ("UserArea", typeof(int)), ("AreaName", typeof(string)), ("Email", typeof(string)), ("Active", typeof(bool)));
        t.Rows.Add(1, "Ann", "DEFRA\\ann", "3", "Admins", 2, "NI", "ann@example.test", true);
        t.Rows.Add(2, "Bob", "DEFRA\\bob", "", "Users", 1, "GB", "bob@example.test", false);
        return t;
    }

    [Fact]
    public async Task User_GetUsersAndByArea_MapEveryColumn_AndTolerateNonNumericGroups()
    {
        var db = new FakeDb { OnQuery = _ => One(UserRows()) };
        var repo = new UserRepository(db);

        var users = await repo.GetUsersAsync(Ct);
        var byArea = await repo.GetUsersByAreaAsync("NI", Ct);

        Assert.Equal(2, users.Count);
        Assert.Equal(("Ann", "DEFRA\\ann", 3, "Admins", 2, "NI", "ann@example.test", true),
            (users[0].Name, users[0].NtLogin, users[0].GroupCode, users[0].GroupName, users[0].AreaCode, users[0].AreaName, users[0].Email, users[0].Active));
        Assert.Equal(0, users[1].GroupCode);
        Assert.False(users[1].Active);
        Assert.Equal(2, byArea.Count);
        Assert.Equal("NI", db.Commands[1].Parameters["UserArea"]);
    }

    [Fact]
    public async Task User_GetUserByEmail_InactiveUser_ReturnsNull_ActiveUserIsMapped()
    {
        var db = new FakeDb
        {
            OnQuery = c =>
            {
                var rows = UserRows();
                rows.Rows.RemoveAt(c.Parameters["Email"] as string == "bob@example.test" ? 0 : 1);
                return One(rows);
            },
        };
        var repo = new UserRepository(db);

        var active = await repo.GetUserByEmailAsync("ann@example.test", Ct);
        var inactive = await repo.GetUserByEmailAsync("bob@example.test", Ct);

        Assert.Equal("Ann", active?.Name);
        Assert.Null(inactive);
    }

    [Fact]
    public async Task User_GetUserByNtLogin_ActiveMapped_InactiveAndMissingReturnNull()
    {
        var db = new FakeDb
        {
            OnQuery = c =>
            {
                var rows = UserRows();
                var login = c.Parameters["NTLogin"] as string;
                for (var i = rows.Rows.Count - 1; i >= 0; i--)
                    if ((string)rows.Rows[i]["NTLogin"] != login) rows.Rows.RemoveAt(i);
                return One(rows);
            },
        };
        var repo = new UserRepository(db);

        var active = await repo.GetUserByNtLoginAsync("DEFRA\\ann", Ct);

        Assert.Equal(("Ann", 3, "Admins", 2, "NI", "ann@example.test"),
            (active?.Name, active?.GroupCode, active?.GroupName, active?.AreaCode, active?.AreaName, active?.Email));
        Assert.Null(await repo.GetUserByNtLoginAsync("DEFRA\\bob", Ct));
        Assert.Null(await repo.GetUserByNtLoginAsync("nobody", Ct));
    }

    // ---------------- QCNoteRepository ----------------

    private static DataTable QcHeader()
    {
        var t = Table(("ID", typeof(int)), ("QCNoteRef", typeof(int)), ("StainRef", typeof(string)),
            ("ProjectDescription", typeof(string)), ("Species", typeof(string)), ("BatchType", typeof(int)));
        t.Rows.Add(1, 10, "S1", "Project A", "Bovine", 3);
        return t;
    }

    private static DataTable QcDetail(string qcText, object dateCreated)
    {
        var t = Table(("QCText", typeof(string)), ("Name", typeof(string)), ("DateCreated", typeof(object)), ("RowStamp", typeof(byte[])),
            ("SenderRef", typeof(string)), ("HistologyRef", typeof(string)), ("BlockRef", typeof(string)), ("Description", typeof(string)));
        t.Rows.Add(qcText, "Ann", dateCreated, new byte[] { 1, 2 }, "SR1", "H1", "B1", "H&E");
        return t;
    }

    private static FakeDb QcDb(DataTable? header, DataTable histStain, DataTable antibodies) =>
        new()
        {
            OnQuery = c => c.Text switch
            {
                "GetBatchQCNotes" => One(header ?? Table(("ID", typeof(int)))),
                "GetQCNoteHistStainTestInformation" => One(histStain),
                _ => One(antibodies),
            },
        };

    [Fact]
    public async Task QCNote_GetById_NoHeader_ReturnsNull_AndSkipsDetailQueries()
    {
        var db = QcDb(null, QcDetail("", DBNull.Value), QcDetail("", DBNull.Value));

        Assert.Null(await new QCNoteRepository(db).GetByIdAsync(10, Ct));
        Assert.Single(db.Commands);
    }

    [Fact]
    public async Task QCNote_GetById_SavedText_IsReturnedWithHeaderAndAuditFields()
    {
        var db = QcDb(QcHeader(), QcDetail("Saved note", new DateTime(2026, 3, 4)), Table(("Description", typeof(string))));

        var note = await new QCNoteRepository(db).GetByIdAsync(10, Ct);

        Assert.NotNull(note);
        Assert.Equal(("Saved note", "Ann", new DateTime(2026, 3, 4), 10, "S1", "Project A", "Bovine"),
            (note.Text, note.CreatedBy, note.DateCreated, note.QCNoteRef, note.StainRef, note.ProjectDescription, note.Species));
        Assert.Equal(new byte[] { 1, 2 }, note.RowStamp);
        Assert.Equal(3, db.Commands.Single(c => c.Text == "GetQCNoteAntibodiesInformation").Parameters["SubmissionType"]);
    }

    [Fact]
    public async Task QCNote_GetById_BlankText_BuildsDefaultTableFromBothDetailSets()
    {
        var antibodies = QcDetail("", DBNull.Value);
        antibodies.Rows[0]["Description"] = "IHC";
        var db = QcDb(QcHeader(), QcDetail("", DBNull.Value), antibodies);

        var note = await new QCNoteRepository(db).GetByIdAsync(10, Ct);

        Assert.NotNull(note);
        var lines = note.Text!.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.StartsWith("Sender Ref", lines[0]);
        Assert.EndsWith("Test", lines[0]);
        Assert.Equal(3, lines.Length);
        Assert.StartsWith("SR1", lines[1]);
        Assert.EndsWith("H&E", lines[1]);
        Assert.EndsWith("IHC", lines[2]);
        Assert.Null(note.DateCreated);
    }

    [Theory]
    [InlineData("04/03/2026", 2026, 3, 4)]
    [InlineData("04/03/2026 10:15:30", 2026, 3, 4)]
    [InlineData("2026-03-04", 2026, 3, 4)]
    public async Task QCNote_GetById_DateCreatedAsText_IsParsed(string text, int year, int month, int day)
    {
        var db = QcDb(QcHeader(), QcDetail("x", text), Table(("Description", typeof(string))));

        var note = await new QCNoteRepository(db).GetByIdAsync(10, Ct);

        Assert.Equal(new DateTime(year, month, day), note!.DateCreated!.Value.Date);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("not a date")]
    public async Task QCNote_GetById_UnparseableDateCreated_IsNull(string text)
    {
        var db = QcDb(QcHeader(), QcDetail("x", text), Table(("Description", typeof(string))));

        var note = await new QCNoteRepository(db).GetByIdAsync(10, Ct);

        Assert.Null(note!.DateCreated);
    }

    [Fact]
    public async Task QCNote_Update_Succeeds_WhenReturnValueIsZero()
    {
        var db = new FakeDb { OnExecute = c => c.Command.Parameters["RETURN_VALUE"].Value = 0 };

        await new QCNoteRepository(db).UpdateAsync(10, "text", [1], 7, Ct);

        var update = Assert.Single(db.Commands, c => c.Text == "EditQCNote");
        Assert.Equal("text", update.Parameters["QCText"]);
        Assert.Equal(7, update.Parameters["UserID"]);
    }

    [Fact]
    public async Task QCNote_Update_ReturnValueOne_ThrowsConcurrencyException()
    {
        var db = new FakeDb { OnExecute = c => c.Command.Parameters["RETURN_VALUE"].Value = 1 };

        await Assert.ThrowsAsync<QCNoteConcurrencyException>(() => new QCNoteRepository(db).UpdateAsync(10, "text", [1], 7, Ct));
    }

    [Fact]
    public async Task QCNote_Add_ReturnsNewIdFromOutputParameter()
    {
        var db = new FakeDb { OnExecute = c => c.Command.Parameters["NewID"].Value = 55 };

        var id = await new QCNoteRepository(db).AddAsync(3, 7, Ct);

        Assert.Equal(55, id);
        Assert.Equal(7, db.Commands.Single(c => c.Text == "AddQCNote").Parameters["CreatedBy"]);
    }

    // ---------------- BlockTestRepository.SaveTestSelectionsAsync ----------------

    private static DataSet BatchBlocks()
    {
        var details = Table(("ID", typeof(int)), ("BlockRef", typeof(string)), ("AnimalID", typeof(int)), ("Status", typeof(int)));
        details.Rows.Add(1, "01", 2, 1);
        details.Rows.Add(2, "02", 2, 1);

        DataTable Tests(params (int Id, int BlockId, string Code)[] rows)
        {
            var t = Table(("ID", typeof(int)), ("BlockID", typeof(int)), ("Code", typeof(string)));
            foreach (var (id, blockId, code) in rows) t.Rows.Add(id, blockId, code);
            return t;
        }

        DataTable Tc() => Table(("TestID", typeof(int)), ("ID", typeof(int)), ("Code", typeof(string)));

        var animals = Table(("ID", typeof(int)), ("HistologyRef", typeof(string)));
        animals.Rows.Add(2, "H1");

        var ds = new DataSet();
        ds.Tables.Add(details);
        ds.Tables.Add(Table(("ID", typeof(int))));
        ds.Tables.Add(Tests((10, 1, "1"), (11, 1, "3"), (12, 2, "9")));
        ds.Tables.Add(Tests((20, 1, "A1")));
        ds.Tables.Add(Tests((30, 1, "S1")));
        ds.Tables.Add(animals);
        ds.Tables.Add(Table(("ID", typeof(int))));
        ds.Tables.Add(Tc());
        ds.Tables.Add(Tc());
        ds.Tables.Add(Tc());
        return ds;
    }

    [Fact]
    public async Task BlockTest_SaveTestSelections_DeletesRemovedAndAddsNewCodes_ForThatBlockOnly()
    {
        var db = new FakeDb { OnQuery = c => c.Text == "GetBatchBlocksByID" ? BatchBlocks() : null };

        await new BlockTestRepository(db).SaveTestSelectionsAsync(5, 1, ["1", "2"], [], ["s1", "S2", "s2"], 7, Ct);

        var changes = db.Commands.Where(c => c.Text != "GetBatchBlocksByID").ToList();
        Assert.Equal(["DeleteBlockHistology", "AddBlockHistology", "DeleteBlockAntibodies", "AddBlockStain"], changes.Select(c => c.Text));
        Assert.Equal(11, changes[0].Parameters["ID"]);
        Assert.Equal("2", changes[1].Parameters["Code"]);
        Assert.Equal(20, changes[2].Parameters["ID"]);
        Assert.Equal("S2", changes[3].Parameters["Code"]);
        Assert.All(changes.Where(c => c.Text.StartsWith("Add", StringComparison.Ordinal)), c =>
        {
            Assert.Equal(1, c.Parameters["BlockID"]);
            Assert.Equal(5, c.Parameters["BatchID"]);
            Assert.Equal(7, c.Parameters["UserID"]);
        });
    }
}
