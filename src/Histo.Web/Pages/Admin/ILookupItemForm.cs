using Histo.Administration.Models;

namespace Histo.Web.Pages.Admin;

/// <summary>Bound fields/lookups shared by the Add/Edit pick-list item forms, consumed by <c>_LookupItemFields.cshtml</c>.</summary>
public interface ILookupItemForm
{
    string Code { get; set; }
    string Description { get; set; }
    bool Active { get; set; }
    string? Area { get; set; }
    bool ShowAreaColumn { get; }
    bool TableHasCodes { get; }
    Dictionary<string, string> Errors { get; }
    IReadOnlyList<LookupItem> UserAreas { get; }
}