namespace Histo.Administration.Models;

/// <summary>
/// A row in the Species pick list (<c>tlkpSpecies</c>).
///
/// Not the generic <see cref="LookupItem"/> shape — Species has its own column names
/// (<c>SpeciesID</c>/<c>Species</c>/<c>CommonName</c>) and no <c>IsActive</c> column, so it's
/// maintained via its own dedicated stored procedures (<c>GettlkpSpecies</c>/<c>AddtlkpSpecies</c>/
/// <c>EdittlkpSpecies</c>/<c>DeletetlkpSpecies</c>) rather than the generic editable-lookup route.
/// </summary>
public sealed class SpeciesItem
{
    public int SpeciesID { get; init; }
    public string Species { get; init; } = string.Empty;
    public string? CommonName { get; init; }
}
