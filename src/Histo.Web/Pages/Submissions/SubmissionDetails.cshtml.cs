using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Submissions;

/// <summary>
/// Replaces <c>SubmissionDetails.aspx</c> — tissue details for the current sample (animal)
/// within a batch submission (non-cassetted workflow).
/// </summary>
public class SubmissionDetailsModel : HistoPageModel
{
    private readonly ISubmissionService _submissions;

    public SubmissionDetailsModel(ISessionService session, ISubmissionService submissions)
        : base(session) => _submissions = submissions;

    /// <summary>Batch ID from the URL (route/query). Needed for back-link and view-mode awareness.</summary>
    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }

    /// <summary>Animal ID from the URL (route/query). Falls back to session for legacy navigation paths.</summary>
    [BindProperty(SupportsGet = true)] public int? AnimalId { get; set; }

    /// <summary>Tissue awaiting delete confirmation — drives the inline GOV.UK confirmation panel.</summary>
    [BindProperty(SupportsGet = true)] public int? ConfirmDeleteTissueId { get; set; }

    [BindProperty] public string? PMDate { get; set; }
    [BindProperty] public string? HistologyRef { get; set; }

    [BindProperty] public string TissueCode { get; set; } = string.Empty;
    [BindProperty] public short NoPieces { get; set; } = 1;
    [BindProperty] public string? Comment { get; set; }

    public Animal? Animal { get; private set; }
    public IReadOnlyList<Tissue> Tissues { get; private set; } = [];

    /// <summary>Mirrors <see cref="SampleSummaryModel.IsViewMode"/> — hides edit/delete/add in View Submission journey.</summary>
    public bool IsViewMode => Session.IsViewSubmissionMode;

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Sample Details";
        ViewData["PageTitle"] = "Sample Details";

        BatchId ??= Session.BatchID;
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null) return Page();

        PMDate = Animal.PMDate;
        HistologyRef = Animal.HistologyRef;
        Tissues = await _submissions.GetTissuesBySubmissionAsync(BatchId ?? 0, Animal.BatchSubmissionID);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveDetailsAsync()
    {
        ViewData["Title"] = "Sample Details";
        ViewData["PageTitle"] = "Sample Details";

        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null) return RedirectToPage("/Submissions/SampleSummary", new { batchId = BatchId });

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
        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });
    }

    public async Task<IActionResult> OnPostAddTissueAsync()
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null) return RedirectToPage("/Submissions/SampleSummary", new { batchId = BatchId });

        var tissue = new Tissue
        {
            OwnerID = Animal!.BatchSubmissionID,
            Owner = TissueOwner.Submission,
            TissueCode = TissueCode,
            NoPieces = NoPieces,
            Comment = Comment,
        };
        await _submissions.AddTissueAsync(tissue, Session.UserID);
        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });
    }

    public async Task<IActionResult> OnPostDeleteTissueAsync(int tissueId)
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;

        await _submissions.DeleteTissueAsync(tissueId, TissueOwner.Submission, Session.UserID);
        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });
    }

    /// <summary>Resolves <see cref="Animal"/> from the current session's BatchID/AnimalID. Returns a redirect if unavailable.</summary>
    private async Task<IActionResult?> LoadAnimalAsync()
    {
        BatchId ??= Session.BatchID;
        if (BatchId is null or <= 0) return RedirectToPage("/Index");

        AnimalId ??= Session.AnimalID;
        if (AnimalId is null or <= 0) return RedirectToPage("/Submissions/SampleSummary", new { batchId = BatchId });
        Session.AnimalID = AnimalId;

        // Mirrors SampleSummaryModel/SubmissionDetailsBlockModel: a "Wet Tissue" batch can still
        // have animals recorded only in the block-animal table — check both before giving up.
        var blockAnimals = await _submissions.GetBlockAnimalsByBatchAsync(BatchId.Value);
        Animal = blockAnimals.FirstOrDefault(a => a.ID == AnimalId);
        if (Animal is null)
        {
            var animals = await _submissions.GetAnimalsByBatchAsync(BatchId.Value);
            Animal = animals.FirstOrDefault(a => a.ID == AnimalId);
        }

        // Deliberately does NOT redirect when the animal cannot be resolved — bouncing back to
        // SampleSummary is indistinguishable from "the button did nothing". Leaving Animal null
        // renders the view's "Sample not found" branch so the failure is visible to the user.
        return null;
    }
}
