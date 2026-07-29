namespace Histo.Core.Domain;

/// <summary>
/// Batch lifecycle status code constants.
///
/// Legacy source: HistopathologyLib/clsBatch.vb, string constant block.
///
/// These string values are used as stored procedure parameters and DataRow
/// filter expressions throughout the application. The target architecture
/// preserves the exact string values to avoid any stored procedure signature changes.
/// </summary>
public static class BatchStatus
{
    /// <summary>Batch submitted by the customer. Legacy value: "1".</summary>
    public const string Submitted = "1";

    /// <summary>Batch received in the histology laboratory. Legacy value: "2".</summary>
    public const string Received = "2";

    /// <summary>Batch rejected. Legacy value: "3".</summary>
    public const string Rejected = "3";

    /// <summary>Batch processing completed. Legacy value: "4".</summary>
    public const string Completed = "4";

    /// <summary>Batch placed on hold. Legacy value: "5".</summary>
    public const string OnHold = "5";

    /// <summary>Batch actively being processed. Legacy value: "6".</summary>
    public const string InProgress = "6";
}
