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
        var rows = await conn.QueryAsync<dynamic>(
            procName,
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.Select(MapDescriptionIsActive).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LookupItem>> GetUserAreaDataAsync(
        int tableId,
        string userArea,
        CancellationToken ct = default)
    {
        var procName = await ResolveSelectProcAsync(tableId);

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            procName,
            new { UserArea = userArea },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.Select(MapDescriptionIsActive).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EditableLookup>> ListEditableLookupsAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            "GetEditableLookups",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.Select(MapEditableLookup).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LookupItem>> GetContactsByAreaAsync(string area, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            "GetContactsArea",
            new { Area = area },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.Select(MapDescriptionIsActive).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LookupItem>> GetProjectsByAreaAsync(string area, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            "GetProjectsArea",
            new { Area = area },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.Select(MapDescriptionIsActive).ToList();
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
    public async Task<IReadOnlyList<LookupItem>> GetSpeciesLookupAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            "GetluSpecies",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.Select(r => new LookupItem
        {
            ID   = (int)r.SpeciesID,
            Name = (string)(r.Species ?? string.Empty),
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LookupItem>> GetHistologyTypesAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            "GetluHistology",
            commandType: System.Data.CommandType.StoredProcedure);
        // Use MapLookupItem so the Code column (e.g. "3", "4", "5", "6") is preserved
        // in LookupItem.Code for downstream batch-test-selection storage.
        return rows.Select(MapLookupItem).ToList();
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
        var rows = await conn.QueryAsync<dynamic>(
            "GetluImportedTables",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.Select(MapDescriptionIsActive).ToList();
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
        // Legacy BuildParamListCommon/BuildParamListID (LookupData.vb) never include
        // UserID in insert parameters — only in update parameters. Pass only the columns
        // the SP expects: Description, IsActive, and Area (for area-scoped tables 18/19).
        var parameters = new DynamicParameters();
        parameters.Add("Description", item.Name);
        parameters.Add("IsActive", item.Active);
        // Code-keyed tables (e.g. Archive Location 16, QC Code 14, Remedial Action 15)
        // have a required @Code parameter on their insert SPs. Pass it when present.
        if (!string.IsNullOrEmpty(item.Code))
            parameters.Add("Code", item.Code);
        if (!string.IsNullOrEmpty(item.Area))
            parameters.Add("Area", item.Area);

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            procs.InsertProc,
            parameters,
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task UpdateLookupItemAsync(int tableId, LookupItem item, int userId, string? originalCode = null, CancellationToken ct = default)
    {
        var procs = await ResolveLookupProcsAsync(tableId);
        if (string.IsNullOrWhiteSpace(procs.UpdateProc))
            throw new InvalidOperationException(
                $"The look-up table Update procedure could not be found for table ID {tableId}.");

        // Code-keyed tables (Archive Location 16, QC Code 14, Remedial Action 15, etc.):
        // legacy BuildParamListCommon passes @Original_Code (row identifier), @Code (new value),
        // @Description, @IsActive, @UserID — there is no @ID column on these tables.
        // ID-keyed tables (Contacts 18, Projects 19 and similar):
        // legacy BuildParamListID passes @ID, @Description, @IsActive, @UserID.
        var parameters = new DynamicParameters();
        if (!string.IsNullOrEmpty(originalCode))
        {
            parameters.Add("Original_Code", originalCode);
            parameters.Add("Code",          item.Code);
        }
        else
        {
            parameters.Add("ID", item.ID);
        }
        parameters.Add("Description", item.Name);
        parameters.Add("IsActive",    item.Active);
        parameters.Add("UserID",      userId);

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            procs.UpdateProc,
            parameters,
            commandType: System.Data.CommandType.StoredProcedure);
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Maps a "Code"/"Description" row (the shape returned by <c>GetluUserGroup</c>
    /// and <c>GetluUserArea</c>) into a <see cref="LookupItem"/>.
    /// </summary>
    private static LookupItem MapCodeDescription(dynamic row)
    {
        var d = (IDictionary<string, object>)row;
        return new LookupItem
        {
            ID     = d.TryGetValue("Code",        out var code) ? ToIntSafe(code)               : 0,
            Name   = d.TryGetValue("Description", out var desc) ? Convert.ToString(desc) ?? ""   : "",
            Active = true,
        };
    }

    /// <summary>
    /// Maps a standard pick-list row where the SP returns <c>ID</c>, <c>Description</c>,
    /// and optionally <c>IsActive</c>. Uses <see cref="IDictionary{TKey,TValue}"/> + TryGetValue
    /// to avoid <c>RuntimeBinderException</c> when <c>IsActive</c> is absent from a particular
    /// stored procedure result set — accessing a missing member on a Dapper <c>ExpandoObject</c>
    /// via dynamic dispatch throws at runtime and is silently swallowed by the service-layer catch,
    /// resulting in an empty list with no visible error.
    /// </summary>
    private static LookupItem MapDescriptionIsActive(dynamic row)
    {
        var d = (IDictionary<string, object>)row;
        return new LookupItem
        {
            ID     = d.TryGetValue("ID",          out var id)     ? ToIntSafe(id)                : 0,
            Name   = d.TryGetValue("Description", out var desc)   ? Convert.ToString(desc) ?? "" : "",
            Active = d.TryGetValue("IsActive",     out var active) ? Convert.ToBoolean(active)    : true,
            Code   = d.TryGetValue("Code",         out var code)   ? Convert.ToString(code)       : null,
        };
    }

    /// <summary>
    /// Maps a row from <c>GetEditableLookups</c> where the SP returns <c>Description</c>
    /// (the user-friendly pick-list name) but the model property is <c>TableName</c>.
    /// Uses the dictionary approach for the same RuntimeBinderException safety reason.
    /// </summary>
    private static EditableLookup MapEditableLookup(dynamic row)
    {
        var d = (IDictionary<string, object>)row;
        return new EditableLookup
        {
            ID                    = d.TryGetValue("ID",                    out var id)    ? ToIntSafe(id)                : 0,
            TableName             = d.TryGetValue("Description",           out var desc)  ? Convert.ToString(desc) ?? "" : "",
            SelectStoredProcedure = d.TryGetValue("SelectStoredProcedure", out var sel)   ? Convert.ToString(sel)  ?? "" : "",
            UpdateStoredProcedure = d.TryGetValue("UpdateStoredProcedure", out var upd)   ? Convert.ToString(upd)  ?? "" : "",
            InsertStoredProcedure = d.TryGetValue("InsertStoredProcedure", out var ins)   ? Convert.ToString(ins)  ?? "" : "",
            DeleteStoredProcedure = d.TryGetValue("DeleteStoredProcedure", out var del)   ? Convert.ToString(del)  ?? "" : "",
        };
    }

    /// <summary>
    /// Converts a dictionary value to int without throwing.
    /// Handles the case where the SP returns a numeric column as a <c>string</c> —
    /// <c>Convert.ToInt32("")</c> throws <c>FormatException</c> for empty strings;
    /// <c>int.TryParse</c> safely returns 0.
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

    // -----------------------------------------------------------------------
    // Shared dynamic mapper
    // -----------------------------------------------------------------------

    /// <summary>
    /// Maps a dynamic Dapper row to a <see cref="LookupItem"/>.
    ///
    /// Legacy stored procedures use inconsistent column naming: some return
    /// <c>Name</c>, others return <c>Description</c> (e.g. Contacts/Projects
    /// tables 18/19, Fixation table 10). Similarly, the active flag may be
    /// <c>Active</c> or <c>IsActive</c>. This method handles both conventions
    /// so every lookup table is correctly populated regardless of its SP shape.
    /// </summary>
    private static LookupItem MapLookupItem(dynamic r)
    {
        var d = (IDictionary<string, object>)r;

        var id = d.TryGetValue("ID", out var idVal) ? Convert.ToInt32(idVal) : 0;

        string name;
        if (d.TryGetValue("Name", out var nameVal) && nameVal is not null)
            name = nameVal.ToString()!;
        else if (d.TryGetValue("Description", out var descVal) && descVal is not null)
            name = descVal.ToString()!;
        else
            name = string.Empty;

        bool active;
        if (d.TryGetValue("IsActive", out var isActiveVal) && isActiveVal is not null)
            active = Convert.ToBoolean(isActiveVal);
        else if (d.TryGetValue("Active", out var activeVal) && activeVal is not null)
            active = Convert.ToBoolean(activeVal);
        else
            active = true;

        string? area = d.TryGetValue("Area", out var areaVal) ? areaVal?.ToString() : null;

        // Capture the raw Code column for "Code-keyed" pick-list tables (e.g. QC Code,
        // Remedial Action, Archive Location) whose SPs expose a Code column instead of ID.
        string? code = d.TryGetValue("Code", out var codeVal) ? codeVal?.ToString() : null;

        return new LookupItem { ID = id, Name = name, Active = active, Area = area, Code = code };
    }

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
