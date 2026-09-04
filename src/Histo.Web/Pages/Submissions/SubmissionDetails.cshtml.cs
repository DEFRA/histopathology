using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
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
    private const int LookupTissueCode = 9;

    private readonly ISubmissionService _submissions;
    private readonly ILookupService _lookups;

    public SubmissionDetailsModel(ISessionService session, ISubmissionService submissions, ILookupService lookups)
        : base(session)
    {
        _submissions = submissions;
        _lookups = lookups;
    }

    /// <summary>Batch ID from the URL (route/query). Needed for back-link and view-mode awareness.</summary>
    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }

    /// <summary>Animal ID from the URL (route/query). Falls back to session for legacy navigation paths.</summary>
    [BindProperty(SupportsGet = true)] public int? AnimalId { get; set; }

    /// <summary>Tissue awaiting delete confirmation — drives the inline GOV.UK confirmation panel.</summary>
    [BindProperty(SupportsGet = true)] public int? ConfirmDeleteTissueId { get; set; }

    /// <summary>Tissue currently shown in its inline edit row.</summary>
    [BindProperty(SupportsGet = true)] public int? EditTissueId { get; set; }

    [BindProperty] public string? PMDate { get; set; }
    [BindProperty] public string? HistologyRef { get; set; }

    [BindProperty] public int TissueId { get; set; }
    [BindProperty] public string TissueCode { get; set; } = string.Empty;
    [BindProperty] public short NoPieces { get; set; } = 1;
    [BindProperty] public string? Comment { get; set; }

    /// <summary>Inline edit-row fields — separate from <see cref="TissueCode"/>/<see cref="NoPieces"/>/<see cref="Comment"/> above (the Add tissue form) so editing a row doesn't pre-fill Add tissue.</summary>
    [BindProperty] public string EditTissueCode { get; set; } = string.Empty;
    [BindProperty] public short EditNoPieces { get; set; } = 1;
    [BindProperty] public string? EditComment { get; set; }

    public Animal? Animal { get; private set; }
    public IReadOnlyList<Tissue> Tissues { get; private set; } = [];
    public IReadOnlyList<LookupItem> TissueOptions { get; private set; } = [];

    /// <summary>Resolves a tissue code to its description, matching legacy's GetListType(TissueCode, LOOKUP_TISSUE_CODE).</summary>
    public string TissueName(string code) => TissueOptions.FirstOrDefault(o => o.Code == code)?.Name ?? code;

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

        PMDate = DateFormatHelpers.ToIsoDate(Animal.PMDate);
        HistologyRef = Animal.HistologyRef;
        Tissues = await _submissions.GetTissuesBySubmissionAsync(BatchId ?? 0, Animal.BatchSubmissionID);
        TissueOptions = await _lookups.GetLookupDataAsync(LookupTissueCode);

        if (EditTissueId is > 0)
        {
            var editing = Tissues.FirstOrDefault(t => t.ID == EditTissueId);
            if (editing is not null)
            {
                TissueId = editing.ID;
                EditTissueCode = editing.TissueCode;
                EditNoPieces = editing.NoPieces;
                EditComment = editing.Comment;
            }
        }

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
            PMDate = DateFormatHelpers.ToLegacyDate(PMDate),
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

        if (!string.IsNullOrWhiteSpace(TissueCode))
        {
            var tissue = new Tissue
            {
                OwnerID = Animal!.BatchSubmissionID,
                Owner = TissueOwner.Submission,
                TissueCode = TissueCode,
                NoPieces = NoPieces,
                Comment = Comment,
            };
            await _submissions.AddTissueAsync(tissue, Session.UserID);
        }
        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });
    }

    public async Task<IActionResult> OnPostUpdateTissueAsync()
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null || TissueId <= 0) return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });

        var existing = (await _submissions.GetTissuesBySubmissionAsync(BatchId ?? 0, Animal.BatchSubmissionID))
            .FirstOrDefault(t => t.ID == TissueId);
        if (existing is null || string.IsNullOrWhiteSpace(EditTissueCode))
            return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });

        var updated = new Tissue
        {
            ID = TissueId,
            OwnerID = existing.OwnerID,
            Owner = TissueOwner.Submission,
            TissueCode = EditTissueCode,
            NoPieces = EditNoPieces,
            Comment = EditComment,
            ArchiveLocation = existing.ArchiveLocation,
            ArchivedDate = existing.ArchivedDate,
            ArchiveComment = existing.ArchiveComment,
            RowStamp = existing.RowStamp,
        };
        await _submissions.UpdateTissueAsync(updated, Session.UserID);
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

        // Neither GetBatchAnimal nor GetBatchBlockAnimal return a BatchSubmissionID column, so
        // it's always 0 from either source above — resolve the real value from the already
        // batch-scoped, already-fixed GetSubmissionsByBatchAsync (an animal can have
        // BatchSubmission rows under other batches too, so this must be scoped to this batch).
        if (Animal is not null)
        {
            var submissions = await _submissions.GetSubmissionsByBatchAsync(BatchId.Value);
            var realSubmissionId = submissions.FirstOrDefault(s => s.AnimalID == AnimalId)?.ID;
            if (realSubmissionId is > 0)
                Animal.BatchSubmissionID = realSubmissionId.Value;
        }

        // Deliberately does NOT redirect when the animal cannot be resolved — bouncing back to
        // SampleSummary is indistinguishable from "the button did nothing". Leaving Animal null
        // renders the view's "Sample not found" branch so the failure is visible to the user.
        return null;
    }
}
