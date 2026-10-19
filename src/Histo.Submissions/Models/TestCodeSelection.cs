namespace Histo.Submissions.Models;

/// <summary>The histology, antibody and special-stain codes a premium-charge query is filtered on.</summary>
public sealed record TestCodeSelection(
    IReadOnlyList<string> HistologyCodes,
    IReadOnlyList<string> AntibodyCodes,
    IReadOnlyList<string> StainCodes);
