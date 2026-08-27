using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Submissions;

/// <summary>
/// Replaces <c>SubmissionDetailsBlock.aspx</c> — block summary for the current sample (animal)
/// within a batch submission (cassetted workflow). This is the single, animal-scoped page for
/// managing a sample's blocks (add/edit/delete/copy) — <c>Pages/Blocks/BlockDetails.cshtml</c>
/// remains the separate batch-wide view reached from Submission details "Assign blocks".
///
/// SIMPLIFIED: the legacy page renders a hierarchical block/tissue grid with per-block
/// test-assignment checkboxes (EO, H&amp;E, H&amp;E BSE, IHC Prp, IHC Other, Special Stain).
/// The migrated <see cref="Block"/> model does not carry per-block test flags (see
/// <c>BatchService.GetTestItemRowsAsync</c> scope notes for the existing precedent) — this
/// page presents block reference, customer ref, comment, repeat, and status only.
/// </summary>
public class SubmissionDetailsBlockModel : HistoPageModel
{
    private readonly ISubmissionService _submissions;
    private readonly IBlockService _blocks;

    public SubmissionDetailsBlockModel(ISessionService session, ISubmissionService submissions, IBlockService blocks)
        : base(session)
    {
        _submissions = submissions;
        _blocks = blocks;
    }

    [BindProperty] public string? PMDate { get; set; }
    [BindProperty] public string? HistologyRef { get; set; }

    [BindProperty] public string? NewBlockRef { get; set; }
    [BindProperty] public string? NewCustomerRef { get; set; }
    [BindProperty] public bool NewRepeatBlock { get; set; }

    /// <summary>Block awaiting delete confirmation — drives the inline GOV.UK confirmation panel (replaces browser confirm()).</summary>
    [BindProperty(SupportsGet = true)] public int? ConfirmDeleteBlockId { get; set; }

    /// <summary>Set when the user has requested the inline "Check used block refs" lookup for this sample.</summary>
    [BindProperty(SupportsGet = true)] public bool ShowUsedRefs { get; set; }

    public Animal? Animal { get; private set; }
    public IReadOnlyList<Block> Blocks { get; private set; } = [];
    public IReadOnlyList<BlockRefRangeHelpers.BlockRefRangeRow> UsedBlockRefResults { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Sample Blocks";
        ViewData["PageTitle"] = "Sample Blocks";

        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;

        PMDate = Animal!.PMDate;
        HistologyRef = Animal.HistologyRef;

        var allBlocks = await _blocks.GetByBatchAsync(Session.BatchID ?? 0);
        Blocks = allBlocks.Where(b => b.AnimalID == Animal.ID).ToList();

        if (ShowUsedRefs)
        {
            var used = await _blocks.GetUsedBlockRefsBySenderRefAsync(Animal.SenderRef);
            UsedBlockRefResults = BlockRefRangeHelpers.ComputeRanges(
                used.Select(b => (b.BlockRef, b.Status)).ToList());
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveDetailsAsync()
    {
        ViewData["Title"] = "Sample Blocks";
        ViewData["PageTitle"] = "Sample Blocks";

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

    /// <summary>Adds a new block for this sample \u2014 restores the "Add block" action missing from the migrated page.</summary>
    public async Task<IActionResult> OnPostAddBlockAsync()
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;

        if (!string.IsNullOrWhiteSpace(NewBlockRef))
        {
            var allBlocks = await _blocks.GetByBatchAsync(Session.BatchID ?? 0);
            var existingOrders = allBlocks.Select(b => b.Order);
            await _blocks.AddBlockAsync(
                Session.BatchID ?? 0, Animal!.ID, NewBlockRef, existingOrders, Session.UserID,
                NewCustomerRef, comment: null, repeatBlock: NewRepeatBlock);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int blockId)
    {
        await _blocks.DeleteBlockAsync(blockId, Session.UserID);
        return RedirectToPage();
    }

    /// <summary>
    /// Stores the selected block IDs and redirects to the "Copy blocks" workflow.
    /// Replaces the legacy <c>SubmissionDetailsBlock.aspx.vb</c>::<c>btnCopyBlock_Click</c>
    /// handler, which stored the selection in <c>Session(SV_BlockIDs)</c> before
    /// redirecting to <c>CopyBlocks.aspx</c>.
    /// </summary>
    public async Task<IActionResult> OnPostCopyAsync(List<int>? blockIds)
    {
        if (blockIds is null || blockIds.Count == 0)
        {
            var redirect = await LoadAnimalAsync();
            return redirect ?? RedirectToPage();
        }

        TempData["CopyBlockIds"] = string.Join(",", blockIds);
        return RedirectToPage("/Blocks/CopyBlocks");
    }

    /// <summary>Resolves <see cref="Animal"/> from the current session's BatchID/AnimalID. Returns a redirect if unavailable.</summary>
    private async Task<IActionResult?> LoadAnimalAsync()
    {
        if (Session.BatchID <= 0) return RedirectToPage("/Index");
        if (Session.AnimalID is null) return RedirectToPage("/Submissions/BatchBlockSummary");

        var animals = await _submissions.GetAnimalsByBatchAsync(Session.BatchID ?? 0);
        Animal = animals.FirstOrDefault(a => a.ID == Session.AnimalID);
        return Animal is null ? RedirectToPage("/Submissions/BatchBlockSummary") : null;
    }
}
