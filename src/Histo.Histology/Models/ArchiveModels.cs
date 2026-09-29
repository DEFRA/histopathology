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
///
/// The SP's SELECT list returns two distinct row identifiers: <c>Batch.ID</c> (the
/// submission number, aliased in the SP simply as <c>ID</c>) and <c>BatchBlock.ID</c> (the
/// block's own row id, aliased as <c>BlockID</c> to avoid a same-named-column collision —
/// Dapper's typed mapper sets a property once per matching column name, so two columns both
/// named "ID" would silently leave <see cref="ID"/> holding whichever one came last).
/// </summary>
public sealed class BlockArchiveInfo
{
    public int ID { get; init; }
    public int BlockID { get; init; }
    public string? BlockRef { get; init; }
    public string? ArchiveLocation { get; init; }
    public DateTime? ArchivedDate { get; init; }
    public string? TissueDescription { get; init; }
    public short? NoPieces { get; init; }
    public string? ArchiveComment { get; init; }
}

/// <summary>
/// One result row for the Slide Archive search mode of SearchArchiveLocation.
///
/// Legacy source: HistopathologyLib/clsAnimal.vb — <c>GetAnimalSlideArchiveInformation</c>, which
/// merges rows from <c>GetAnimalStainArchiveInformation</c>, per-batch
/// <c>GetAnimalAntibodiesArchiveInformation</c>, and <c>GetAnimalHistologyArchiveInformation</c>
/// into this one shape — reproduced in full by <see cref="Histo.Histology.Repositories.BlockRepository.GetSlideArchiveAsync"/>.
/// </summary>
public sealed class SlideArchiveInfo
{
    public int BatchID { get; init; }
    public string? BlockRef { get; init; }
    public string? ArchiveLocation { get; init; }
    public DateTime? ArchivedDate { get; init; }
    public string? Description { get; init; }
    public string? TissueDescription { get; init; }
    public short? NoPieces { get; init; }
}
