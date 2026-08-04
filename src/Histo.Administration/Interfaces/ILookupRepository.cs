using Histo.Administration.Models;

namespace Histo.Administration.Interfaces;

/// <summary>
/// Data access contract for pick-list/lookup tables.
///
/// Legacy source: LookupData.vb — all public methods translated to async Dapper pattern.
/// </summary>
public interface ILookupRepository
{
    /// <summary>
    /// Returns all rows from a pick-list table identified by <paramref name="tableId"/>.
    /// When <paramref name="includeInactive"/> is <see langword="true"/>, appends "All"
    /// to the select stored procedure name (legacy convention).
    /// </summary>
    Task<IReadOnlyList<LookupItem>> GetLookupDataAsync(
        int tableId,
        bool includeInactive = false,
        CancellationToken ct = default);

    /// <summary>
    /// Returns lookup rows for a pick-list table filtered by user area.
    /// Maps to the area-scoped select procedure resolved via <c>GetEditableLookupProcs</c>.
    /// </summary>
    Task<IReadOnlyList<LookupItem>> GetUserAreaDataAsync(
        int tableId,
        string userArea,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the list of editable lookup table descriptors.
    /// Maps to <c>GetEditableLookups</c>.
    /// </summary>
    Task<IReadOnlyList<EditableLookup>> ListEditableLookupsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns contacts for a submitted area. Maps to <c>GetContactsArea</c>.
    /// </summary>
    Task<IReadOnlyList<LookupItem>> GetContactsByAreaAsync(string area, CancellationToken ct = default);

    /// <summary>
    /// Returns projects for a submitted area. Maps to <c>GetProjectsArea</c>.
    /// </summary>
    Task<IReadOnlyList<LookupItem>> GetProjectsByAreaAsync(string area, CancellationToken ct = default);

    /// <summary>
    /// Returns the list of user groups for the User Maintenance group drop-down.
    /// Maps to <c>GetluUserGroup</c> (legacy source: <c>LookupData.GetUserGroups</c>).
    /// </summary>
    Task<IReadOnlyList<LookupItem>> GetUserGroupsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the list of user areas for the User Maintenance area drop-down.
    /// Maps to <c>GetluUserArea</c> (legacy source: <c>LookupData.GetUserAreas</c>).
    /// </summary>
    Task<IReadOnlyList<LookupItem>> GetUserAreasAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the list of legacy imported ICC_Sub table names for the ViewImportedData
    /// table drop-down. Maps to <c>GetluImportedTables</c>
    /// (legacy source: <c>LookupData.GetImportedtables</c>).
    /// </summary>
    Task<IReadOnlyList<LookupItem>> GetImportedTablesAsync(CancellationToken ct = default);

    /// <summary>
    /// Inserts a new pick-list row into the table identified by <paramref name="tableId"/>,
    /// using the Insert stored procedure resolved via <c>GetEditableLookupProcs</c>.
    /// Legacy source: <c>LookupData.SaveLookupData</c> insert path
    /// (<c>PickListMaintenanceID.aspx</c> / <c>PickListUserArea.aspx</c> grid "Add new" row).
    /// </summary>
    Task CreateLookupItemAsync(int tableId, LookupItem item, int userId, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing pick-list row in the table identified by <paramref name="tableId"/>,
    /// using the Update stored procedure resolved via <c>GetEditableLookupProcs</c>.
    /// Legacy source: <c>LookupData.SaveLookupData</c> update path
    /// (<c>PickListMaintenanceID.aspx</c> / <c>PickListUserArea.aspx</c> grid "Edit" row).
    /// </summary>
    Task UpdateLookupItemAsync(int tableId, LookupItem item, int userId, CancellationToken ct = default);

    /// <summary>
    /// Returns all species from the species lookup table.
    /// Uses the dedicated <c>GetSpeciesLookup</c> stored procedure (not the generic
    /// editable-lookup route) because species uses a non-standard column shape
    /// (<c>SpeciesID</c> / <c>Species</c>) rather than the standard <c>ID</c>/<c>Name</c> shape.
    /// Legacy source: <c>LookupData.GetSpeciesLookup</c>.
    /// </summary>
    Task<IReadOnlyList<LookupItem>> GetSpeciesLookupAsync(CancellationToken ct = default);
}
