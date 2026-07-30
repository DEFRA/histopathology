using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Submissions;

/// <summary>Replaces <c>ViewSamples.aspx</c> — lists animals for the current batch submission.</summary>
public class ViewSamplesModel : HistoPageModel
{
    private readonly SubmissionService _submissions;

    public ViewSamplesModel(ISessionService session, SubmissionService submissions)
        : base(session) => _submissions = submissions;

    public IReadOnlyList<Animal> Animals { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "View Samples";
        ViewData["PageTitle"] = "View Samples";
        if (Session.BatchID <= 0) return RedirectToPage("/Index");
        Animals = await _submissions.GetAnimalsByBatchAsync(Session.BatchID ?? 0);
        return Page();
    }

    public async Task<IActionResult> OnPostSelectAsync(int animalId)
    {
        Session.AnimalID = animalId;
        return RedirectToPage("/Blocks/BlockDetails");
    }
}
