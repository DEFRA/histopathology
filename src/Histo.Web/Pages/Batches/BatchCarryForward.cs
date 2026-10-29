using Histo.Submissions.Models;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Full-row carry-forward for pages that edit only part of a <see cref="Batch"/> — the EditBatch SP
/// takes the whole row, so every field the page does not edit must be passed through unchanged.
/// </summary>
public static class BatchCarryForward
{
    /// <summary>The fields a page edits; every other field is carried forward from the source batch.</summary>
    public sealed record Edits(string Status, string CustomerRef, string? StatusComments,
        DateTime? ReceivedDate, DateTime? CompletedDate, string? TimeReceived, int? ReceivedBy, string? PostFixationOther);

    public static Batch Create(Batch source, Edits edits) => new()
    {
        ID                  = source.ID,
        Status              = edits.Status,
        CustomerRef         = edits.CustomerRef,
        Comments            = source.Comments,
        StatusComments      = edits.StatusComments,
        BatchDate           = source.BatchDate,
        ReceivedDate        = edits.ReceivedDate,
        TimeReceived        = edits.TimeReceived,
        ReceivedBy          = edits.ReceivedBy,
        PostFixationOther   = edits.PostFixationOther,
        CompletedDate       = edits.CompletedDate,
        SubmittedByUserID   = source.SubmittedByUserID,
        UserAreaCode        = source.UserAreaCode,
        IsCassetted         = source.IsCassetted,
        ByPassSort          = source.ByPassSort,
        RowStamp            = source.RowStamp,
        BatchType           = source.BatchType,
        ProjectContractCode = source.ProjectContractCode,
        ContactName         = source.ContactName,
        Species             = source.Species,
        Fixation            = source.Fixation,
        CustomerReceivedDate = source.CustomerReceivedDate,
        SubmittedBy         = source.SubmittedBy,
        SubmittedArea       = source.SubmittedArea,
        OtherSubmittedBy    = source.OtherSubmittedBy,
        OtherSubmittedArea  = source.OtherSubmittedArea ?? "",
        SafeToHandle        = source.SafeToHandle,
        IsBlocked           = source.IsBlocked,
        SampleSameProjects  = source.SampleSameProjects,
        AllTissuesAssigned  = source.AllTissuesAssigned,
    };
}
