namespace Histo.Histology.Models;

/// <summary>Raised when a block ref is already held by the same sample on another submission.</summary>
public sealed class DuplicateBlockRefException(string blockRef)
    : InvalidOperationException($"Block reference {blockRef} has already been used for this sample.")
{
    public string BlockRef { get; } = blockRef;
}
