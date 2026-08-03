namespace Histo.Histology.Models;

/// <summary>
/// A block reference and its usage status, as returned by the
/// <c>GetBlocksForHistoRef</c> / <c>GetBlocksForSenderRef</c> stored procedures.
///
/// Legacy source: SearchBlockRefs.aspx.vb — used to build Used/Unused/Pre-Booked
/// range rows via <see cref="Histo.Core.Domain.BlockRefRangeHelpers"/>.
/// Status values match <see cref="BlockStatus"/>.
/// </summary>
public sealed class UsedBlockRef
{
    public int BlockRef { get; init; }
    public int Status { get; init; }
}

/// <summary>
/// One result row for the Block Archive search mode of SearchArchiveLocation.
///
/// Legacy source: HistopathologyLib/clsAnimal.vb — <c>GetAnimalBlockArchiveInformation</c>,
/// column shape from SearchArchiveLocation.aspx grdBlockArchive BoundColumns
/// (flattened — the legacy expand/collapse hierarchy is not reproduced).
/// </summary>
public sealed class BlockArchiveInfo
{
    public int ID { get; init; }
    public string? BlockRef { get; init; }
    public string? ArchiveLocation { get; init; }
    public DateTime? ArchivedDate { get; init; }
    public string? TissueDescription { get; init; }
    public short? NoPieces { get; init; }
}

/// <summary>
/// One result row for the Slide Archive search mode of SearchArchiveLocation.
///
/// Legacy source: HistopathologyLib/clsAnimal.vb — <c>GetAnimalSlideArchiveInformation</c>.
/// SIMPLIFIED: the legacy method fans out across <c>GetAnimalStainArchiveInformation</c>,
/// <c>GetAnimalBatches</c>, and per-batch-type merge logic to build this shape. This
/// model reflects only the direct <c>GetAnimalStainArchiveInformation</c> result —
/// see the search module report for what was not ported.
/// </summary>
public sealed class SlideArchiveInfo
{
    public int BatchID { get; init; }
    public string? BlockRef { get; init; }
    public string? ArchiveLocation { get; init; }
    public DateTime? ArchivedDate { get; init; }
    public string? Description { get; init; }
    public string? TissueDescription { get; init; }
}
