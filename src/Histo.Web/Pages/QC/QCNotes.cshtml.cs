using Histo.QualityControl.Models;
using Histo.QualityControl.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.QC;

/// <summary>
/// Lists QC notes for the current batch — replaces <c>QCNotes.aspx</c>.
/// </summary>
public class QCNotesModel : HistoPageModel
{
    private readonly QCNoteService _qc;

    public QCNotesModel(ISessionService session, QCNoteService qc)
        : base(session) => _qc = qc;

    public IReadOnlyList<QCNote> Notes { get; private set; } = [];
    public int BatchID => Session.BatchID ?? 0;

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "QC Notes";
        ViewData["PageTitle"] = "Quality Control Notes";
        if (Session.BatchID.HasValue)
            Notes = await _qc.GetBySubmissionAsync(Session.BatchID.Value);
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!Session.BatchID.HasValue) return RedirectToPage("/Index");
        await _qc.AddAsync(Session.BatchID.Value, Session.UserID);
        return RedirectToPage();
    }

    public IActionResult OnPostEdit(int noteId)
    {
        return RedirectToPage("/QC/EditQCNote", new { noteId });
    }
}
