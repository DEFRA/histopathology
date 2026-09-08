using Histo.Histology.Interfaces;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Bookings;

/// <summary>
/// Manual admin override of the "next histology ref" counter for a given histology type,
/// setting it to an absolute value (looks up the current RowStamp itself via
/// <see cref="IHistologyRefService.SetCounterAsync"/>). Not a legacy page — legacy's real
/// increment-by-N range-booking workflow is now correctly implemented at
/// <see cref="Histo.Web.Pages.Bookings.BookHistologyRefModel"/> (SP <c>EditHistologyRef</c>).
///
/// Note: the legacy per-animal <c>EditHistologyRef.aspx</c> page (renaming an
/// individual sample's Sender Ref / Histology Ref via <c>clsAnimal.UpdateAnimalSenderRef</c>
/// / <c>UpdateAnimalHistologyRef</c>) is a different workflow, now implemented
/// separately at <see cref="Histo.Web.Pages.Admin.EditAnimalRefModel"/> (ISS-022).
/// </summary>
public class EditHistologyRefModel : HistoPageModel
{
    private readonly IHistologyRefService _refs;

    public EditHistologyRefModel(ISessionService session, IHistologyRefService refs)
        : base(session) => _refs = refs;

    [BindProperty] public int HistologyType { get; set; }
    [BindProperty] public string NewHistologyRef { get; set; } = string.Empty;

    public string? Error { get; private set; }
    public string? SuccessMessage { get; private set; }

    public void OnGet()
    {
        ViewData["Title"] = "Edit Histology Reference";
        ViewData["PageTitle"] = "Edit Histology Reference";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Edit Histology Reference";
        ViewData["PageTitle"] = "Edit Histology Reference";

        if (HistologyType <= 0)
        {
            Error = "You must select a Histology Ref Type.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(NewHistologyRef))
        {
            Error = "You must enter a Histology Ref.";
            return Page();
        }

        var ok = await _refs.SetCounterAsync(HistologyType, NewHistologyRef.Trim());
        if (!ok)
        {
            Error = "Failed to update the Histology Reference. Another user may have altered the record — please try again.";
            return Page();
        }

        SuccessMessage = "The Histology Reference has been updated.";
        return Page();
    }
}
