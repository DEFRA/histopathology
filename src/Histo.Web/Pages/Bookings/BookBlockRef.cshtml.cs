using Histo.Histology.Models;
using Histo.Histology.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Bookings;

/// <summary>Replaces <c>BookBlockRef.aspx</c>.</summary>
public class BookBlockRefModel : HistoPageModel
{
    private readonly BlockService _blocks;
    private readonly HistologyRefService _refs;

    public BookBlockRefModel(ISessionService session, BlockService blocks, HistologyRefService refs)
        : base(session) { _blocks = blocks; _refs = refs; }

    public IReadOnlyList<Block> PreBookedBlocks { get; private set; } = [];
    public string? Error { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Book Block Reference";
        if (Session.AnimalID > 0)
            PreBookedBlocks = await _blocks.GetPreBookedByAnimalAsync(Session.AnimalID ?? 0);
    }

    public async Task<IActionResult> OnPostAsync(int blockId)
    {
        // Booking logic delegates to BlockService update — placeholder until full workflow confirmed.
        _ = blockId;
        return await Task.FromResult(RedirectToPage("/Bookings/BookingMenu"));
    }
}
