using Histo.QualityControl.Interfaces;
using Histo.QualityControl.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.QC;

/// <summary>
/// Lists QC notes — replaces <c>QCNotes.aspx</c>.
/// Shows QC Note Ref, Stain Ref, Project, Species columns matching the legacy grid.
/// Quick-Go textbox allows direct navigation to a note by QC Note Ref.
///
/// ISS-045: when no batch is selected in session (page entered directly from the
/// home page "QC notes" link), falls back to loading all notes system-wide,
/// matching legacy <c>QCNotes.aspx</c> global load behaviour.
/// </summary>
public class QCNotesModel : GridPageModel
{
    private readonly IQCNoteService _qc;

    public QCNotesModel(ISessionService session, IQCNoteService qc)
        : base(session) => _qc = qc;

    public IReadOnlyList<QCNote> Notes { get; private set; } = [];
    public int? BatchID => Session.BatchID;
    public bool IsGlobalView => !Session.BatchID.HasValue || Session.BatchID.Value == 0;

    public IReadOnlyList<QCNote> PagedEntries =>
        (SortColumn switch
        {
            "SubmissionNo" => SortDesc ? Notes.OrderByDescending(n => n.ID)                  : Notes.OrderBy(n => n.ID),
            "StainRef"     => SortDesc ? Notes.OrderByDescending(n => n.StainRef)             : Notes.OrderBy(n => n.StainRef),
            "Project"      => SortDesc ? Notes.OrderByDescending(n => n.ProjectDescription)   : Notes.OrderBy(n => n.ProjectDescription),
            "Species"      => SortDesc ? Notes.OrderByDescending(n => n.Species)              : Notes.OrderBy(n => n.Species),
            _              => SortDesc ? Notes.OrderByDescending(n => n.QCNoteRef)            : Notes.OrderBy(n => n.QCNoteRef),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    /// <summary>Quick-Go: direct navigation to a QC note by its reference number.</summary>
    [BindProperty]
    public int? QuickGoRef { get; set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "QC notes";
        ViewData["PageTitle"] = "Quality control notes";
        Notes = await _qc.GetAllAsync();
        PopulateGridViewData(Notes.Count);
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
