using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;

namespace Histo.Web.Pages.Archive;

/// <summary>Replaces <c>ArchiveTissues.aspx</c>.</summary>
public class ArchiveTissuesModel : GridPageModel
{
    private readonly ISubmissionService _submissions;

    public ArchiveTissuesModel(ISessionService session, ISubmissionService submissions)
        : base(session) => _submissions = submissions;

    public IReadOnlyList<Animal> Animals { get; private set; } = [];

    public int TotalCount => Animals.Count;

    public IReadOnlyList<Animal> PagedEntries =>
        (SortColumn switch
        {
            "HistologyRef" => SortDesc ? Animals.OrderByDescending(a => a.HistologyRef) : Animals.OrderBy(a => a.HistologyRef),
            "OnHold"       => SortDesc ? Animals.OrderByDescending(a => a.OnHold)        : Animals.OrderBy(a => a.OnHold),
            _              => SortDesc ? Animals.OrderByDescending(a => a.SenderRef)     : Animals.OrderBy(a => a.SenderRef),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Archive Tissues";
        ViewData["PageTitle"] = "Archive Tissues";
        if (Session.BatchID > 0)
            Animals = await _submissions.GetAnimalsByBatchAsync(Session.BatchID ?? 0);

        PopulateGridViewData(TotalCount);
    }
}
