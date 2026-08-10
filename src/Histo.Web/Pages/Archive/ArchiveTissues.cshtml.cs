using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;

namespace Histo.Web.Pages.Archive;

/// <summary>Replaces <c>ArchiveTissues.aspx</c>.</summary>
public class ArchiveTissuesModel : HistoPageModel
{
    private readonly ISubmissionService _submissions;

    public ArchiveTissuesModel(ISessionService session, ISubmissionService submissions)
        : base(session) => _submissions = submissions;

    public IReadOnlyList<Animal> Animals { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Archive Tissues";
        ViewData["PageTitle"] = "Archive Tissues";
        if (Session.BatchID > 0)
            Animals = await _submissions.GetAnimalsByBatchAsync(Session.BatchID ?? 0);
    }
}
