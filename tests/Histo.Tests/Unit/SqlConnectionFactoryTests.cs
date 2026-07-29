using Histo.Infrastructure;
using Microsoft.Data.SqlClient;

namespace Histo.Tests.Unit;

/// <summary>
/// Baseline unit tests for <see cref="SqlConnectionFactory"/>.
///
/// These tests verify that the factory creates correct <see cref="SqlConnection"/>
/// instances without requiring an actual database connection — constructing a
/// SqlConnection does not open it.
///
/// Legacy source: DataAccessLib/clsDataAccess.vb — lazy connection string initialisation.
/// </summary>
public class SqlConnectionFactoryTests
{
    private const string TestConnectionString =
        "Server=localhost;Database=Test;Trusted_Connection=True;TrustServerCertificate=True;";

    [Fact]
    public void CreateConnection_ReturnsSqlConnection()
    {
        var factory = new SqlConnectionFactory(TestConnectionString);
        using var conn = factory.CreateConnection();
        Assert.NotNull(conn);
        Assert.IsType<SqlConnection>(conn);
    }

    [Fact]
    public void CreateConnection_EachCallReturnsNewInstance()
    {
        var factory = new SqlConnectionFactory(TestConnectionString);
        using var conn1 = factory.CreateConnection();
        using var conn2 = factory.CreateConnection();
        Assert.NotSame(conn1, conn2);
    }

    [Fact]
    public void CreateConnection_ConnectionStringIsPreserved()
    {
        var factory = new SqlConnectionFactory(TestConnectionString);
        using var conn = factory.CreateConnection();
        Assert.Equal(TestConnectionString, conn.ConnectionString);
    }

    [Fact]
    public void Constructor_NullConnectionString_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlConnectionFactory(null!));
    }
}
