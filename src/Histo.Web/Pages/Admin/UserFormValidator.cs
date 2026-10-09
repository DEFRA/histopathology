namespace Histo.Web.Pages.Admin;

/// <summary>
/// Shared field-validation rules for the Add/Edit user forms — identical on both pages
/// (Name/Email required + max length, Group/Area must be selected), so both
/// <see cref="AddUserModel"/> and <see cref="EditUserModel"/> delegate to this instead of
/// duplicating the rule set.
/// </summary>
public static class UserFormValidator
{
    public static void Validate(Dictionary<string, string> errors, string name, string email, int groupCode, int areaCode)
    {
        if (string.IsNullOrWhiteSpace(name)) errors["Name"] = "Enter the user's name.";
        else if (name.Length > 35) errors["Name"] = "Name must be 35 characters or less.";

        if (string.IsNullOrWhiteSpace(email)) errors["Email"] = "Enter the user's email.";
        else if (email.Length > 60) errors["Email"] = "Email must be 60 characters or less.";

        if (groupCode <= 0) errors["GroupCode"] = "Select a user group.";
        if (areaCode <= 0) errors["AreaCode"] = "Select a user area.";
    }
}
