using Histo.Administration.Models;

namespace Histo.Web.Pages.Admin;

/// <summary>
/// Common bound fields/lookups shared by the Add/Edit user forms, so the identical
/// Name/Email/Group/Area/Active markup can live in one partial (<c>_UserFormFields.cshtml</c>)
/// typed against this interface instead of being duplicated per page.
/// </summary>
public interface IUserFormFields
{
    string Name { get; set; }
    string Email { get; set; }
    int GroupCode { get; set; }
    int AreaCode { get; set; }
    bool Active { get; set; }
    Dictionary<string, string> Errors { get; }
    IReadOnlyList<LookupItem> Groups { get; }
    IReadOnlyList<LookupItem> Areas { get; }
}
