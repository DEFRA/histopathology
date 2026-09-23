using Histo.Administration.Models;

namespace Histo.Web.Pages.Admin;

/// <summary>
/// Shared field-shape rules for the pick-list maintenance pages (<see cref="LookupItemsModel"/>,
/// <see cref="AddLookupItemModel"/>, <see cref="EditLookupItemModel"/>), so the three pages stay
/// in lock-step on which fields a given lookup table needs. Confirmed against every editable
/// lookup table's real Add stored procedure (<c>sys.parameters</c>): all 14 tables except
/// Contacts (18) and Projects (19) are "Code-keyed" (@Code/@Description/@IsActive); Contacts and
/// Projects are "Area-scoped" (@Area/@Description/@IsActive/@ID, no @Code).
/// </summary>
internal static class LookupTableSchema
{
    /// <summary>Area-scoped tables (Contacts/Pathologists = 18, Projects = 19) show an Area dropdown instead of a Code field.</summary>
    public static bool ShowAreaColumn(int tableId) => tableId is 18 or 19;

    /// <summary>True when the loaded table's items carry a distinct string Code column (all tables except 18/19).</summary>
    public static bool HasCodes(IReadOnlyList<LookupItem> items) => items.Any(i => i.Code is not null);
}
