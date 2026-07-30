using Dapper;
using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Infrastructure;

namespace Histo.Administration.Repositories;

/// <summary>
/// Dapper implementation of <see cref="ILookupRepository"/>.
///
/// The legacy system resolves the correct select/update/insert/delete stored
/// procedure names dynamically by calling <c>GetEditableLookupProcs</c> with the
/// table ID. This implementation mirrors that lookup on every call — no caching
/// at the repository layer (add caching in the service layer if required).
/// </summary>
public sealed class LookupRepository : ILookupRepository
{
    private readonly IDbConnectionFactory _db;

    public LookupRepository(IDbConnectionFactory db) => _db = db;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LookupItem>> GetLookupDataAsync(
        int tableId,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        var procName = await ResolveSelectProcAsync(tableId);
        if (includeInactive) procName += "All";

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<LookupItem>(
            procName,
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LookupItem>> GetUserAreaDataAsync(
        int tableId,
        string userArea,
        CancellationToken ct = default)
    {
        var procName = await ResolveSelectProcAsync(tableId);

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<LookupItem>(
            procName,
            new { UserArea = userArea },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EditableLookup>> ListEditableLookupsAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<EditableLookup>(
            "GetEditableLookups",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LookupItem>> GetContactsByAreaAsync(string area, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<LookupItem>(
            "GetContactsArea",
            new { Area = area },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LookupItem>> GetProjectsByAreaAsync(string area, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<LookupItem>(
            "GetProjectsArea",
            new { Area = area },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Calls <c>GetEditableLookupProcs</c> to resolve the select stored procedure
    /// name for a given table ID — mirroring the legacy <c>GetSelectProc()</c> helper.
    /// </summary>
    private async Task<string> ResolveSelectProcAsync(int tableId)
    {
        using var conn = _db.CreateConnection();
        var result = await conn.QuerySingleAsync<dynamic>(
            "GetEditableLookupProcs",
            new { ID = tableId },
            commandType: System.Data.CommandType.StoredProcedure);

        var procName = (string?)result.SelectStoredProcedure;
        if (string.IsNullOrWhiteSpace(procName))
            throw new InvalidOperationException(
                $"The look-up table Select procedure could not be found for table ID {tableId}.");

        return procName;
    }
}
