using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Archive;

public class ArchiveMenuModel : HistoPageModel
{
    public ArchiveMenuModel(ISessionService session) : base(session) { }

    [BindProperty(SupportsGet = true)]
    public int? BatchId { get; set; }

    protected override int? RequestedBatchId => BatchId is > 0 ? BatchId : Session.BatchID;

    /// <summary>
    /// Page path for the back link, populated from <see cref="ISessionService.ReturnPage"/>
    /// (set by whichever page navigated here — BatchesForArchiving, SearchSubmissions, etc.).
    /// Falls back to the canonical Archive entry point if absent (e.g. direct URL access).
    /// </summary>
    public string BackLinkPage => string.IsNullOrWhiteSpace(Session.ReturnPage)
        ? "/Batches/BatchesForArchiving"
        : Session.ReturnPage;

    public void OnGet()
    {
        if (BatchId is > 0) Session.BatchID = BatchId;
        BatchId ??= Session.BatchID;
    }
}
