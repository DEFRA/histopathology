using Histo.Administration.Interfaces;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Admin;

/// <summary>
/// Dedicated "edit an existing species" page, linked from <see cref="SpeciesItemsModel"/>'s
/// per-row "Change" link. See <see cref="SpeciesItemsModel"/> for why Species has its own
/// pages instead of reusing <see cref="EditLookupItemModel"/>.
/// </summary>
public class EditSpeciesItemModel : HistoPageModel
{
    private readonly ILookupService _lookups;

    public EditSpeciesItemModel(ISessionService session, ILookupService lookups)
        : base(session) => _lookups = lookups;

    [BindProperty(SupportsGet = true)] public int SpeciesID { get; set; }

    [BindProperty] public string Species { get; set; } = string.Empty;
    [BindProperty] public string? CommonName { get; set; }

    /// <summary>Field id → message, rendered via the shared clickable _ErrorSummary partial.</summary>
    public Dictionary<string, string> Errors { get; } = new();

    public string? SaveError { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        SetTitle();

        var existing = await _lookups.GetSpeciesItemsAsync();
        var item = existing.FirstOrDefault(i => i.SpeciesID == SpeciesID);
        if (item is null) return RedirectToPage("/Admin/SpeciesItems");

        Species = item.Species;
        CommonName = item.CommonName;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SetTitle();

        Validate();
        if (Errors.Count > 0) return Page();

        var ok = await _lookups.UpdateSpeciesItemAsync(
            SpeciesID, Species.Trim(), string.IsNullOrWhiteSpace(CommonName) ? null : CommonName.Trim(), Session.UserID);

        if (!ok)
        {
            SaveError = "Failed to save the species. Please try again.";
            return Page();
        }

        TempData["StatusMessage"] = $"'{Species.Trim()}' was updated.";
        return RedirectToPage("/Admin/SpeciesItems");
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Species)) Errors["Species"] = "Enter a species.";
    }

    private void SetTitle()
    {
        ViewData["Title"] = "Edit species";
        ViewData["PageTitle"] = "Edit species";
    }
}
