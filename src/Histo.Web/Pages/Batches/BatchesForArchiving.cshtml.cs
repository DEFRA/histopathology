using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;

namespace Histo.Web.Pages.Batches;

/// <summary>Replaces <c>BatchesForArchiving.aspx</c>.</summary>
public class BatchesForArchivingModel : HistoPageModel
{
    private readonly BatchService _batches;

    public BatchesForArchivingModel(ISessionService session, BatchService batches)
        : base(session) => _batches = batches;

    public IReadOnlyList<BatchListResult> Batches { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Batches for Archiving";
        ViewData["PageTitle"] = "Batches for Archiving";
        Batches = await _batches.GetCompletedAsync();
    }
}
