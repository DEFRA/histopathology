using System.Data;
using Histo.Submissions.Models;
using Histo.Submissions.Repositories;
using Histo.Tests.Support;

namespace Histo.Tests.Unit;

/// <summary>Batch field updates and premium-charge reports of <see cref="BatchRepository"/>, exercised against <see cref="FakeDb"/>.</summary>
public class BatchRepositoryCoverageTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static DataTable Table(string name, (string Column, Type Type)[] columns, params object[][] rows)
    {
        var table = new DataTable(name);
        foreach (var (column, type) in columns) table.Columns.Add(column, type);
        foreach (var row in rows) table.Rows.Add(row);
        return table;
    }

    private static DataSet Single(DataTable table)
    {
        var ds = new DataSet();
        ds.Tables.Add(table);
        return ds;
    }

    private static DataSet Header() =>
        Single(Table("batch", [("ID", typeof(int)), ("BatchStatus", typeof(int)), ("Comments", typeof(string))], [42, 2, "note"]));

    private static DataSet NoBatch() => Single(Table("batch", [("ID", typeof(int))]));

    private static FakeDb.ExecutedCommand SingleEdit(FakeDb db) => Assert.Single(db.Commands, c => c.Text == "EditBatch");

    [Fact]
    public async Task SetCustomerReceivedDateAsync_WritesDateAndRowStamp_KeepingOtherFields()
    {
        var date = new DateTime(2026, 3, 4);
        byte[] stamp = [1, 2, 3];
        var db = new FakeDb { OnQuery = c => c.Text == "GetCommonBatchTablesByID" ? Header() : null };

        await new BatchRepository(db).SetCustomerReceivedDateAsync(42, date, stamp, 7, Ct);

        var edit = SingleEdit(db);
        Assert.Equal(date, edit.Parameters["CustomerReceivedDate"]);
        Assert.Equal(stamp, edit.Parameters["RowStamp"]);
        Assert.Equal("note", edit.Parameters["Comments"]);
        Assert.Equal(7, edit.Parameters["UserID"]);
    }

    [Fact]
    public async Task SetCustomerReceivedDateAsync_BatchNotFound_WritesNothing()
    {
        var db = new FakeDb { OnQuery = _ => NoBatch() };

        await new BatchRepository(db).SetCustomerReceivedDateAsync(42, new DateTime(2026, 3, 4), [1], 7, Ct);

        Assert.DoesNotContain(db.Commands, c => c.Text == "EditBatch");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetByPassSortAsync_WritesFlag(bool byPassSort)
    {
        var db = new FakeDb { OnQuery = c => c.Text == "GetCommonBatchTablesByID" ? Header() : null };

        await new BatchRepository(db).SetByPassSortAsync(42, byPassSort, 7, Ct);

        Assert.Equal(byPassSort, SingleEdit(db).Parameters["ByPassSort"]);
    }

    [Fact]
    public async Task SetByPassSortAsync_BatchNotFound_WritesNothing()
    {
        var db = new FakeDb { OnQuery = _ => NoBatch() };

        await new BatchRepository(db).SetByPassSortAsync(42, true, 7, Ct);

        Assert.DoesNotContain(db.Commands, c => c.Text == "EditBatch");
    }

    [Fact]
    public async Task RefreshAllTissuesAssignedAsync_AllAssignedAndFlagWasOff_WritesFlagOn()
    {
        var db = new FakeDb
        {
            OnQuery = c => c.Text switch
            {
                "GetCommonBatchTablesByID" => Header(),
                "GetBatchAnimal" => Single(Table("animals", [("ID", typeof(int))], [1])),
                _ => null,
            },
        };

        await new BatchRepository(db).RefreshAllTissuesAssignedAsync(42, 7, Ct);

        Assert.Equal(true, SingleEdit(db).Parameters["AllTissuesAssigned"]);
    }

    [Fact]
    public async Task RefreshAllTissuesAssignedAsync_NoAnimalsAndFlagAlreadyOff_WritesNothing()
    {
        var db = new FakeDb { OnQuery = c => c.Text == "GetCommonBatchTablesByID" ? Header() : null };

        await new BatchRepository(db).RefreshAllTissuesAssignedAsync(42, 7, Ct);

        Assert.DoesNotContain(db.Commands, c => c.Text == "EditBatch");
    }

    [Fact]
    public async Task CompleteBlockAssignmentAsync_SetsInProgressBlockedAndFlag()
    {
        var db = new FakeDb { OnQuery = c => c.Text == "GetCommonBatchTablesByID" ? Header() : null };

        await new BatchRepository(db).CompleteBlockAssignmentAsync(42, true, 7, Ct);

        var edit = SingleEdit(db);
        Assert.Equal(6, edit.Parameters["BatchStatus"]);
        Assert.Equal(true, edit.Parameters["IsBlocked"]);
        Assert.Equal(true, edit.Parameters["AllTissuesAssigned"]);
    }

    [Fact]
    public async Task CompleteBlockAssignmentAsync_BatchNotFound_WritesNothing()
    {
        var db = new FakeDb { OnQuery = _ => NoBatch() };

        await new BatchRepository(db).CompleteBlockAssignmentAsync(42, true, 7, Ct);

        Assert.DoesNotContain(db.Commands, c => c.Text == "EditBatch");
    }

    private static DataSet PremiumCharges() =>
        Single(Table("charges", [("ID", typeof(int)), ("Description", typeof(string))], [1, "TC 1401"], [2, ""]));

    [Fact]
    public async Task GetTestPremiumChargeCountsAsync_QueriesEachSelectedTestTypePerCharge()
    {
        var db = new FakeDb
        {
            OnQuery = c => c.Text switch
            {
                "GetluPremiumCharges" => PremiumCharges(),
                "GetTestHistologyCounts" => Single(Table("counts", [("Description", typeof(string)), ("TC 1401", typeof(int))], ["Proj A", 5])),
                _ => null,
            },
        };

        var result = await new BatchRepository(db).GetTestPremiumChargeCountsAsync(
            "Proj A", 1, new TestCodeSelection(["H1", "H2"], [], []), ct: Ct);

        var row = Assert.Single(result);
        Assert.Equal("Proj A", row.ProjectDescription);
        Assert.Equal("TC 1401", row.PremiumCode);
        Assert.Equal(5, row.Count);
        var query = Assert.Single(db.Commands, c => c.Text == "GetTestHistologyCounts");
        Assert.Equal("H1','H2", query.Parameters["Tests"]);
        Assert.Equal("TC 1401", query.Parameters["TestCode"]);
        Assert.Equal("Proj A", query.Parameters["ProjectContractCode"]);
        Assert.Equal(true, query.Parameters["BatchType"]);
        Assert.DoesNotContain(db.Commands, c => c.Text == "GetTestAntibodiesCounts");
    }

    [Fact]
    public async Task GetTestPremiumChargeCountsAsync_NoProjectAndBatchTypeZero_SendsEmptyProjectAndFalse()
    {
        var db = new FakeDb { OnQuery = c => c.Text == "GetluPremiumCharges" ? PremiumCharges() : null };

        await new BatchRepository(db).GetTestPremiumChargeCountsAsync(null, 0, new TestCodeSelection([], ["A1"], ["S1"]), ct: Ct);

        var query = Assert.Single(db.Commands, c => c.Text == "GetTestAntibodiesCounts");
        Assert.Equal(string.Empty, query.Parameters["ProjectContractCode"]);
        Assert.Equal(false, query.Parameters["BatchType"]);
        Assert.Contains(db.Commands, c => c.Text == "GetTestStainsCounts");
    }

    [Fact]
    public async Task GetTestPremiumChargeBatchesAsync_ReturnsBatchIdsPerCharge()
    {
        var db = new FakeDb
        {
            OnQuery = c => c.Text switch
            {
                "GetluPremiumCharges" => PremiumCharges(),
                "GetTestStainsBatch" => Single(Table("ids", [("ID", typeof(int))], [11], [12])),
                _ => null,
            },
        };

        var result = await new BatchRepository(db).GetTestPremiumChargeBatchesAsync(
            "P", 1, new TestCodeSelection([], [], ["S1"]), new DateTime(2026, 1, 1), new DateTime(2026, 2, 1), Ct);

        Assert.Equal([11, 12], result.Select(r => r.BatchID));
        Assert.All(result, r => Assert.Equal("TC 1401", r.PremiumCode));
        var query = Assert.Single(db.Commands, c => c.Text == "GetTestStainsBatch");
        Assert.Equal(new DateTime(2026, 1, 1), query.Parameters["SubmittedDateFrom"]);
        Assert.Equal(new DateTime(2026, 2, 1), query.Parameters["SubmittedDateTo"]);
    }
}
