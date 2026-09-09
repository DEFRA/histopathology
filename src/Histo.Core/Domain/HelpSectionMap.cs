namespace Histo.Core.Domain;

/// <summary>
/// Maps a Razor Page path to the anchor ID of its corresponding section on <c>Help/Index.cshtml</c>,
/// so the "Help" nav link can deep-link into the relevant part of the (single-page) help content
/// instead of always opening at the top. Built from the legacy-page-name IDs already present on
/// each Help section's <c>&lt;h2&gt;</c> (e.g. <c>id="UserMaintenance"</c>), cross-referenced against
/// <c>docs/Functionality-Traceability-Matrix.md</c> for legacy-page-to-current-page renames/consolidations.
/// </summary>
public static class HelpSectionMap
{
    private static readonly Dictionary<string, string> PageToAnchor = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/Index"] = "home-screen",
        ["/Batches/Cassetted"] = "submission-type",
        ["/Batches/BatchDetails"] = "submission-details",
        ["/Submissions/SampleSummary"] = "sample-summary",
        ["/Submissions/AddSubmission"] = "add-sample",
        ["/Submissions/SubmissionDetails"] = "sample-details",
        ["/Submissions/SubmissionDetailsBlock"] = "sample-blocks",
        ["/Blocks/BlockDetails"] = "block-details",
        ["/Submissions/ViewSubmissions"] = "view-submissions",
        ["/Batches/CopyBatch"] = "copy-submission",
        ["/Batches/CopyBatchSummary"] = "copy-submission",
        ["/Search/SearchPMDates"] = "search-pm-dates",
        ["/Submissions/ViewSamples"] = "view-samples",
        ["/Batches/BatchesNotReceived"] = "submissions-awaiting-receipt",
        ["/Batches/ReceiveBatch"] = "receive-submission",
        ["/Batches/BatchesReceived"] = "submissions-received",
        ["/QC/QualityData"] = "quality-data",
        ["/QC/EditQualityDataTest"] = "quality-data",
        ["/Batches/BatchesForArchiving"] = "archive-submission",
        ["/Archive/ArchiveMenu"] = "archive-menu",
        ["/Archive/ArchiveBlocks"] = "archive-blocks",
        ["/Archive/ArchiveTissues"] = "archive-tissues",
        ["/QC/QCNotes"] = "qc-notes",
        ["/QC/AddQCNote"] = "qc-notes",
        ["/QC/EditQCNote"] = "edit-qc-note",
        ["/Search/SearchSubmissions"] = "search-submissions",
        ["/Search/SearchMenu"] = "search-menu",
        ["/Bookings/BookingMenu"] = "booking-menu",
        ["/Bookings/BookBlockRef"] = "book-block-ref",
        ["/Bookings/BookHistologyRef"] = "book-histology-ref",
        ["/Batches/EditBatch"] = "edit-submission-status",
        ["/Batches/BatchesForEditing"] = "edit-submission-status",
        ["/Batches/SubmissionsOnHold"] = "samples-on-hold",
        ["/Admin/EditAnimalRef"] = "edit-histology-ref",
        ["/Admin/UserMaintenance"] = "user-maintenance",
        ["/Admin/AddUser"] = "user-maintenance",
        ["/Admin/EditUser"] = "user-maintenance",
        ["/AuditLog/AuditLogMenu"] = "audit-logs",
        ["/AuditLog/AuditLogByDate"] = "audit-logs",
        ["/AuditLog/AuditLogByUser"] = "audit-logs",
        ["/AuditLog/AuditLogBySubmission"] = "audit-logs",
        ["/Search/ViewImportedData"] = "view-imported-data",
        ["/Admin/PickListMaintenance"] = "pick-list-maintenance",
        ["/Admin/LookupItems"] = "pick-list-maintenance",
        ["/Admin/AddLookupItem"] = "pick-list-maintenance",
        ["/Admin/EditLookupItem"] = "pick-list-maintenance",
        ["/Admin/PickListUserArea"] = "pick-list-maintenance",
        ["/Search/SearchTest"] = "search-test-totals",
        ["/Search/SearchBlockRefs"] = "search-block-refs",
        ["/Search/SearchArchiveLocation"] = "search-archive-location",
        ["/Batches/BatchesForDispatch"] = "submissions-awaiting-quality-data",
    };

    /// <summary>Returns the Help section anchor ID for <paramref name="pagePath"/>, or <see langword="null"/> when no section maps to it (the Help link then opens at the top of the page).</summary>
    public static string? Resolve(string? pagePath) =>
        pagePath is not null && PageToAnchor.TryGetValue(pagePath, out var anchor) ? anchor : null;
}
