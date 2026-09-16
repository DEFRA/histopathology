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

    /// <summary>
    /// Returns a human-readable display name for the given status code.
    /// Used anywhere the status code must be rendered as text rather than stored.
    /// </summary>
    public static string DisplayName(string status) => status switch
    {
        // Legacy display text sourced from the real luStatus lookup table (confirmed live:
        // Code=1 → Description="Not Received") — every legacy status dropdown (ViewSubmissions,
        // SearchSubmissions, EditBatch, ReceiveBatch) binds to this via DataTextField="Description".
        // A batch with this status has been submitted by the customer but not yet received by the
        // lab. Sentence-cased to "Not received" to match this method's existing casing convention
        // for other multi-word statuses ("On hold", "In progress").
        Submitted  => "Not received",
        Received   => "Received",
        Rejected   => "Rejected",
        Completed  => "Completed",
        OnHold     => "On hold",
        InProgress => "In progress",
        _          => status   // unknown code — show raw value rather than blank
    };

    /// <summary>
    /// Normalises a status label that came back as free text from a legacy stored procedure
    /// (e.g. <c>GetBatchesWithStatus</c>, which returns a description column rather than a
    /// code) to the same canonical wording as <see cref="DisplayName"/>.
    /// </summary>
    public static string NormalizeDisplayText(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return rawText ?? string.Empty;
        return rawText.Trim().ToLowerInvariant() switch
        {
            "not received" or "not started" or "submitted" => "Not received",
            "received"                                      => "Received",
            "rejected"                                       => "Rejected",
            "completed"                                      => "Completed",
            "on hold" or "onhold"                             => "On hold",
            "in progress" or "inprogress"                     => "In progress",
            _                                                 => rawText
        };
    }

    /// <summary>
    /// Returns the GOV.UK Design System tag colour modifier (e.g. "grey", "turquoise") for the
    /// given status code, for rendering the status as a <c>govuk-tag</c> in grids/summaries.
    /// Restricted to modifiers actually shipped in this app's govuk-frontend build:
    /// grey, green, turquoise, teal, purple, pink, red, orange, yellow, magenta.
    /// </summary>
    public static string TagClass(string status) => status switch
    {
        Submitted  => "grey",
        Received   => "turquoise",
        InProgress => "teal",
        OnHold     => "orange",
        Completed  => "green",
        Rejected   => "red",
        _          => "grey"
    };

    /// <summary>
    /// Same colour mapping as <see cref="TagClass"/>, but for callers that only have the free-text
    /// status description (e.g. a legacy stored procedure result) rather than the status code.
    /// </summary>
    public static string TagClassForText(string? rawText) => NormalizeDisplayText(rawText) switch
    {
        "Not received" => "grey",
        "Received"     => "turquoise",
        "In progress"  => "teal",
        "On hold"      => "orange",
        "Completed"    => "green",
        "Rejected"     => "red",
        _              => "grey"
    };
}
