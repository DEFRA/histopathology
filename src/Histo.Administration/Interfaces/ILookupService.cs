using Histo.Administration.Models;

namespace Histo.Administration.Interfaces;

/// <summary>
/// Public service contract for pick-list/lookup data — the module boundary exposed to Histo.Web.
/// Concrete implementation: <see cref="Histo.Administration.Services.LookupService"/>.
/// </summary>
public interface ILookupService
{
    /// <summary>Returns all rows from a pick-list table. Pass <paramref name="includeInactive"/> to include soft-deleted rows.</summary>
    Task<IReadOnlyList<LookupItem>> GetLookupDataAsync(int tableId, bool includeInactive = false, CancellationToken ct = default);

    /// <summary>Returns pick-list rows scoped to a specific user area.</summary>
    Task<IReadOnlyList<LookupItem>> GetUserAreaDataAsync(int tableId, string userArea, CancellationToken ct = default);

    /// <summary>Returns all editable pick-list table descriptors.</summary>
    Task<IReadOnlyList<EditableLookup>> ListEditableLookupsAsync(CancellationToken ct = default);

    /// <summary>Returns the user group pick-list.</summary>
    Task<IReadOnlyList<LookupItem>> GetUserGroupsAsync(CancellationToken ct = default);

    /// <summary>Returns the user area pick-list.</summary>
    Task<IReadOnlyList<LookupItem>> GetUserAreasAsync(CancellationToken ct = default);

    /// <summary>Returns the legacy imported ICC_Sub table pick-list.</summary>
    Task<IReadOnlyList<LookupItem>> GetImportedTablesAsync(CancellationToken ct = default);

    /// <summary>Returns all species from the dedicated species lookup stored procedure.</summary>
    Task<IReadOnlyList<LookupItem>> GetSpeciesLookupAsync(CancellationToken ct = default);

    /// <summary>Creates a new pick-list row. Returns <see langword="false"/> on failure.</summary>
    Task<bool> CreateLookupItemAsync(int tableId, LookupItem item, int userId, CancellationToken ct = default);

    /// <summary>Updates an existing pick-list row. Returns <see langword="false"/> on failure.</summary>
    Task<bool> UpdateLookupItemAsync(int tableId, LookupItem item, int userId, CancellationToken ct = default);
}
