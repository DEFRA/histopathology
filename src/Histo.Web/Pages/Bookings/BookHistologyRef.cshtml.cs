using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Bookings;

/// <summary>Replaces <c>BookHistologyRef.aspx</c>.</summary>
public class BookHistologyRefModel : HistoPageModel
{
    private readonly IHistologyRefService _refs;

    public BookHistologyRefModel(ISessionService session, IHistologyRefService refs)
        : base(session) => _refs = refs;

    public IReadOnlyList<HistologyRef> AvailableRefs { get; private set; } = [];
    public string? Error { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Book Histology Reference";
        AvailableRefs = await _refs.GetUnusedRefsAsync(1); // type 1 = standard histology
    }

    public async Task<IActionResult> OnPostAsync(string histoRef)
    {
        if (Session.AnimalID <= 0) return RedirectToPage("/Index");
        var ok = await _refs.BookRefAsync(histoRef, Session.AnimalID ?? 0, Session.UserID);
        if (!ok) { Error = "Could not book the selected reference."; AvailableRefs = await _refs.GetUnusedRefsAsync(1); return Page(); }
        return RedirectToPage("/Submissions/ViewSamples");
    }
}
