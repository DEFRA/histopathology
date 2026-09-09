using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Bookings;

/// <summary>
/// Replaces <c>BookHistologyRef.aspx</c> — a standalone admin utility that pre-reserves a
/// contiguous range of future histology ref numbers for a type by incrementing that type's
/// "next histology ref" counter. Not linked to any specific animal/session — confirmed the
/// legacy page never reads <c>Session(SV_AnimalID)</c> or similar.
///
/// Legacy source: BookHistologyRef.aspx.vb::UpdateHistologyRefs/btnOK_Click.
/// </summary>
public class BookHistologyRefModel : HistoPageModel
{
    private readonly IHistologyRefService _refs;

    public BookHistologyRefModel(ISessionService session, IHistologyRefService refs)
        : base(session) => _refs = refs;

    public IReadOnlyList<HistologyRefCounter> Counters { get; private set; } = [];

    [BindProperty] public int HistologyType { get; set; }
    [BindProperty] public int? NumberToBook { get; set; }

    public string? Error { get; private set; }
    public string? SuccessMessage { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Block book histology refs";
        ViewData["PageTitle"] = "Block book histology refs";
        Counters = await _refs.GetCountersAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Block book histology refs";
        ViewData["PageTitle"] = "Block book histology refs";

        if (HistologyType <= 0)
        {
            Error = "You must select a Histology Ref Range Type.";
            Counters = await _refs.GetCountersAsync();
            return Page();
        }

        if (NumberToBook is not > 0)
        {
            Error = "You must enter a valid Number Required (a whole number greater than zero).";
            Counters = await _refs.GetCountersAsync();
            return Page();
        }

        var result = await _refs.BookCounterRangeAsync(HistologyType, NumberToBook.Value);
        Counters = await _refs.GetCountersAsync();

        if (!result.Success)
        {
            Error = result.Error;
            return Page();
        }

        SuccessMessage = $"You have successfully booked Histology numbers in the range {result.FirstBooked} - {result.LastBooked}, inclusive.";
        return Page();
    }
}
