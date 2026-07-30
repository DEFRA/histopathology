using Histo.Administration.Models; // EditableLookup is in LookupItem.cs
using Histo.Administration.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Admin;

/// <summary>Replaces <c>PickListMaintenance.aspx</c>.</summary>
public class PickListMaintenanceModel : HistoPageModel
{
    private readonly LookupService _lookups;

    public PickListMaintenanceModel(ISessionService session, LookupService lookups)
        : base(session) => _lookups = lookups;

    public IReadOnlyList<EditableLookup> Lookups { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Pick List Maintenance";
        ViewData["PageTitle"] = "Pick List Maintenance";
        Lookups = await _lookups.ListEditableLookupsAsync();
    }
}
