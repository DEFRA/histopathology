using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Web.Services;

namespace Histo.Web.Pages.Archive;

/// <summary>Replaces <c>ArchiveBlocks.aspx</c>.</summary>
public class ArchiveBlocksModel : GridPageModel
{
    private readonly IBlockService _blocks;

    public ArchiveBlocksModel(ISessionService session, IBlockService blocks)
        : base(session) => _blocks = blocks;

    public IReadOnlyList<Block> Blocks { get; private set; } = [];

    public int TotalCount => Blocks.Count;

    public IReadOnlyList<Block> PagedEntries =>
        (SortColumn switch
        {
            "CustomerRef" => SortDesc ? Blocks.OrderByDescending(b => b.CustomerRef) : Blocks.OrderBy(b => b.CustomerRef),
            "Status"      => SortDesc ? Blocks.OrderByDescending(b => b.Status)      : Blocks.OrderBy(b => b.Status),
            _             => SortDesc ? Blocks.OrderByDescending(b => b.BlockRef)    : Blocks.OrderBy(b => b.BlockRef),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Archive Blocks";
        ViewData["PageTitle"] = "Archive Blocks";
        if (Session.BatchID > 0)
            Blocks = await _blocks.GetByBatchAsync(Session.BatchID ?? 0);

        PopulateGridViewData(TotalCount);
    }
}
