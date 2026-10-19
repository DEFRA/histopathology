namespace Histo.Histology.Models;

/// <summary>Optional free-text and flag fields recorded on a block when it is created or claimed.</summary>
public sealed record BlockOptions(string? CustomerRef = null, string? Comment = null, bool RepeatBlock = false);
