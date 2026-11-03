using Histo.Administration.Interfaces;
using Histo.Administration.Models; // EditableLookup is in LookupItem.cs
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Admin;

/// <summary>Replaces <c>PickListMaintenance.aspx</c>.</summary>
public class PickListMaintenanceModel : GridPageModel
{
    private readonly ILookupService _lookups;

    public PickListMaintenanceModel(ISessionService session, ILookupService lookups)
        : base(session) => _lookups = lookups;

    public IReadOnlyList<EditableLookup> Lookups { get; private set; } = [];

    private List<EditableLookup> GetFilteredLookups() =>
        Lookups.Where(l => l.TableName != "User Area").ToList();

    public int TotalCount => GetFilteredLookups().Count;

    public IReadOnlyList<EditableLookup> GetPagedEntries() =>
        (SortDesc ? GetFilteredLookups().OrderByDescending(l => l.TableName) : GetFilteredLookups().OrderBy(l => l.TableName))
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Pick List Maintenance";
        ViewData["PageTitle"] = "Pick List Maintenance";
        Lookups = await _lookups.ListEditableLookupsAsync();
        PopulateGridViewData(TotalCount);
    }
}
