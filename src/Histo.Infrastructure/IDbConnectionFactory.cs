using Microsoft.Data.SqlClient;

namespace Histo.Infrastructure;

/// <summary>
/// Factory that creates <see cref="SqlConnection"/> instances from a configured connection string.
///
/// Registered as a singleton in the DI container. Each call to <see cref="CreateConnection"/>
/// returns a new, unopened connection — the caller is responsible for opening and disposing it.
///
/// Replaces the lazy-initialised shared connection string field in the legacy
/// <c>DataAccessLib/clsDataAccess.vb</c>.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>Returns a new, unopened <see cref="SqlConnection"/>.</summary>
    SqlConnection CreateConnection();
}
