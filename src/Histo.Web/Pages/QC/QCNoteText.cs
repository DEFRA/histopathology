using Histo.Administration.Interfaces;
using Histo.Histology.Models;
using Histo.QualityControl.Models;
using Histo.Submissions.Models;

namespace Histo.Web.Pages.QC;

/// <summary>QC note wording shared by <see cref="QualityDataModel"/> and <see cref="EditQualityDataTestModel"/>.</summary>
public static class QCNoteText
{
    /// <summary>Carries a note warning from the single-test editor through its redirect to Quality data.</summary>
    public const string WarningTempDataKey = "QCNoteWarning";

    public const string CreateFailedWarning = "The test was saved, but the QC note could not be created, so it is not linked to one.";

    private const int LookupAntibodiesTse    = 4;
    private const int LookupAntibodiesNonTse = 5;
    private const int LookupSpecialStain     = 6;

    /// <summary>Test display names keyed "TestType|Code" — codes are only unique within a test type.</summary>
    public static async Task<IReadOnlyDictionary<string, string>> LoadTestNamesAsync(ILookupService lookups, int batchType)
    {
        var antibodyId = batchType == BatchTypeConstants.Tse ? LookupAntibodiesTse : LookupAntibodiesNonTse;

        var histTask     = lookups.GetHistologyTypesAsync();
        var antibodyTask = lookups.GetLookupDataAsync(antibodyId);
        var stainTask    = lookups.GetLookupDataAsync(LookupSpecialStain);
        await Task.WhenAll(histTask, antibodyTask, stainTask);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var i in histTask.Result ?? [])     map[$"{BlockTestType.Histology}|{i.Code ?? i.ID.ToString()}"]  = i.Name;
        foreach (var i in antibodyTask.Result ?? []) map[$"{BlockTestType.Antibodies}|{i.Code ?? i.ID.ToString()}"] = i.Name;
        foreach (var i in stainTask.Result ?? [])    map[$"{BlockTestType.Stain}|{i.Code ?? i.ID.ToString()}"]      = i.Name;
        return map;
    }

    public static string TestName(IReadOnlyDictionary<string, string> names, BlockTest t) =>
        names.TryGetValue($"{t.TestType}|{t.Code}", out var n) ? n : t.Code;

    /// <summary>One note entry; the test type keeps same-code tests of different types distinct.</summary>
    public static string Line(BlockTest t, string testName, string comment) =>
        $"{t.TestType} - {testName} ({t.HistologyRef} / {t.BlockRef}): {comment.Trim()}";

    public static string? AppendWarning(QCNoteAppendResult outcome, int noteRef) => outcome switch
    {
        QCNoteAppendResult.TooLong     => $"QC note {noteRef} is full, so the comments were not added to it. Edit the note to make room.",
        QCNoteAppendResult.Concurrency => $"QC note {noteRef} was changed by another user, so the comments were not added to it.",
        QCNoteAppendResult.NotFound    => $"QC note {noteRef} could not be found, so the comments were not added to it.",
        _ => null,
    };
}
