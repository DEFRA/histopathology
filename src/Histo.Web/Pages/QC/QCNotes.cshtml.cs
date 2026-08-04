using Histo.QualityControl.Models;
using Histo.QualityControl.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.QC;

/// <summary>
/// Lists QC notes for the current batch — replaces <c>QCNotes.aspx</c>.
/// Shows QC Note Ref, Stain Ref, Project, Species columns matching the legacy grid.
/// Quick-Go textbox allows direct navigation to a note by QC Note Ref.
/// </summary>
public class QCNotesModel : HistoPageModel
{
    private readonly QCNoteService _qc;

    public QCNotesModel(ISessionService session, QCNoteService qc)
        : base(session) => _qc = qc;

    public IReadOnlyList<QCNote> Notes { get; private set; } = [];
    public int BatchID => Session.BatchID ?? 0;

    /// <summary>Quick-Go: direct navigation to a QC note by its reference number.</summary>
    [BindProperty]
    public int? QuickGoRef { get; set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "QC notes";
        ViewData["PageTitle"] = "Quality control notes";
        if (Session.BatchID.HasValue)
            Notes = await _qc.GetBySubmissionAsync(Session.BatchID.Value);
    }

    public IActionResult OnPostEdit(int noteId)
    {
        return RedirectToPage("/QC/EditQCNote", new { noteId });
    }

    public IActionResult OnPostGoAsync()
    {
        if (QuickGoRef.HasValue)
            return RedirectToPage("/QC/EditQCNote", new { noteId = QuickGoRef.Value });
        return RedirectToPage();
    }
}
