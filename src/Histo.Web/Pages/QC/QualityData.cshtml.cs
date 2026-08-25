using Histo.Administration.Interfaces;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
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
    private readonly IBatchService _batches;
    private readonly ILookupService _lookups;
    private readonly IUserService _users;

    private const int LookupProjects  = 19;
    private const int LookupContacts  = 18;

    public QualityDataModel(
        ISessionService session,
        IBlockTestService tests,
        IBatchService batches,
        ILookupService lookups,
        IUserService users)
        : base(session)
    {
        _tests   = tests;
        _batches = batches;
        _lookups = lookups;
        _users   = users;
    }

    public IReadOnlyList<BlockTest> Tests { get; private set; } = [];
    public int BatchID => Session.BatchID ?? 0;
    public Batch? BatchSummary { get; private set; }

    // Resolved display names for batch summary header
    public string? ProjectName { get; private set; }
    public string? PathologistName { get; private set; }
    public string? EnteredByName { get; private set; }
    public string? EnteredAreaName { get; private set; }
    public string? SubmittedByName { get; private set; }
    public string? SubmittedAreaName { get; private set; }

    [BindProperty(SupportsGet = true)] public string? FilterHistologyRef { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterTest { get; set; }

    public IReadOnlyList<string> HistologyRefs { get; private set; } = [];
    public IReadOnlyList<string> TestNames { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Quality data";
        ViewData["PageTitle"] = "Quality data";
        if (!Session.BatchID.HasValue) return RedirectToPage("/Index");

        var allTests    = await _tests.GetByBatchAsync(Session.BatchID.Value);
        BatchSummary    = await _batches.GetByIdAsync(Session.BatchID.Value);

        if (BatchSummary is not null)
            await ResolveBatchSummaryAsync(BatchSummary);

        HistologyRefs = allTests
            .Select(t => t.HistologyRef)
            .Where(r => !string.IsNullOrEmpty(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(r => r)
            .ToList()!;

        TestNames = allTests
            .Select(t => t.TestDetails ?? t.Code)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList()!;

        var filtered = allTests.AsEnumerable();
        if (!string.IsNullOrEmpty(FilterHistologyRef))
            filtered = filtered.Where(t => t.HistologyRef == FilterHistologyRef);
        if (!string.IsNullOrEmpty(FilterTest))
            filtered = filtered.Where(t => (t.TestDetails ?? t.Code) == FilterTest);
        Tests = filtered.ToList();

        return Page();
    }

    public IActionResult OnPostEdit(int testId)
    {
        return RedirectToPage("/QC/EditQualityDataTest", new { testId });
    }

    private async Task ResolveBatchSummaryAsync(Batch batch)
    {
        var projectsTask  = _lookups.GetLookupDataAsync(LookupProjects);
        var contactsTask  = _lookups.GetLookupDataAsync(LookupContacts);
        var userAreasTask = _lookups.GetUserAreasAsync();
        var usersTask     = _users.GetAllUsersAsync();

        await Task.WhenAll(projectsTask, contactsTask, userAreasTask, usersTask);

        var projectsById = projectsTask.Result.ToDictionary(p => p.ID.ToString(), p => p.Name, StringComparer.OrdinalIgnoreCase);
        ProjectName = !string.IsNullOrWhiteSpace(batch.ProjectContractCode)
            && projectsById.TryGetValue(batch.ProjectContractCode, out var pn) ? pn : batch.ProjectContractCode;

        var contactsById = contactsTask.Result.ToDictionary(c => c.ID.ToString(), c => c.Name, StringComparer.OrdinalIgnoreCase);
        PathologistName = !string.IsNullOrWhiteSpace(batch.ContactName)
            && contactsById.TryGetValue(batch.ContactName, out var cn) ? cn : batch.ContactName;

        var userById = usersTask.Result.ToDictionary(u => u.UserID, u => u.Name);
        EnteredByName   = batch.SubmittedBy.HasValue      && userById.TryGetValue(batch.SubmittedBy.Value,      out var eb) ? eb : null;
        SubmittedByName = batch.OtherSubmittedBy.HasValue && userById.TryGetValue(batch.OtherSubmittedBy.Value, out var sb) ? sb : null;

        var areaById = userAreasTask.Result.ToDictionary(a => a.ID, a => a.Name);
        EnteredAreaName   = batch.SubmittedArea.HasValue      && areaById.TryGetValue(batch.SubmittedArea.Value,      out var ea) ? ea : null;
        SubmittedAreaName = batch.OtherSubmittedArea.HasValue && areaById.TryGetValue(batch.OtherSubmittedArea.Value, out var sa) ? sa : null;
    }
}


