using Dapper;
using Histo.AuditLog;
using Histo.AuditLog.Models;
using Histo.Tests.Support;

namespace Histo.Tests.Unit;

public class AuditLogDapperSetupTests
{
    [Fact]
    public async Task RegisterTypeMaps_MapsTheDateTimeAliasToChangedAt_AndIgnoresUnknownColumns()
    {
        AuditLogDapperSetup.RegisterTypeMaps();
        var when = new DateTime(2026, 3, 4, 5, 6, 7);
        var table = new System.Data.DataTable();
        table.Columns.Add("DateTime", typeof(DateTime));
        table.Columns.Add("UserName", typeof(string));
        table.Columns.Add("NotAProperty", typeof(string));
        table.Rows.Add(when, "Ada", "ignored");
        var db = new FakeDb { OnQuery = _ => new System.Data.DataSet { Tables = { table } } };

        using var conn = db.CreateConnection();
        var rows = (await conn.QueryAsync<AuditLogEntry>("GetAuditLog", commandType: System.Data.CommandType.StoredProcedure)).ToList();

        var row = Assert.Single(rows);
        Assert.Equal(when, row.ChangedAt);
        Assert.Equal("Ada", row.UserName);
    }
}
