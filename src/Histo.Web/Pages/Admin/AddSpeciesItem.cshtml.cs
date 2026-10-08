using Histo.Administration.Interfaces;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Admin;

/// <summary>
/// Dedicated "add a new species" page, linked from <see cref="SpeciesItemsModel"/>'s
/// "Add item" button. See <see cref="SpeciesItemsModel"/> for why Species has its own
/// pages instead of reusing <see cref="AddLookupItemModel"/>.
/// </summary>
public class AddSpeciesItemModel : HistoPageModel
{
    private readonly ILookupService _lookups;

    public AddSpeciesItemModel(ISessionService session, ILookupService lookups)
        : base(session) => _lookups = lookups;

    [BindProperty] public string Species { get; set; } = string.Empty;
    [BindProperty] public string? CommonName { get; set; }

    /// <summary>Field id → message, rendered via the shared clickable _ErrorSummary partial.</summary>
    public Dictionary<string, string> Errors { get; } = new();

    public string? SaveError { get; private set; }

    public void OnGet()
    {
        SetTitle();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SetTitle();

        Validate();
        if (Errors.Count > 0) return Page();

        // AddtlkpSpecies's @SpeciesID is a plain (non-identity) column — the caller must supply it.
        var existing = await _lookups.GetSpeciesItemsAsync();
        var nextId = existing.Count == 0 ? 1 : existing.Max(i => i.SpeciesID) + 1;

        var ok = await _lookups.AddSpeciesItemAsync(nextId, Species.Trim(), string.IsNullOrWhiteSpace(CommonName) ? null : CommonName.Trim());

        if (!ok)
        {
            SaveError = "Failed to save the species. Please try again.";
            return Page();
        }

        TempData["StatusMessage"] = $"'{Species.Trim()}' was added.";
        return RedirectToPage("/Admin/SpeciesItems");
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Species)) Errors["Species"] = "Enter a species.";
    }

    private void SetTitle()
    {
        ViewData["Title"] = "Add species";
        ViewData["PageTitle"] = "Add species";
    }
}
