using Histo.Histology.Models;
using Histo.Histology.Services;
using Histo.Web.Services;

namespace Histo.Web.Pages.Archive;

/// <summary>Replaces <c>ArchiveBlocks.aspx</c>.</summary>
public class ArchiveBlocksModel : HistoPageModel
{
    private readonly BlockService _blocks;

    public ArchiveBlocksModel(ISessionService session, BlockService blocks)
        : base(session) => _blocks = blocks;

    public IReadOnlyList<Block> Blocks { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Archive Blocks";
        ViewData["PageTitle"] = "Archive Blocks";
        if (Session.BatchID > 0)
            Blocks = await _blocks.GetByBatchAsync(Session.BatchID ?? 0);
    }
}
