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

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LookupItem>> GetUserGroupsAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            "GetluUserGroup",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.Select(MapCodeDescription).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LookupItem>> GetUserAreasAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            "GetluUserArea",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.Select(MapCodeDescription).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LookupItem>> GetImportedTablesAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<LookupItem>(
            "GetluImportedTables",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task CreateLookupItemAsync(int tableId, LookupItem item, int userId, CancellationToken ct = default)
    {
        var procs = await ResolveLookupProcsAsync(tableId);
        if (string.IsNullOrWhiteSpace(procs.InsertProc))
            throw new InvalidOperationException(
                $"The look-up table Insert procedure could not be found for table ID {tableId}.");

        // Dynamic parameter set: most pick-list tables only need Description/IsActive/UserID,
        // but the area-scoped tables (Contacts/Projects — table IDs 18/19) also require an
        // Area value on insert (legacy LookupData.SaveLookupData / BuildParamListID). Building
        // this with DynamicParameters — rather than a fixed anonymous object — lets the Area
        // parameter be included only when the caller has supplied one, since supplying an
        // undeclared parameter to a stored procedure that doesn't expect it would fail at
        // the database layer for the common (non-area) pick-list tables.
        var parameters = new DynamicParameters();
        parameters.Add("Description", item.Name);
        parameters.Add("IsActive", item.Active);
        parameters.Add("UserID", userId);
        if (!string.IsNullOrEmpty(item.Area))
            parameters.Add("Area", item.Area);

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            procs.InsertProc,
            parameters,
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task UpdateLookupItemAsync(int tableId, LookupItem item, int userId, CancellationToken ct = default)
    {
        var procs = await ResolveLookupProcsAsync(tableId);
        if (string.IsNullOrWhiteSpace(procs.UpdateProc))
            throw new InvalidOperationException(
                $"The look-up table Update procedure could not be found for table ID {tableId}.");

        // Legacy BuildParamListID/BuildParamListCommon never update the Area column — it is
        // only ever set on insert — so the update parameter set is fixed for every table.
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            procs.UpdateProc,
            new
            {
                ID          = item.ID,
                Description = item.Name,
                IsActive    = item.Active,
                UserID      = userId,
            },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Maps a "Code"/"Description" row (the shape returned by <c>GetluUserGroup</c>
    /// and <c>GetluUserArea</c>) into a <see cref="LookupItem"/>.
    /// </summary>
    private static LookupItem MapCodeDescription(dynamic row) => new()
    {
        ID     = Convert.ToInt32(row.Code),
        Name   = (string)(row.Description ?? string.Empty),
        Active = true,
    };

    /// <summary>
    /// Calls <c>GetEditableLookupProcs</c> to resolve the select stored procedure
    /// name for a given table ID — mirroring the legacy <c>GetSelectProc()</c> helper.
    /// </summary>
    private async Task<string> ResolveSelectProcAsync(int tableId)
    {
        var procs = await ResolveLookupProcsAsync(tableId);
        if (string.IsNullOrWhiteSpace(procs.SelectProc))
            throw new InvalidOperationException(
                $"The look-up table Select procedure could not be found for table ID {tableId}.");

        return procs.SelectProc;
    }

    /// <summary>
    /// Calls <c>GetEditableLookupProcs</c> to resolve all four stored procedure names for a
    /// given table ID — mirroring the legacy <c>GetStoredProcedures()</c> helper, which resolves
    /// Select/Update/Insert/Delete in a single round trip rather than one call per procedure kind.
    /// </summary>
    private async Task<(string SelectProc, string UpdateProc, string InsertProc, string DeleteProc)> ResolveLookupProcsAsync(int tableId)
    {
        using var conn = _db.CreateConnection();
        var result = await conn.QuerySingleAsync<dynamic>(
            "GetEditableLookupProcs",
            new { ID = tableId },
            commandType: System.Data.CommandType.StoredProcedure);

        return (
            (string?)result.SelectStoredProcedure ?? string.Empty,
            (string?)result.UpdateStoredProcedure ?? string.Empty,
            (string?)result.InsertStoredProcedure ?? string.Empty,
            (string?)result.DeleteStoredProcedure ?? string.Empty);
    }
}
