namespace Histo.Histology.Models;

/// <summary>
/// Represents a histology reference record (a unique identifier assigned to a
/// submission or animal).
///
/// Legacy source: HistopathologyLib/clsHistology.vb — CreateUsedHistologyRefs and
/// CreateUnusedHistologyRefs DataTable column shapes.
/// </summary>
public sealed class HistologyRef
{
    /// <summary>The histology reference string (e.g. "23/01234").</summary>
    public string Ref { get; init; } = string.Empty;

    /// <summary>Numeric type code classifying the reference (Neuropath, AbattoirSurvey, etc.).</summary>
    public int HistologyType { get; init; }

    /// <summary>The sender reference associated with this histology ref. Populated for unused refs.</summary>
    public string? SenderRef { get; init; }
}

/// <summary>
/// Histology reference booking result.
/// </summary>
public sealed class BookedHistologyRef
{
    public string Ref { get; init; } = string.Empty;
    public int HistologyType { get; init; }
    public bool IsBooked { get; init; }
    public int? AnimalID { get; init; }
}
