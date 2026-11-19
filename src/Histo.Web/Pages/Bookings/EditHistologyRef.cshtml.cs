using Histo.Histology.Interfaces;
using Histo.Histology.Models;
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
    private const string EditHistologyRefTitle = "Edit Histology Reference";

    private readonly IHistologyRefService _refs;

    public EditHistologyRefModel(ISessionService session, IHistologyRefService refs)
        : base(session) => _refs = refs;

    [BindProperty] public int HistologyType { get; set; }
    [BindProperty] public string NewHistologyRef { get; set; } = string.Empty;

    public string? Error { get; private set; }
    public string? SuccessMessage { get; private set; }

    public void OnGet()
    {
        ViewData["Title"] = EditHistologyRefTitle;
        ViewData["PageTitle"] = EditHistologyRefTitle;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = EditHistologyRefTitle;
        ViewData["PageTitle"] = EditHistologyRefTitle;

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

        // The counter column is an unconstrained varchar(5); a non-numeric or out-of-range value
        // saved here silently breaks every draw for that type (both the "Get next ref" button and
        // auto-generate), so it is rejected at the point of entry.
        if (!HistologyRefTypeCode.TryGetRange(HistologyType, out var lower, out var upper))
        {
            Error = "You must select a Histology Ref Type.";
            return Page();
        }

        var entered = NewHistologyRef.Trim();
        // Exactly five ASCII digits: the column is varchar(5), and int.TryParse would otherwise
        // accept "040000"/"+40000"/"4 0000" — all in range numerically, none storable in 5 chars.
        if (entered.Length != 5 || !entered.All(char.IsAsciiDigit) ||
            !int.TryParse(entered, out var number) || number < lower || number >= upper)
        {
            Error = $"The Histology Ref must be a 5-digit number between {lower} and {upper - 1} for this type.";
            return Page();
        }

        var ok = await _refs.SetCounterAsync(HistologyType, entered);
        if (!ok)
        {
            Error = "Failed to update the Histology Reference. Another user may have altered the record — please try again.";
            return Page();
        }

        SuccessMessage = "The Histology Reference has been updated.";
        return Page();
    }
}
