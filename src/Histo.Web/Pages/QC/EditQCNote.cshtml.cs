using Histo.QualityControl.Models;
using Histo.QualityControl.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.QC;

/// <summary>Replaces <c>EditQCNote.aspx</c>.</summary>
public class EditQCNoteModel : HistoPageModel
{
    private readonly QCNoteService _qc;

    public EditQCNoteModel(ISessionService session, QCNoteService qc)
        : base(session) => _qc = qc;

    [BindProperty(SupportsGet = true)] public int NoteId { get; set; }
    [BindProperty]                     public string Text { get; set; } = string.Empty;

    public QCNote? Note { get; private set; }
    public string? ConcurrencyError { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Edit QC Note";
        ViewData["PageTitle"] = "Edit QC Note";
        Note = await _qc.GetByIdAsync(NoteId);
        if (Note is null) return RedirectToPage("/QC/QCNotes");
        Text = Note.Text;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Edit QC Note";
        ViewData["PageTitle"] = "Edit QC Note";
        Note = await _qc.GetByIdAsync(NoteId);
        if (Note?.RowStamp is null) return RedirectToPage("/QC/QCNotes");

        try
        {
            await _qc.UpdateAsync(NoteId, Text, Note.RowStamp, Session.UserID);
            return RedirectToPage("/QC/QCNotes");
        }
        catch (QCNoteConcurrencyException)
        {
            ConcurrencyError = "Another user has modified this QC note. Please reload and try again.";
            return Page();
        }
    }
}
