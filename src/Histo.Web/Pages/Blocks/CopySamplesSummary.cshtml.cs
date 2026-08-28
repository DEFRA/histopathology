using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Blocks;

/// <summary>
/// Replaces the confirmation step of the legacy "Copy samples" wizard —
/// displays the outcome of a block-copy operation initiated on
/// <see cref="CopySamplesModel"/>.
/// </summary>
public class CopySamplesSummaryModel : HistoPageModel
{
    private readonly IBatchService _batches;
    private readonly ISubmissionService _submissions;

    public CopySamplesSummaryModel(ISessionService session, IBatchService batches, ISubmissionService submissions)
        : base(session)
    {
        _batches = batches;
        _submissions = submissions;
    }

    public int SourceBatchId { get; private set; }
    public Animal? SourceAnimal { get; private set; }
    public IReadOnlyList<Animal> TargetAnimals { get; private set; } = [];
    public int BlocksCopiedCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(int sourceBatchId, int sourceAnimalId, string? targetAnimalIds, int blocksCopiedCount)
    {
        ViewData["Title"] = "Samples Copied";
        ViewData["PageTitle"] = "Samples Copied";

        SourceBatchId = sourceBatchId;
        BlocksCopiedCount = blocksCopiedCount;

        var sourceAnimals = await _submissions.GetAnimalsByBatchAsync(sourceBatchId);
        SourceAnimal = sourceAnimals.FirstOrDefault(a => a.ID == sourceAnimalId);

        var ids = ParseIds(targetAnimalIds);
        if (Session.BatchID is not null && ids.Count > 0)
        {
            var forbidden = await CheckBatchAccessAsync(_batches, Session.BatchID.Value);
            if (forbidden is not null) return forbidden;

            var currentAnimals = await _submissions.GetAnimalsByBatchAsync(Session.BatchID ?? 0);
            TargetAnimals = currentAnimals.Where(a => ids.Contains(a.ID)).OrderBy(a => a.SenderRef).ToList();
        }

        return Page();
    }

    public IActionResult OnPostDone() => RedirectToPage("/Submissions/SubmissionDetailsBlock");

    private static List<int> ParseIds(string? csv) =>
        string.IsNullOrEmpty(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                 .Select(s => int.TryParse(s, out var n) ? n : 0)
                 .Where(n => n > 0)
                 .ToList();
}
