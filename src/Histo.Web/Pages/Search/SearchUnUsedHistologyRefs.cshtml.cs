using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Web.Services;

namespace Histo.Web.Pages.Search;

/// <summary>Replaces <c>SearchUnUsedHistologyRefs.aspx</c>.</summary>
public class SearchUnUsedHistologyRefsModel : HistoPageModel
{
    private readonly IHistologyRefService _histologyRefs;

    public SearchUnUsedHistologyRefsModel(ISessionService session, IHistologyRefService histologyRefs)
        : base(session) => _histologyRefs = histologyRefs;

    public IReadOnlyList<HistologyRef> Results { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Unused Histology Refs";
        ViewData["PageTitle"] = "Unused Histology Refs";
        Results = await _histologyRefs.GetAllUnusedRefsAsync();
    }
}
