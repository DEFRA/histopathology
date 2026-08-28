namespace Histo.Web.Services;

/// <summary>
/// Typed session wrapper — replaces the legacy <c>SessionVars</c> string-constant pattern
/// and the <c>VLAHeader.ascx::getUserDetails()</c> identity resolution call.
///
/// In Phase 2 (auth migration) <see cref="NtLogin"/> is populated from the Entra ID
/// UPN claim. Until then it is sourced from <see cref="System.Security.Claims.ClaimsPrincipal"/>.
///
/// All properties are nullable to handle unauthenticated / pre-session states
/// without throwing in razor markup.
/// </summary>
public interface ISessionService
{
    // ── User identity (replaces SV_Header* session keys) ────────────────────

    int UserID { get; }
    string UserName { get; }
    string GroupName { get; }
    int GroupID { get; }
    string UserEmail { get; }
    string UserArea { get; }
    int UserAreaID { get; }

    // ── Active workflow state (replaces SV_BatchID, SV_BatchSubmissionID …) ─

    int? BatchID { get; set; }
    int? BatchSubmissionID { get; set; }
    int? AnimalID { get; set; }
    int? BlockID { get; set; }

    /// <summary>
    /// Batch type for the currently active submission: 0 = TSE, 1 = Non-TSE.
    /// Mirrors <c>SessionVars.SV_SubmissionType</c> from the legacy application.
    /// Set in <c>CassettedModel</c> (new batch) and in <c>BatchDetailsModel</c>
    /// (opening an existing batch).
    /// </summary>
    int BatchType { get; set; }

    // ── Role helpers ─────────────────────────────────────────────────────────

    bool IsCustomer { get; }
    bool IsHistoUser { get; }
    bool IsMaintenance { get; }

    /// <summary>
    /// Populates user-identity session properties from the current
    /// <see cref="System.Security.Claims.ClaimsPrincipal"/>.
    ///
    /// Called once per request in the Razor Pages base class or layout.
    /// Replaces <c>VLAHeader.ascx::getUserDetails()</c>.
    /// </summary>
    void Populate(System.Security.Claims.ClaimsPrincipal principal);

    /// <summary>
    /// Hydrates all session identity fields from a resolved <see cref="Histo.Administration.Models.User"/>.
    /// Called by <c>HistoPageModel</c> after <c>UserService.ResolveUserAsync</c> succeeds.
    /// Replaces the legacy pattern of writing individual Session() keys in
    /// <c>VLAHeader.ascx::getUserDetails()</c>.
    /// </summary>
    void PopulateFromUser(Histo.Administration.Models.User user);

    /// <summary>
    /// Hydrates all session identity fields from the claims attached to the authenticated
    /// <see cref="System.Security.Claims.ClaimsPrincipal"/> after SAML sign-in.
    /// Called by <see cref="Histo.Web.Pages.HistoPageModel"/> on the first request after
    /// the SAML ACS redirect bakes app claims into the auth cookie.
    /// </summary>
    void PopulateFromClaims(System.Security.Claims.ClaimsPrincipal principal);

    /// <summary>
    /// The page path the user navigated from before arriving at BatchDetails.
    /// Used by <c>BatchDetails.cshtml</c> to provide a context-aware back link.
    /// Set by <c>ViewSubmissionsModel</c> and <c>SearchSubmissionsModel</c> in their
    /// <c>OnPostSelectAsync</c> handlers before returning <c>Page()</c>.
    /// Replaces the legacy <c>SessionVars.SV_RedirectCancelPage</c> pattern.
    /// </summary>
    string ReturnPage { get; set; }

    /// <summary>
    /// True when the user is in the read-only "View Submission" journey (legacy
    /// <c>SessionVars.SV_ViewSubmission</c>). Gates Add/Edit/Copy/Delete sample on
    /// <c>BatchBlockSummary</c>/<c>BatchDetails</c>.
    ///
    /// Distinct from <see cref="ReturnPage"/> (a navigation breadcrumb) — legacy sets/clears
    /// this flag independently per action button (e.g. "View submission" sets it true,
    /// "Edit submission"/"Copy submission" set it false), not merely on row selection.
    /// Set true by <c>ViewSubmissionsModel</c>/<c>SearchSubmissionsModel.OnPostSelectAsync</c>;
    /// cleared false by any journey that enters an editable context (<c>EditBatchModel</c>,
    /// <c>CassettedModel</c>, <c>BatchesForEditing</c>/<c>BatchesReceived</c>/<c>BatchesForDispatch</c>/
    /// <c>SubmissionsOnHold.OnPostSelect</c>, <c>CopyBatchSummaryModel</c>).
    /// </summary>
    bool IsViewSubmissionMode { get; set; }
}
