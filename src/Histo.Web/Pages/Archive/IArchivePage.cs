using Histo.Administration.Models;
using Histo.Submissions.Models;

namespace Histo.Web.Pages.Archive;

/// <summary>Members shared by the Archive blocks/tissues pages and consumed by the <c>_Archive*.cshtml</c> partials.</summary>
public interface IArchivePage
{
    int? BatchId { get; }
    Batch? Batch { get; }
    string? Error { get; }
    string? SuccessMessage { get; }
    string? SpeciesName { get; }
    string? ProjectName { get; }
    string? PathologistName { get; }
    string? EnteredByName { get; }
    string? EnteredAreaName { get; }
    string? SubmittedByName { get; }
    string? SubmittedAreaName { get; }
    bool IsViewMode { get; }
    bool SelectAllAcrossPages { get; }
    List<int> SelectedIds { get; }
    int TotalPages { get; }
    int PageNumber { get; }
    IReadOnlyList<LookupItem> ArchiveLocations { get; }
    string? ArchiveLocationCode { get; }
    DateTime? ArchivedDate { get; }
    string? Comment { get; }
}

/// <summary>One filter drop-down of the Archive pages' GET filter form.</summary>
public sealed record ArchiveFilter(string Id, string Label, IReadOnlyList<string> Options, string? Selected);

/// <summary>One row-selection checkbox cell of the Archive grids.</summary>
public sealed record ArchiveRowCheckbox(IArchivePage Page, int Id, string? ArchiveLocation, DateTime? ArchivedDate, string? Comment, string Label);
