namespace Histo.Histology.Models;

/// <summary>Outcome of one requested block ref in a "Book blocks" batch.</summary>
public enum PreBookedBlockOutcome
{
    Booked,

    /// <summary>The ref was already present for the sender ref (any status, any animal row).</summary>
    AlreadyExists,

    /// <summary>The sample record could not be retrieved or created, so nothing was inserted.</summary>
    NoSample,
}

/// <summary>Per-ref result returned by <see cref="Interfaces.IBlockRepository.BookPreBookedBlocksAsync"/>.</summary>
public sealed record PreBookedBlockResult(int BlockRef, PreBookedBlockOutcome Outcome);
