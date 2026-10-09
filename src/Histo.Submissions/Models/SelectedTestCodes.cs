namespace Histo.Submissions.Models;

/// <summary>
/// The histology/antibody/stain pick-list codes selected for a batch — grouped into a single
/// parameter so <c>CreateBatchWithCopiedSamplesAsync</c> stays under the 7-parameter limit.
/// </summary>
public sealed record SelectedTestCodes(
    IReadOnlyList<string> HistologyCodes,
    IReadOnlyList<string> AntibodyCodes,
    IReadOnlyList<string> StainCodes);
