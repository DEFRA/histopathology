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

    /// <summary>
    /// Returns all histology types from <c>GetluHistology</c>.
    /// Each item's <see cref="LookupItem.Code"/> holds the string code used when storing
    /// batch-level test-type selections (e.g. "3" = Special Stain, "4" = IHC-PrP,
    /// "5" = H&amp;E BSE, "6" = IHC-Other).
    /// Callers should filter by <see cref="Histo.Submissions.Models.BatchTypeConstants"/>:
    /// hide code "6" (IHC-Other) for TSE; hide codes "4" and "5" for NonTSE.
    /// </summary>
    Task<IReadOnlyList<LookupItem>> GetHistologyTypesAsync(CancellationToken ct = default);

    /// <summary>Creates a new pick-list row. Returns <see langword="false"/> on failure.</summary>
    Task<bool> CreateLookupItemAsync(int tableId, LookupItem item, int userId, CancellationToken ct = default);

    /// <summary>Updates an existing pick-list row. Returns <see langword="false"/> on failure.</summary>
    /// <param name="originalCode">For Code-keyed tables: the original code identifying the row. Null for ID-keyed tables.</param>
    Task<bool> UpdateLookupItemAsync(int tableId, LookupItem item, int userId, string? originalCode = null, CancellationToken ct = default);
}
