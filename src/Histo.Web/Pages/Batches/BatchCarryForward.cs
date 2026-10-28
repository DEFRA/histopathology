using Histo.Submissions.Models;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Full-row carry-forward for pages that edit only part of a <see cref="Batch"/> — the EditBatch SP
/// takes the whole row, so every field the page does not edit must be passed through unchanged.
/// </summary>
public static class BatchCarryForward
{
    public static Batch Create(Batch source, string status, string customerRef, string? statusComments,
        DateTime? receivedDate, DateTime? completedDate, string? timeReceived, int? receivedBy, string? postFixationOther) => new()
    {
        ID                  = source.ID,
        Status              = status,
        CustomerRef         = customerRef,
        Comments            = source.Comments,
        StatusComments      = statusComments,
        BatchDate           = source.BatchDate,
        ReceivedDate        = receivedDate,
        TimeReceived        = timeReceived,
        ReceivedBy          = receivedBy,
        PostFixationOther   = postFixationOther,
        CompletedDate       = completedDate,
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
