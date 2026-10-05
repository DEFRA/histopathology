using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Services;

namespace Histo.Web.Pages.Admin;

/// <summary>
/// Read-only list of Species pick-list items, linked from <c>Admin/PickListMaintenance</c>.
/// Species has its own dedicated pages (this, <see cref="AddSpeciesItemModel"/>,
/// <see cref="EditSpeciesItemModel"/>) rather than reusing the generic
/// <see cref="LookupItemsModel"/>/<see cref="AddLookupItemModel"/>/<see cref="EditLookupItemModel"/>
/// pages, because Species has no Active column and a non-standard column shape
/// (<c>SpeciesID</c>/<c>Species</c>/<c>CommonName</c>) that the generic infrastructure can't map.
/// </summary>
public class SpeciesItemsModel : GridPageModel
{
    private readonly ILookupService _lookups;

    public SpeciesItemsModel(ISessionService session, ILookupService lookups)
        : base(session) => _lookups = lookups;

    public IReadOnlyList<SpeciesItem> Items { get; private set; } = [];
    public string? StatusMessage { get; private set; }

    public int TotalCount => Items.Count;

    public IReadOnlyList<SpeciesItem> PagedEntries
    {
        get
        {
            IOrderedEnumerable<SpeciesItem> sorted = SortColumn switch
            {
                "SpeciesID" => SortDesc ? Items.OrderByDescending(i => i.SpeciesID) : Items.OrderBy(i => i.SpeciesID),
                "CommonName" => SortDesc ? Items.OrderByDescending(i => i.CommonName) : Items.OrderBy(i => i.CommonName),
                "Species" => SortDesc ? Items.OrderByDescending(i => i.Species) : Items.OrderBy(i => i.Species),
                _ => Items.OrderBy(i => i.Species),
            };
            return sorted.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();
        }
    }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Species";
        ViewData["PageTitle"] = "Species";
        StatusMessage = TempData["StatusMessage"] as string;

        Items = await _lookups.GetSpeciesItemsAsync();

        PopulateGridViewData(TotalCount);
    }
}
