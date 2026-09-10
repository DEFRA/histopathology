using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Replaces <c>SubmissionsOnHold.aspx</c> — "Put Samples On Hold", reached from Edit Submission
/// Status's "Samples on hold" button. Legacy shows every sample of the CURRENT batch only (not a
/// global report of every on-hold batch in the system) and lets the user toggle each sample's
/// On Hold flag via a checkbox; this page previously (incorrectly) listed every on-hold batch
/// system-wide.
/// </summary>
public class SubmissionsOnHoldModel : HistoPageModel
{
    private readonly IBatchService _batches;
    private readonly ISubmissionService _submissions;

    public SubmissionsOnHoldModel(ISessionService session, IBatchService batches, ISubmissionService submissions)
        : base(session)
    {
        _batches = batches;
        _submissions = submissions;
    }

    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }

    /// <summary>Checked animal IDs from the "On hold" checkbox column.</summary>
    [BindProperty] public List<int> OnHoldAnimalIds { get; set; } = [];

    public Batch? Batch { get; private set; }
    public IReadOnlyList<Animal> Animals { get; private set; } = [];
    public string? SaveError { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Samples on hold";
        ViewData["PageTitle"] = "Samples on hold";

        BatchId ??= Session.BatchID;
        if (BatchId is null or <= 0) return RedirectToPage("/Index");
        Session.BatchID = BatchId;

        var forbidden = await CheckBatchAccessAsync(_batches, BatchId.Value);
        if (forbidden is not null) return forbidden;

        Batch = await _batches.GetByIdAsync(BatchId.Value);
        if (Batch is null) return RedirectToPage("/Index");

        Animals = await LoadAnimalsAsync(BatchId.Value);
        return Page();
    }

    /// <summary>Applies the checked "On hold" state to every sample in the batch, then returns to this page.</summary>
    public async Task<IActionResult> OnPostSaveAsync()
    {
        BatchId ??= Session.BatchID;
        if (BatchId is null or <= 0) return RedirectToPage("/Index");

        var forbidden = await CheckBatchAccessAsync(_batches, BatchId.Value);
        if (forbidden is not null) return forbidden;

        var animals = await LoadAnimalsAsync(BatchId.Value);
        var onHoldIds = OnHoldAnimalIds.ToHashSet();
        var anyFailed = false;
        foreach (var animal in animals)
        {
            var shouldBeOnHold = onHoldIds.Contains(animal.ID);
            if (animal.OnHold == shouldBeOnHold) continue;
            animal.OnHold = shouldBeOnHold;
            if (!await _submissions.UpdateAnimalAsync(animal, Session.UserID))
                anyFailed = true;
        }

        if (anyFailed)
        {
            SaveError = "Could not save one or more on-hold changes. Please try again.";
            Batch = await _batches.GetByIdAsync(BatchId.Value);
            Animals = await LoadAnimalsAsync(BatchId.Value);
            return Page();
        }

        return RedirectToPage(new { batchId = BatchId });
    }

    /// <summary>Legacy btnDone_Click — returns to Edit Submission Status without further action.</summary>
    public IActionResult OnPostDone()
    {
        BatchId ??= Session.BatchID;
        return RedirectToPage("/Batches/EditSubmissionStatus", new { batchId = BatchId });
    }

    private async Task<IReadOnlyList<Animal>> LoadAnimalsAsync(int batchId)
    {
        var blockAnimals = await _submissions.GetBlockAnimalsByBatchAsync(batchId);
        var allAnimals = await _submissions.GetAnimalsByBatchAsync(batchId);
        var merged = blockAnimals.Count > 0 ? MergeAnimals(blockAnimals, allAnimals) : allAnimals;

        // GetBlockAnimalsByBatchAsync's result set doesn't reliably carry OnHold or RowStamp (same
        // gap that broke BatchSubmissionID elsewhere) — patch both from the plain animals list,
        // which does. A missing/stale RowStamp makes EditAnimal's concurrency check fail silently,
        // which was why ticking a box and clicking Save appeared to do nothing.
        var patchById = allAnimals.ToDictionary(a => a.ID, a => a);
        foreach (var animal in merged)
            if (patchById.TryGetValue(animal.ID, out var real))
            {
                animal.OnHold   = real.OnHold;
                animal.RowStamp = real.RowStamp;
            }

        return merged;
    }

    /// <summary>Unions two animal lists by ID, keeping the first list's entries and appending any not already present.</summary>
    private static IReadOnlyList<Animal> MergeAnimals(IReadOnlyList<Animal> primary, IReadOnlyList<Animal> supplementary)
    {
        var seenIds = primary.Select(a => a.ID).ToHashSet();
        var missing = supplementary.Where(a => !seenIds.Contains(a.ID));
        return [.. primary, .. missing];
    }
}
