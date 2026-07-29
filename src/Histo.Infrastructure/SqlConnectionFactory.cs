using Microsoft.Data.SqlClient;

namespace Histo.Infrastructure;

/// <inheritdoc cref="IDbConnectionFactory"/>
public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        _connectionString = connectionString;
    }

    /// <inheritdoc/>
    public SqlConnection CreateConnection() => new(_connectionString);
}
