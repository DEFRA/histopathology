using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;

namespace Histo.Web.Pages.Batches;

/// <summary>Replaces <c>BatchesForArchiving.aspx</c>.</summary>
public class BatchesForArchivingModel : GridPageModel
{
    private readonly IBatchService _batches;

    public BatchesForArchivingModel(ISessionService session, IBatchService batches)
        : base(session) => _batches = batches;

    public IReadOnlyList<BatchListResult> Batches { get; private set; } = [];

    public int TotalCount => Batches.Count;

    public IReadOnlyList<BatchListResult> PagedEntries =>
        (SortColumn switch
        {
            "ProjectDescription" => SortDesc ? Batches.OrderByDescending(b => b.ProjectDescription) : Batches.OrderBy(b => b.ProjectDescription),
            "ContactDescription" => SortDesc ? Batches.OrderByDescending(b => b.ContactDescription) : Batches.OrderBy(b => b.ContactDescription),
            "Species"            => SortDesc ? Batches.OrderByDescending(b => b.Species)            : Batches.OrderBy(b => b.Species),
            "OtherSubmittedBy"   => SortDesc ? Batches.OrderByDescending(b => b.OtherSubmittedBy)   : Batches.OrderBy(b => b.OtherSubmittedBy),
            "CompletedDate"      => SortDesc ? Batches.OrderByDescending(b => b.CompletedDate)      : Batches.OrderBy(b => b.CompletedDate),
            _                    => SortDesc ? Batches.OrderByDescending(b => b.ID)                  : Batches.OrderBy(b => b.ID),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Batches for Archiving";
        ViewData["PageTitle"] = "Batches for Archiving";
        Batches = await _batches.GetCompletedAsync();
        PopulateGridViewData(TotalCount);
    }
}
