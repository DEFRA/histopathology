using Dapper;
using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Infrastructure;

namespace Histo.Administration.Repositories;

/// <summary>
/// Dapper implementation of <see cref="IUserRepository"/>.
///
/// Every method opens a fresh connection from <see cref="IDbConnectionFactory"/>
/// and disposes it after the query completes — matching the per-call open/close
/// pattern of the legacy <c>DataAccess</c> static methods.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _db;

    public UserRepository(IDbConnectionFactory db) => _db = db;

    /// <inheritdoc/>
    public async Task<User?> GetUserByNtLoginAsync(string ntLogin, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var result = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "GetUserByNTLogin",
            new { NTLogin = ntLogin },
            commandType: System.Data.CommandType.StoredProcedure);

        if (result is null) return null;

        // The legacy SP returns Active as a bit. If the account is inactive, return null
        // to mirror the false return value of the legacy GetUserByNTLogin.
        bool active = Convert.ToBoolean(result.Active);
        if (!active) return null;

        return new User
        {
            UserID    = (int)result.ID,
            Name      = (string)(result.Name ?? string.Empty),
            GroupCode = Convert.ToInt32(result.UserGroup),
            GroupName = (string)(result.GroupName ?? string.Empty),
            Email     = (string)(result.Email ?? string.Empty),
            AreaCode  = Convert.ToInt32(result.UserArea),
            AreaName  = (string)(result.AreaName ?? string.Empty),
            Active    = active,
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<User>(
            "GetUsers",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<User>> GetUsersByAreaAsync(string userArea, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<User>(
            "GetUsersByUserArea",
            new { UserArea = userArea },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }
}
