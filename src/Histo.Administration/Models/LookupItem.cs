namespace Histo.Administration.Models;

/// <summary>
/// Generic pick-list item used across all lookup tables.
///
/// Legacy source: LookupData.vb — GetLookupData, GetUserAreaData, ListEditableLookups
/// all return DataTables with at minimum an ID and a display name.
/// </summary>
public sealed class LookupItem
{
    public int ID { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool Active { get; init; } = true;

    /// <summary>
    /// The raw pick-list code value returned by the stored procedure's <c>Code</c>
    /// column. Populated for "Code-keyed" pick-list tables (e.g. QC Code table 14,
    /// Remedial Action table 15, Archive Location table 16) whose SPs return a
    /// <c>Code</c> column rather than an integer <c>ID</c>. Null for "ID-keyed"
    /// tables such as Contacts (18) and Projects (19).
    ///
    /// UI dropdowns for these tables must bind their option value to this property
    /// rather than <see cref="ID"/> so that the correct code string is round-tripped
    /// to and from the database.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Optional user-area code. Only meaningful for the area-scoped pick-list tables
    /// (Contacts/Projects — table IDs 18/19), where the legacy <c>LookupData.SaveLookupData</c>
    /// insert path requires an Area value. Ignored (and not sent to the database) when creating
    /// rows in any other pick-list table.
    /// </summary>
    public string? Area { get; init; }
}

/// <summary>
/// Describes a pick-list table that the maintenance screen can edit.
///
/// Legacy source: LookupData.vb — ListEditableLookups (GetEditableLookups SP).
/// Columns: ID, TableName, SelectStoredProcedure, UpdateStoredProcedure,
///          InsertStoredProcedure, DeleteStoredProcedure.
/// </summary>
public sealed class EditableLookup
{
    public int ID { get; init; }
    public string TableName { get; init; } = string.Empty;
    public string SelectStoredProcedure { get; init; } = string.Empty;
    public string UpdateStoredProcedure { get; init; } = string.Empty;
    public string InsertStoredProcedure { get; init; } = string.Empty;
    public string DeleteStoredProcedure { get; init; } = string.Empty;
}
