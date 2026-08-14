using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Infrastructure;

namespace Histo.Administration.Services;

/// <summary>
/// Application service for pick-list/lookup data.
///
/// Replaces the legacy <c>LookupData</c> VB class. Thin orchestration over
/// <see cref="ILookupRepository"/> with structured error logging.
/// </summary>
public sealed class LookupService : ILookupService
{
    private readonly ILookupRepository _lookups;
    private readonly IAppLogger _logger;

    public LookupService(ILookupRepository lookups, IAppLogger logger)
    {
        _lookups = lookups;
        _logger  = logger;
    }

    /// <summary>
    /// Returns all rows from a pick-list table identified by its numeric ID.
    /// Pass <paramref name="includeInactive"/> = <see langword="true"/> to include
    /// soft-deleted entries (calls the "…All" variant SP).
    /// </summary>
    public async Task<IReadOnlyList<LookupItem>> GetLookupDataAsync(
        int tableId,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        try
        {
            return await _lookups.GetLookupDataAsync(tableId, includeInactive, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get lookup data for table ID {TableId}.", ex, tableId);
            return [];
        }
    }

    /// <summary>Returns pick-list rows scoped to a specific user area.</summary>
    public async Task<IReadOnlyList<LookupItem>> GetUserAreaDataAsync(
        int tableId,
        string userArea,
        CancellationToken ct = default)
    {
        try
        {
            return await _lookups.GetUserAreaDataAsync(tableId, userArea, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get user area data for table ID {TableId}.", ex, tableId);
            return [];
        }
    }

    /// <summary>Returns all editable pick-list table descriptors.</summary>
    public async Task<IReadOnlyList<EditableLookup>> ListEditableLookupsAsync(CancellationToken ct = default)
    {
        try
        {
            return await _lookups.ListEditableLookupsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to list editable lookups.", ex);
            return [];
        }
    }

    /// <summary>Returns the user group pick-list for the User Maintenance form.</summary>
    public async Task<IReadOnlyList<LookupItem>> GetUserGroupsAsync(CancellationToken ct = default)
    {
        try
        {
            return await _lookups.GetUserGroupsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get user groups.", ex);
            return [];
        }
    }

    /// <summary>Returns the user area pick-list for the User Maintenance form.</summary>
    public async Task<IReadOnlyList<LookupItem>> GetUserAreasAsync(CancellationToken ct = default)
    {
        try
        {
            return await _lookups.GetUserAreasAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get user areas.", ex);
            return [];
        }
    }

    /// <summary>Returns the legacy imported ICC_Sub table pick-list for the ViewImportedData form.</summary>
    public async Task<IReadOnlyList<LookupItem>> GetImportedTablesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _lookups.GetImportedTablesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get imported tables.", ex);
            return [];
        }
    }

    /// <summary>
    /// Returns all species from the dedicated <c>GetSpeciesLookup</c> stored procedure.
    /// Used to populate the Species drop-down on ViewSubmissions and SearchSubmissions.
    /// </summary>
    public async Task<IReadOnlyList<LookupItem>> GetSpeciesLookupAsync(CancellationToken ct = default)
    {
        try
        {
            return await _lookups.GetSpeciesLookupAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get species lookup.", ex);
            return [];
        }
    }

    /// <summary>
    /// Creates a new pick-list row in the table identified by <paramref name="tableId"/>.
    /// Replaces the legacy <c>LookupData.SaveLookupData</c> insert path
    /// (<c>PickListMaintenanceID.aspx</c> / <c>PickListUserArea.aspx</c> grid "Add new" row).
    ///
    /// Returns <see langword="false"/> and logs the error if the create fails —
    /// callers should surface a generic save error to the user.
    /// </summary>
    public async Task<bool> CreateLookupItemAsync(int tableId, LookupItem item, int userId, CancellationToken ct = default)
    {
        try
        {
            await _lookups.CreateLookupItemAsync(tableId, item, userId, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to create lookup item for table ID {TableId}.", ex, tableId);
            return false;
        }
    }

    /// <summary>
    /// Updates an existing pick-list row in the table identified by <paramref name="tableId"/>.
    /// Replaces the legacy <c>LookupData.SaveLookupData</c> update path
    /// (<c>PickListMaintenanceID.aspx</c> / <c>PickListUserArea.aspx</c> grid "Edit" row).
    ///
    /// Returns <see langword="false"/> and logs the error if the update fails —
    /// callers should surface a generic save error to the user.
    /// </summary>
    public async Task<bool> UpdateLookupItemAsync(int tableId, LookupItem item, int userId, CancellationToken ct = default)
    {
        try
        {
            await _lookups.UpdateLookupItemAsync(tableId, item, userId, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to update lookup item {ItemId} for table ID {TableId}.", ex, item.ID, tableId);
            return false;
        }
    }
}
