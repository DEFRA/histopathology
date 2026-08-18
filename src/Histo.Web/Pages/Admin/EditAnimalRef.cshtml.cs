using Histo.Core.Domain;
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
/// Deliberate simplification: the legacy page also validated that a manually
/// entered Histology Ref did not exceed the "next available" pool counter for
/// its type (cross-referencing <c>clsHistology.GetHistologyRefsTable</c>). That
/// counter is owned by the separate pool-counter workflow
/// (<see cref="Bookings.EditHistologyRefModel"/>) and is not re-validated here —
/// consistent with other documented simplifications in this codebase (e.g. ISS-018)
/// where cross-referencing unverifiable legacy state was deliberately deferred.
/// </summary>
public class EditAnimalRefModel : HistoPageModel
{
    private readonly ISubmissionService _submissions;

    public EditAnimalRefModel(ISessionService session, ISubmissionService submissions)
        : base(session) => _submissions = submissions;

    [BindProperty] public string OriginalSenderRef { get; set; } = string.Empty;
    [BindProperty] public string NewSenderRef { get; set; } = string.Empty;
    [BindProperty] public string NewHistologyRef { get; set; } = string.Empty;

    public string? CurrentHistologyRef { get; private set; }
    public string? Error { get; private set; }
    public string? SuccessMessage { get; private set; }

    public void OnGet()
    {
        SetTitle();
    }

    /// <summary>Looks up the current Histology Ref for the entered Sample (Sender) Ref.</summary>
    public async Task<IActionResult> OnPostGetHistologyRefAsync()
    {
        SetTitle();

        OriginalSenderRef = OriginalSenderRef.Trim();
        if (string.IsNullOrEmpty(OriginalSenderRef))
        {
            Error = "You must enter a Sample Ref.";
            return Page();
        }

        var matches = await _submissions.GetAnimalsBySenderRefAsync(OriginalSenderRef);
        var match = matches.FirstOrDefault(m =>
            string.Equals(m.SenderRef?.Trim(), OriginalSenderRef, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            Error = "Sample Ref. not found.";
            return Page();
        }

        CurrentHistologyRef = string.IsNullOrEmpty(match.HistologyRef) ? "<null>" : match.HistologyRef;
        return Page();
    }

    /// <summary>Renames the Sender Ref. Legacy source: <c>cmdEditSenderRef_Click</c>.</summary>
    public async Task<IActionResult> OnPostEditSenderRefAsync()
    {
        SetTitle();

        OriginalSenderRef = OriginalSenderRef.Trim();
        NewSenderRef = NewSenderRef.Trim();

        if (string.IsNullOrEmpty(OriginalSenderRef))
        {
            Error = "You must enter a Sample Ref.";
            return Page();
        }

        if (string.IsNullOrEmpty(NewSenderRef))
        {
            Error = "You must enter a New Sample Ref.";
            return Page();
        }

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

        OriginalSenderRef = OriginalSenderRef.Trim();
        NewHistologyRef = NewHistologyRef.Trim();

        if (string.IsNullOrEmpty(OriginalSenderRef))
        {
            Error = "You must enter a Sample Ref.";
            return Page();
        }

        // If the Sample Ref is a PG number, the Histology Ref must be its reverse-format equivalent.
        var pgReversedRef = AnimalHelpers.ComputePgAutoHistologyRef(OriginalSenderRef, isNeuropath: true);
        if (pgReversedRef is not null)
        {
            if (NewHistologyRef != pgReversedRef)
            {
                Error = "The Histology Ref is not correct for the PG Number entered.";
                return Page();
            }
        }
        else if (NewHistologyRef.Length > 0 && !ValidationHelpers.ValidateHistoRef(NewHistologyRef, isHistologyUser: false))
        {
            Error = "You must enter a valid Histology Ref.";
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

    private void SetTitle()
    {
        ViewData["Title"] = "Edit Sender/Histology Ref";
        ViewData["PageTitle"] = "Edit Sender/Histology Ref";
    }
}
