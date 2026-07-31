using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Submissions;

/// <summary>
/// Replaces <c>SubmissionDetails.aspx</c> — tissue details for the current sample (animal)
/// within a batch submission (non-cassetted workflow).
/// </summary>
public class SubmissionDetailsModel : HistoPageModel
{
    private readonly SubmissionService _submissions;

    public SubmissionDetailsModel(ISessionService session, SubmissionService submissions)
        : base(session) => _submissions = submissions;

    [BindProperty] public string? PMDate { get; set; }
    [BindProperty] public string? HistologyRef { get; set; }

    [BindProperty] public string TissueCode { get; set; } = string.Empty;
    [BindProperty] public short NoPieces { get; set; } = 1;
    [BindProperty] public string? Comment { get; set; }

    public Animal? Animal { get; private set; }
    public IReadOnlyList<Tissue> Tissues { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Sample Details";
        ViewData["PageTitle"] = "Sample Details";

        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;

        PMDate = Animal!.PMDate;
        HistologyRef = Animal.HistologyRef;
        Tissues = await _submissions.GetTissuesBySubmissionAsync(Animal.BatchSubmissionID);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveDetailsAsync()
    {
        ViewData["Title"] = "Sample Details";
        ViewData["PageTitle"] = "Sample Details";

        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;

        var updated = new Animal
        {
            ID = Animal!.ID,
            BatchSubmissionID = Animal.BatchSubmissionID,
            SenderRef = Animal.SenderRef,
            NextBlockRef = Animal.NextBlockRef,
            HistoRefSet = !string.IsNullOrWhiteSpace(HistologyRef),
            HistologyRef = HistologyRef,
            OnHold = Animal.OnHold,
            PMDate = PMDate,
            PMDateSet = !string.IsNullOrWhiteSpace(PMDate),
            IsPGNumber = Animal.IsPGNumber,
            BookedHistologyRef = Animal.BookedHistologyRef,
            RowStamp = Animal.RowStamp,
        };

        await _submissions.UpdateAnimalAsync(updated, Session.UserID);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddTissueAsync()
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;

        var tissue = new Tissue
        {
            OwnerID = Animal!.BatchSubmissionID,
            Owner = TissueOwner.Submission,
            TissueCode = TissueCode,
            NoPieces = NoPieces,
            Comment = Comment,
        };
        await _submissions.AddTissueAsync(tissue, Session.UserID);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteTissueAsync(int tissueId)
    {
        await _submissions.DeleteTissueAsync(tissueId, TissueOwner.Submission, Session.UserID);
        return RedirectToPage();
    }

    /// <summary>Resolves <see cref="Animal"/> from the current session's BatchID/AnimalID. Returns a redirect if unavailable.</summary>
    private async Task<IActionResult?> LoadAnimalAsync()
    {
        if (Session.BatchID <= 0) return RedirectToPage("/Index");
        if (Session.AnimalID is null) return RedirectToPage("/Submissions/ViewSamples");

        var animals = await _submissions.GetAnimalsByBatchAsync(Session.BatchID ?? 0);
        Animal = animals.FirstOrDefault(a => a.ID == Session.AnimalID);
        return Animal is null ? RedirectToPage("/Submissions/ViewSamples") : null;
    }
}
