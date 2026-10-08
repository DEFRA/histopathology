namespace Histo.Web.Pages.Admin;

/// <summary>
/// Common bound fields shared by the Add/Edit species forms, so the identical
/// Species/CommonName markup can live in one partial (<c>_SpeciesItemFormFields.cshtml</c>)
/// typed against this interface instead of being duplicated per page.
/// </summary>
public interface ISpeciesItemFormFields
{
    string Species { get; set; }
    string? CommonName { get; set; }
    Dictionary<string, string> Errors { get; }
}
