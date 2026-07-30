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
}
