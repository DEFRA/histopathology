namespace Histo.Web.Pages.Admin;

/// <summary>Page-level members shared by the Add/Edit user pages, consumed by <c>_UserFormShell.cshtml</c>.</summary>
public interface IUserForm : IUserFormFields
{
    string? ReturnUrl { get; set; }
    string? SortColumn { get; set; }
    bool SortDesc { get; set; }
    bool ShowDeactivated { get; set; }
    string? SafeReturnUrl { get; }
    string? SaveError { get; }
}