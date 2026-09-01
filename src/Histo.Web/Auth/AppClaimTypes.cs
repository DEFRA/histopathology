namespace Histo.Web.Auth;

/// <summary>
/// Canonical claim type URIs used across the Histopathology application.
/// Set by <see cref="HistopathologyClaimsTransformation"/> after resolving the
/// user from <c>tblUser</c>, then read by <see cref="Pages.HistoPageModel"/> and
/// <see cref="Services.SessionService.PopulateFromClaims"/>.
/// </summary>
public static class AppClaimTypes
{
    private const string Base = "https://histopathology.apha.gov.uk/claims/";

    /// <summary>Application group name — Customer | Histopathology User | Maintenance.</summary>
    public const string GroupName  = Base + "group-name";

    /// <summary>Integer primary key from <c>tblUser.ID</c>.</summary>
    public const string UserDbId   = Base + "user-id";

    /// <summary>Integer group code from <c>tblUser.UserGroup</c>.</summary>
    public const string GroupId    = Base + "group-id";

    /// <summary>User area display name from <c>tblUser.AreaName</c>.</summary>
    public const string UserArea   = Base + "user-area";

    /// <summary>Integer area code from <c>tblUser.UserArea</c>.</summary>
    public const string UserAreaId = Base + "user-area-id";
}
