using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Admin;

/// <summary>
/// Per-animal Sender Ref / Histology Ref rename utility — replaces the true
/// legacy <c>EditHistologyRef.aspx</c> page (linked from <c>Home.aspx</c> as
/// "Edit Sender/Histology Ref", Maintenance group only).
///
/// Named <c>EditAnimalRef</c> in the current app to avoid colliding with
/// <see cref="Bookings.EditHistologyRefModel"/>, which replaces a different
/// legacy workflow — the pool-level "next histology ref" counter maintenance
/// page (<c>clsHistology.UpdateHistologyRefs</c>, SP <c>EditHistologyRef</c>).
///
/// Resolves ISS-022. Legacy source: HistopathologySystem/EditHistologyRef.aspx.vb,
/// HistopathologyLib/clsAnimal.vb (<c>UpdateAnimalSenderRef</c>/<c>UpdateAnimalHistologyRef</c>).
///
/// Legacy validation kept: a PG-number sample's Histology Ref must be the reverse of its PG number
/// (PG years 2001 and earlier are not checked), and any other current-year ref must be lower than
/// the type's next counter (<c>clsHistology.GetHistologyRefsTable</c>). Refs from earlier years are
/// not numbered against this year's counters and skip the counter check.
/// </summary>
public class EditAnimalRefModel : HistoPageModel
{
    private const string OriginalSenderRefField = "OriginalSenderRef";

    private readonly ISubmissionService _submissions;
    private readonly IHistologyRefService _histologyRefs;

    public EditAnimalRefModel(ISessionService session, ISubmissionService submissions, IHistologyRefService histologyRefs)
        : base(session)
    {
        _submissions = submissions;
        _histologyRefs = histologyRefs;
    }

    [BindProperty] public string OriginalSenderRef { get; set; } = string.Empty;
    [BindProperty] public string NewSenderRef { get; set; } = string.Empty;
    [BindProperty] public string NewHistologyRef { get; set; } = string.Empty;

    public string? CurrentHistologyRef { get; private set; }
    public string? Error { get; private set; }

    /// <summary>Id of the field <see cref="Error"/> relates to — drives the error summary link and inline field error.</summary>
    public string? ErrorField { get; private set; }
    public string? SuccessMessage { get; private set; }

    public void OnGet()
    {
        SetTitle();
    }

    /// <summary>Looks up the current Histology Ref for the entered Sample (Sender) Ref.</summary>
    public async Task<IActionResult> OnPostGetHistologyRefAsync()
    {
        SetTitle();

        if (string.IsNullOrWhiteSpace(OriginalSenderRef))
        {
            Error = "You must enter a Sample Ref.";
            ErrorField = OriginalSenderRefField;
            return Page();
        }

        OriginalSenderRef = OriginalSenderRef.Trim();

        // Use GetAnimalBySender (exact match) rather than GetAnimalsBySenderRef (partial search),
        // matching the legacy EditHistologyRef.aspx::getHistologyRef behaviour which called
        // clsAnimal.GetAnimalBySender with the exact sender ref supplied.
        var matches = await _submissions.GetAnimalBySenderAsync(OriginalSenderRef);
        var match = matches.Count > 0 ? matches[0] : null;

        if (match is null)
        {
            Error = "Sample Ref. not found.";
            ErrorField = OriginalSenderRefField;
            return Page();
        }

        CurrentHistologyRef = match.HistologyRef;
        return Page();
    }

    /// <summary>Renames the Sender Ref. Legacy source: <c>cmdEditSenderRef_Click</c>.</summary>
    public async Task<IActionResult> OnPostEditSenderRefAsync()
    {
        SetTitle();

        if (string.IsNullOrWhiteSpace(OriginalSenderRef))
        {
            Error = "You must enter a Sample Ref.";
            ErrorField = OriginalSenderRefField;
            return Page();
        }

        if (string.IsNullOrWhiteSpace(NewSenderRef))
        {
            Error = "You must enter a New Sample Ref.";
            ErrorField = "NewSenderRef";
            return Page();
        }

        OriginalSenderRef = OriginalSenderRef.Trim();
        NewSenderRef = NewSenderRef.Trim();

        try
        {
            await _submissions.UpdateAnimalSenderRefAsync(OriginalSenderRef, NewSenderRef, Session.UserID);
            SuccessMessage = "The new Sample Ref has been saved";
        }
        catch (AnimalRefUpdateException ex)
        {
            Error = "The Sample Ref was not updated because: " + ex.Message;
        }
        catch (Exception ex)
        {
            Error = "ERROR: " + ex.Message;
        }

        return Page();
    }

    /// <summary>Renames (or clears) the Histology Ref. Legacy source: <c>cmdSaveHistologyRef_Click</c>.</summary>
    public async Task<IActionResult> OnPostSaveHistologyRefAsync()
    {
        SetTitle();

        if (string.IsNullOrWhiteSpace(OriginalSenderRef))
        {
            Error = "You must enter a Sample Ref.";
            ErrorField = OriginalSenderRefField;
            return Page();
        }

        OriginalSenderRef = OriginalSenderRef.Trim();
        NewHistologyRef = string.IsNullOrWhiteSpace(NewHistologyRef)
            ? string.Empty
            : NewHistologyRef.Trim();

        var validationError = await ValidateHistologyRefAsync();
        if (validationError is not null)
        {
            Error = validationError;
            ErrorField = "NewHistologyRef";
            return Page();
        }

        try
        {
            await _submissions.UpdateAnimalHistologyRefAsync(OriginalSenderRef, NewHistologyRef, Session.UserID);
            SuccessMessage = NewHistologyRef.Length > 0
                ? "The new Histology Ref has been saved"
                : "The old Histology Ref has been removed. You may now enter a new Histology Ref.";
        }
        catch (AnimalRefUpdateException ex)
        {
            Error = "The Histology Reference was not updated because: " + ex.Message;
        }
        catch (Exception ex)
        {
            Error = "ERROR: " + ex.Message;
        }

        return Page();
    }

    /// <summary>
    /// Legacy <c>cmdSaveHistologyRef_Click</c> validation: a PG-number sample must carry the reversed PG
    /// number; any other ref must be well formed and, for the current year, below the type's next counter.
    /// </summary>
    private async Task<string?> ValidateHistologyRefAsync()
    {
        if (SenderRefHelpers.TryParsePgNumber(OriginalSenderRef, out _, out var pgYear))
        {
            var correct = SenderRefHelpers.HistologyRefFromPgNumber(OriginalSenderRef);
            return !IsPreEqual01(pgYear) && correct is not null && NewHistologyRef != correct
                ? "The Histology Ref is not correct for the PG Number entered."
                : null;
        }

        if (NewHistologyRef.Length == 0) return null;

        if (!ValidationHelpers.ValidateHistoRef(NewHistologyRef, isHistologyUser: false))
            return "You must enter a valid Histology Ref.";

        // Legacy IsPreviousYearHistoRef: earlier years and 1970-1999 aren't numbered against this year's counters.
        var refYear = int.Parse(NewHistologyRef[..2]);
        if (refYear < DateTime.Now.Year % 100 || refYear >= 70) return null;

        var type = HistologyRefTypeCode.FromExistingRef(NewHistologyRef);
        if (type is null)
            return "The Histology Ref entered is outside the number range of every histology type.";

        var counter = (await _histologyRefs.GetCountersAsync()).FirstOrDefault(c => c.Type == type);
        if (counter is null || !int.TryParse(counter.NextHistologyRef, out var next))
            return "The next available Histology Ref for this type could not be checked.";

        return int.Parse(NewHistologyRef[^5..]) >= next
            ? $"The Histology Ref entered is higher than or equal to the current next Histology Ref ({HistologyRefHelpers.CurrentYearPrefix}{counter.NextHistologyRef.Trim()}) for the selected area."
            : null;
    }

    /// <summary>Legacy <c>IsPreEqual01</c>: PG years 2000, 2001 and 1970-1999 are not checked.</summary>
    private static bool IsPreEqual01(string year) =>
        year is "00" or "01" || (int.TryParse(year, out var y) && y >= 70);

    private void SetTitle()
    {
        ViewData["Title"] = "Edit Sender/Histology Ref";
        ViewData["PageTitle"] = "Edit Sender/Histology Ref";
    }
}
