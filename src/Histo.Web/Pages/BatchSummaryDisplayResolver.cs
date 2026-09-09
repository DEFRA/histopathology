using Histo.Administration.Interfaces;
using Histo.Submissions.Models;

namespace Histo.Web.Pages;

/// <summary>
/// Batch summary fields resolved from raw stored codes/IDs to display names — shared by every
/// page that shows the "Species/Project/Pathologist/Entered by/Submitted by" batch header
/// (<c>QualityData</c>, <c>ArchiveBlocks</c>, <c>ArchiveTissues</c>), so the resolution logic
/// (including the <c>includeInactive: true</c> lookups needed so a deactivated Project/Pathologist
/// still resolves to a name instead of falling back to the raw code) lives in one place.
/// </summary>
public sealed record BatchSummaryDisplay(
    string? ProjectName,
    string? PathologistName,
    string? SpeciesName,
    string? EnteredByName,
    string? EnteredAreaName,
    string? SubmittedByName,
    string? SubmittedAreaName);

public static class BatchSummaryDisplayResolver
{
    private const int LookupProjects = 19;
    private const int LookupContacts = 18;

    public static async Task<BatchSummaryDisplay> ResolveAsync(
        Batch batch, ILookupService lookups, IUserService users, CancellationToken ct = default)
    {
        var projectsTask  = lookups.GetLookupDataAsync(LookupProjects, includeInactive: true, ct);
        var contactsTask  = lookups.GetLookupDataAsync(LookupContacts, includeInactive: true, ct);
        var speciesTask   = lookups.GetSpeciesLookupAsync(ct);
        var userAreasTask = lookups.GetUserAreasAsync(ct);
        var usersTask     = users.GetAllUsersAsync(ct);

        await Task.WhenAll(projectsTask, contactsTask, speciesTask, userAreasTask, usersTask);

        var projectsById = projectsTask.Result.ToDictionary(p => p.ID.ToString(), p => p.Name, StringComparer.OrdinalIgnoreCase);
        var projectName = !string.IsNullOrWhiteSpace(batch.ProjectContractCode)
            && projectsById.TryGetValue(batch.ProjectContractCode, out var pn) ? pn : batch.ProjectContractCode;

        var contactsById = contactsTask.Result.ToDictionary(c => c.ID.ToString(), c => c.Name, StringComparer.OrdinalIgnoreCase);
        var pathologistName = !string.IsNullOrWhiteSpace(batch.ContactName)
            && contactsById.TryGetValue(batch.ContactName, out var cn) ? cn : batch.ContactName;

        var speciesById = speciesTask.Result.ToDictionary(s => s.ID.ToString(), s => s.Name, StringComparer.OrdinalIgnoreCase);
        var speciesName = !string.IsNullOrWhiteSpace(batch.Species)
            && speciesById.TryGetValue(batch.Species, out var sn) ? sn : batch.Species;

        var userById = usersTask.Result.ToDictionary(u => u.UserID, u => u.Name);
        var enteredByName   = batch.SubmittedBy.HasValue      && userById.TryGetValue(batch.SubmittedBy.Value,      out var eb) ? eb : null;
        var submittedByName = batch.OtherSubmittedBy.HasValue && userById.TryGetValue(batch.OtherSubmittedBy.Value, out var sb) ? sb : null;

        var areaById = userAreasTask.Result.ToDictionary(a => a.ID, a => a.Name);
        var enteredAreaName = int.TryParse(batch.SubmittedArea, out var enteredAreaId)
            && areaById.TryGetValue(enteredAreaId, out var ea) ? ea : batch.SubmittedArea;
        var submittedAreaName = int.TryParse(batch.OtherSubmittedArea, out var submittedAreaId)
            && areaById.TryGetValue(submittedAreaId, out var sa) ? sa : batch.OtherSubmittedArea;

        return new BatchSummaryDisplay(
            projectName, pathologistName, speciesName,
            enteredByName, enteredAreaName, submittedByName, submittedAreaName);
    }
}
