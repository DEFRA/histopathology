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
