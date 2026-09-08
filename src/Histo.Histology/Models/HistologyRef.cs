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

/// <summary>
/// One row of the "next histology ref" counter grid — one per <see cref="HistologyRefTypeCode"/>.
///
/// Legacy source: HistopathologyLib/clsHistology.vb — <c>GetHistologyRefsTable</c>
/// (SP <c>GetHistologyRefs</c>), consumed by BookHistologyRef.aspx's counters grid.
/// </summary>
public sealed class HistologyRefCounter
{
    public int Type { get; init; }
    public string Description { get; init; } = string.Empty;
    public string NextHistologyRef { get; init; } = string.Empty;
    public byte[]? RowStamp { get; init; }
}

/// <summary>
/// Result of booking (reserving) a range of histology refs for a type.
/// Legacy source: BookHistologyRef.aspx.vb::UpdateHistologyRefs.
/// </summary>
public sealed class HistologyBookingResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    /// <summary>First ref number in the newly-booked range (inclusive).</summary>
    public int FirstBooked { get; init; }

    /// <summary>Last ref number in the newly-booked range (inclusive).</summary>
    public int LastBooked { get; init; }
}

/// <summary>
/// Histology ref range-type codes.
/// Legacy source: HistopathologySystem/Common.vb — <c>Enum HistologyRefType</c>.
/// The "use pg number" option (legacy value 6) is deliberately excluded — BookHistologyRef.aspx
/// removes it from its type dropdown via <c>RemovePGNumberOption</c>.
/// </summary>
public static class HistologyRefTypeCode
{
    public const int Neuropath = 1;
    public const int AbattoirSurvey = 2;
    public const int TBDiagnostic = 3;
    public const int GeneralPool = 4;
    public const int MouseProjects = 5;
}
