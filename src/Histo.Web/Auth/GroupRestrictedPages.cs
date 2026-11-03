using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Histo.Web.Auth;

/// <summary>
/// Pages restricted by user group, taken from the legacy <c>CheckPermissions()</c> calls
/// (one per legacy <c>.aspx.vb</c>). Pages not listed here were open to every group in legacy.
/// </summary>
public static class GroupRestrictedPages
{
    /// <summary>Legacy pages that redirected Customer to Home, plus the new pages split out of them (EditBatch, QualityData).</summary>
    public static readonly string[] HistoOrMaintenance =
    [
        "/Archive/ArchiveBlocks", "/Archive/ArchiveMenu", "/Archive/ArchiveTissues",
        "/Batches/BatchBlocks", "/Batches/BatchesForArchiving", "/Batches/BatchesForDispatch",
        "/Batches/BatchesForEditing", "/Batches/BatchesNotReceived", "/Batches/BatchesReceived",
        "/Batches/EditBatch", "/Batches/ReceiveBatch", "/Batches/SubmissionsOnHold",
        "/Batches/EditSubmissionStatus",
        "/Blocks/CopyBlocks", "/Blocks/CopySamples", "/Blocks/CopySamplesSummary",
        "/Bookings/BookBlockRef", "/Bookings/BookHistologyRef", "/Bookings/BookingMenu",
        "/QC/EditQCNote", "/QC/QCNotes", "/QC/QualityData",
        "/QC/AddQCNote", "/QC/EditQualityDataTest",
        "/Search/SearchArchiveLocation", "/Search/SearchBlockRefs", "/Search/SearchMenu",
        "/Search/SearchSubmissions", "/Search/SearchTest", "/Search/SearchUnUsedHistologyRefs",
        "/Search/ViewImportedData",
    ];

    /// <summary>Legacy pages that were Maintenance-only but sit outside the /Admin and /AuditLog folders.</summary>
    public static readonly string[] MaintenanceOnly =
    [
        "/Bookings/EditHistologyRef",
    ];

    public static void Apply(RazorPagesOptions options)
    {
        foreach (var page in HistoOrMaintenance)
            options.Conventions.AddPageApplicationModelConvention(page, model => model.Filters.Add(new HistoOrMaintenancePageFilter()));

        foreach (var page in MaintenanceOnly)
            options.Conventions.AddPageApplicationModelConvention(page, model => model.Filters.Add(new MaintenanceOnlyPageFilter()));
    }
}
