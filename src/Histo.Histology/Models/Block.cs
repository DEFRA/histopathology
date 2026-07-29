namespace Histo.Histology.Models;

/// <summary>
/// Represents a cassette block record.
///
/// Legacy source: HistopathologyLib/clsBlock.vb — DataTable columns produced by
/// <c>GetBatchBlocksByID</c> and related stored procedures.
///
/// Status values are defined in <see cref="BlockStatus"/>.
/// </summary>
public sealed class Block
{
    public int ID { get; init; }
    public int BatchID { get; init; }
    public int AnimalID { get; init; }
    public string BlockRef { get; init; } = string.Empty;
    public string? CustomerRef { get; init; }
    public string? Comment { get; init; }
    public bool RepeatBlock { get; init; }
    public int Status { get; init; }
    public int Order { get; init; }
    public byte[]? RowStamp { get; init; }
}

/// <summary>
/// Block lifecycle status codes.
///
/// Legacy source: HistopathologyLib/clsBlock.vb — integer constants.
/// Values are stored in the Block.Status column in the database.
/// </summary>
public static class BlockStatus
{
    /// <summary>Block has been used (normal cassette). Legacy value: 1.</summary>
    public const int Used = 1;

    /// <summary>Block has been pre-booked but not yet assigned to a batch. Legacy value: 2.</summary>
    public const int PreBooked = 2;

    /// <summary>Block was pre-booked and has now been used. Legacy value: 3.</summary>
    public const int PreBookedUsed = 3;
}
