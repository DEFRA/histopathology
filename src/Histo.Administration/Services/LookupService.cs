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
public sealed class LookupService
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
}
