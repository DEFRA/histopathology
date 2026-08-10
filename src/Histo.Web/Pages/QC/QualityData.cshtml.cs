using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.QC;

/// <summary>
/// Quality-control / dispatch worklist for the current batch — replaces
/// <c>QualityData.aspx</c>. Lists every histology, antibodies and special-stain
/// test on the batch's blocks so results, QC data, dispatch and archive
/// information can be recorded per test via <see cref="EditQualityDataTestModel"/>.
///
/// SIMPLIFIED: the legacy page edits several selected tests at once in a single
/// save. This page edits one test at a time instead. See
/// <see cref="Histo.Histology.Models.BlockTest"/> for further scope notes.
/// </summary>
public class QualityDataModel : HistoPageModel
{
    private readonly IBlockTestService _tests;

    public QualityDataModel(ISessionService session, IBlockTestService tests)
        : base(session) => _tests = tests;

    public IReadOnlyList<BlockTest> Tests { get; private set; } = [];
    public int BatchID => Session.BatchID ?? 0;

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Quality Data";
        ViewData["PageTitle"] = "Quality Data";
        if (!Session.BatchID.HasValue) return RedirectToPage("/Index");
        Tests = await _tests.GetByBatchAsync(Session.BatchID.Value);
        return Page();
    }

    public IActionResult OnPostEdit(int testId)
    {
        return RedirectToPage("/QC/EditQualityDataTest", new { testId });
    }
}
