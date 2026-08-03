namespace Histo.Histology.Models;

/// <summary>
/// Thrown when updating a <see cref="BlockTest"/> fails because another user has
/// modified the record since it was loaded (stale <see cref="BlockTest.RowStamp"/>).
/// </summary>
public sealed class BlockTestConcurrencyException : Exception
{
    public BlockTestConcurrencyException()
        : base("The test record has been modified by another user since it was loaded.")
    {
    }
}
