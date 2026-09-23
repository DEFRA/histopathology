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
    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var result = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "GetUserByEmail",
            new { Email = email },
            commandType: System.Data.CommandType.StoredProcedure);

        if (result is null) return null;

        var d = (IDictionary<string, object>)result;
        bool active = d.TryGetValue("Active", out var a) && Convert.ToBoolean(a);
        if (!active) return null;

        return new User
        {
            UserID    = d.TryGetValue("ID",        out var id)  ? ToIntSafe(id)                              : 0,
            Name      = d.TryGetValue("Name",      out var n)   ? (string?)n ?? string.Empty                 : string.Empty,
            GroupCode = d.TryGetValue("UserGroup", out var gc)  ? ToIntSafe(gc)                              : 0,
            GroupName = d.TryGetValue("GroupName", out var gn)  ? (string?)gn ?? string.Empty                : string.Empty,
            Email     = d.TryGetValue("Email",     out var em)  ? (string?)em ?? string.Empty                : string.Empty,
            AreaCode  = d.TryGetValue("UserArea",  out var uc)  ? ToIntSafe(uc)                              : 0,
            AreaName  = d.TryGetValue("AreaName",  out var an)  ? (string?)an ?? string.Empty                : string.Empty,
            Active    = active,
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            "GetUsers",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.Select(MapUser).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<User>> GetUsersByAreaAsync(string userArea, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            "GetUsersByUserArea",
            new { UserArea = userArea },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.Select(MapUser).ToList();
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Converts a dictionary value to int without throwing.
    /// Handles the case where the SP returns a numeric column as a <c>string</c> (e.g. UserGroup,
    /// UserArea in <c>GetUsers</c>) — <c>Convert.ToInt32("")</c> throws <c>FormatException</c>;
    /// <c>int.TryParse</c> safely returns 0 for empty or non-numeric strings.
    /// </summary>
    private static int ToIntSafe(object? val)
    {
        if (val is null) return 0;
        if (val is int i) return i;
        if (val is long l) return (int)l;
        if (val is short s) return s;
        if (val is decimal dec) return (int)dec;
        return int.TryParse(Convert.ToString(val), out var n) ? n : 0;
    }

    /// <summary>
    /// Maps a row from the <c>GetUsers</c> / <c>GetUsersByUserArea</c> stored procedures.
    /// <para>
    /// Uses a case-insensitive <see cref="Dictionary{TKey,TValue}"/> copy of the Dapper
    /// <c>ExpandoObject</c> row. Dapper returns column names exactly as the SQL result set
    /// defines them; the backing <c>IDictionary&lt;string, object&gt;</c> of an ExpandoObject
    /// is case-sensitive, so a column named <c>groupname</c> would not be found by
    /// <c>TryGetValue("GroupName")</c>. The case-insensitive copy removes this fragility.
    /// Uses <see cref="ToIntSafe"/> for numeric columns because the SP may return
    /// <c>UserGroup</c> and <c>UserArea</c> as string columns;
    /// <c>Convert.ToInt32("")</c> throws <c>FormatException</c> for empty strings.
    /// </para>
    /// </summary>
    private static User MapUser(dynamic row)
    {
        // Build a case-insensitive dictionary so column-name casing differences
        // between SQL Server SP aliases and TryGetValue key strings do not silently
        // produce empty strings / zero values.
        var d = new Dictionary<string, object?>(
            ((IDictionary<string, object>)row).ToDictionary(p => p.Key, p => (object?)p.Value),
            StringComparer.OrdinalIgnoreCase);

        return new User
        {
            UserID    = d.TryGetValue("ID",        out var id)   ? ToIntSafe(id)                 : 0,
            Name      = d.TryGetValue("Name",      out var nm)   ? Convert.ToString(nm)  ?? ""   : "",
            NtLogin   = d.TryGetValue("NTLogin",   out var ntl)  ? Convert.ToString(ntl) ?? ""   : "",
            GroupCode = d.TryGetValue("UserGroup",  out var grp)  ? ToIntSafe(grp)               : 0,
            GroupName = d.TryGetValue("GroupName",  out var gn)   ? Convert.ToString(gn)  ?? ""  : "",
            AreaCode  = d.TryGetValue("UserArea",   out var area) ? ToIntSafe(area)              : 0,
            AreaName  = d.TryGetValue("AreaName",   out var an)   ? Convert.ToString(an)  ?? ""  : "",
            Email     = d.TryGetValue("Email",      out var em)   ? Convert.ToString(em)  ?? ""  : "",
            Active    = d.TryGetValue("Active",     out var act)  ? Convert.ToBoolean(act)        : false,
        };
    }

    /// <inheritdoc/>
    public async Task CreateUserAsync(User user, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "AddUser",
            new
            {
                NTLogin   = user.NtLogin,
                Name      = user.Name,
                Email     = user.Email,
                UserGroup = user.GroupCode,
                UserArea  = user.AreaCode,
                Active    = user.Active,
            },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task UpdateUserAsync(User user, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "EditUser",
            new
            {
                ID        = user.UserID,   // record being edited
                NTLogin   = user.NtLogin,
                Name      = user.Name,
                Email     = user.Email,
                UserGroup = user.GroupCode,
                UserArea  = user.AreaCode,
                Active    = user.Active,
                UserID    = userId,        // session editor (audit — matches legacy @UserID param)
            },
            commandType: System.Data.CommandType.StoredProcedure);
    }
}
