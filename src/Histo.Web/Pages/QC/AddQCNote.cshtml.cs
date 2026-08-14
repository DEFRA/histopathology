using Histo.QualityControl.Interfaces;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.QC;

/// <summary>
/// Replaces <c>QCNoteForm.aspx</c>'s underlying create workflow — the legacy ASPX
/// itself only printed a QC note report; the actual blank-note creation happened
/// inline in <c>QualityData.aspx.vb</c> (<c>clsQCNote.NewQCNote</c>). This page exposes
/// <c>IQCNoteRepository.AddAsync</c> (via <see cref="QCNoteService.AddAsync"/>)
/// as a dedicated "Add QC Note" page, letting the user enter the note text immediately
/// rather than requiring a separate trip to <c>EditQCNote</c>.
/// </summary>
public class AddQCNoteModel : HistoPageModel
{
    private readonly IQCNoteService _qc;

    public AddQCNoteModel(ISessionService session, IQCNoteService qc)
        : base(session) => _qc = qc;

    [BindProperty] public string Text { get; set; } = string.Empty;

    public int BatchID => Session.BatchID ?? 0;
    public string? Error { get; private set; }

    public IActionResult OnGet()
    {
        ViewData["Title"] = "Add QC Note";
        ViewData["PageTitle"] = "Add QC Note";
        if (!Session.BatchID.HasValue) return RedirectToPage("/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Add QC Note";
        ViewData["PageTitle"] = "Add QC Note";

        if (!Session.BatchID.HasValue) return RedirectToPage("/Index");

        var newId = await _qc.AddAsync(Session.BatchID.Value, Session.UserID);
        if (newId <= 0)
        {
            Error = "Failed to add the QC note. Please try again.";
            return Page();
        }

        // Save the entered text (if any) against the note just created.
        if (!string.IsNullOrWhiteSpace(Text))
        {
            var note = await _qc.GetByIdAsync(newId);
            if (note?.RowStamp is not null)
                await _qc.UpdateAsync(newId, Text, note.RowStamp, Session.UserID);
        }

        return RedirectToPage("/QC/QCNotes");
    }
}
