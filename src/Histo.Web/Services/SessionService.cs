using System.Security.Claims;
using Histo.Administration.Models;
using Histo.Web.Auth;

namespace Histo.Web.Services;

/// <summary>
/// HTTP-session-backed implementation of <see cref="ISessionService"/>.
///
/// Registered as scoped (one per request) in <c>Program.cs</c>.
/// Reads/writes ASP.NET Core <see cref="ISession"/> using the same semantic keys
/// that the legacy <c>SessionVars</c> class defined — ensuring the concepts
/// are preserved while eliminating string-constant scatter.
///
/// Phase 2 note: <see cref="Populate"/> currently derives the user name from
/// the <c>ClaimsPrincipal.Identity.Name</c> claim. When Entra ID auth is wired
/// in Phase 2, the UPN → NT login mapping (ISS-009) must be resolved here.
/// </summary>
public sealed class SessionService : ISessionService
{
    private const string KeyUserID    = "UserID";
    private const string KeyUserName  = "UserName";
    private const string KeyGroupName = "GroupName";
    private const string KeyGroupID   = "GroupID";
    private const string KeyUserEmail = "UserEmail";
    private const string KeyUserArea  = "UserArea";
    private const string KeyUserAreaID= "UserAreaID";
    private const string KeyBatchID   = "BatchID";
    private const string KeySubmID    = "BatchSubmissionID";
    private const string KeyAnimalID  = "AnimalID";
    private const string KeyBlockID   = "BlockID";
    private const string KeyBatchType  = "BatchType";
    private const string KeyReturnPage = "ReturnPage";

    private readonly ISession _session;

    public SessionService(IHttpContextAccessor accessor)
    {
        _session = accessor.HttpContext?.Session
            ?? throw new InvalidOperationException("ISession is not available.");
    }

    // ── User identity ────────────────────────────────────────────────────────

    public int    UserID    => GetInt(KeyUserID);
    public string UserName  => GetStr(KeyUserName);
    public string GroupName => GetStr(KeyGroupName);
    public int    GroupID   => GetInt(KeyGroupID);
    public string UserEmail => GetStr(KeyUserEmail);
    public string UserArea  => GetStr(KeyUserArea);
    public int    UserAreaID=> GetInt(KeyUserAreaID);

    // ── Workflow state ───────────────────────────────────────────────────────

    public int? BatchID
    {
        get => GetNullableInt(KeyBatchID);
        set => SetNullableInt(KeyBatchID, value);
    }

    public int? BatchSubmissionID
    {
        get => GetNullableInt(KeySubmID);
        set => SetNullableInt(KeySubmID, value);
    }

    public int? AnimalID
    {
        get => GetNullableInt(KeyAnimalID);
        set => SetNullableInt(KeyAnimalID, value);
    }

    public int? BlockID
    {
        get => GetNullableInt(KeyBlockID);
        set => SetNullableInt(KeyBlockID, value);
    }

    public int BatchType
    {
        get => GetInt(KeyBatchType);
        set => _session.Set(KeyBatchType, BitConverter.GetBytes(value));
    }

    public string ReturnPage
    {
        get => GetStr(KeyReturnPage);
        set => _session.SetString(KeyReturnPage, value);
    }

    // ── Role helpers ─────────────────────────────────────────────────────────

    public bool IsCustomer    => GroupName == "Customer";
    public bool IsHistoUser   => GroupName == "Histopathology User";
    public bool IsMaintenance => GroupName == "Maintenance";

    // ── Populate ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Populate(ClaimsPrincipal principal)
    {
        // Phase 2: replace with full Entra ID claim mapping (ISS-009).
        // For now, read identity name only — PopulateFromUser must be called
        // after UserService.ResolveUserAsync to set group/area fields.
        var name = principal.Identity?.Name ?? string.Empty;
        _session.SetString(KeyUserName, name);
    }

    /// <inheritdoc/>
    public void PopulateFromUser(User user)
    {
        // Mirrors the legacy VLAHeader.ascx::getUserDetails() Session writes:
        //   Session(SessionVars.SV_HeaderUserName)  = sName
        //   Session(SessionVars.SV_HeaderGroupID)   = sGroupCode
        //   Session(SessionVars.SV_HeaderGroupName) = sGroupName
        //   Session(SessionVars.SV_HeaderUserID)    = iUserID
        //   Session(SessionVars.SV_HeaderUserEmail) = sEmail
        //   Session(SessionVars.SV_HeaderUserArea)  = sAreaName
        //   Session(SessionVars.SV_HeaderUserAreaID)= iUserArea
        _session.SetString(KeyUserName,  user.Name);
        _session.SetString(KeyGroupName, user.GroupName);
        _session.SetString(KeyUserEmail, user.Email);
        _session.SetString(KeyUserArea,  user.AreaName);
        _session.Set(KeyUserID,    BitConverter.GetBytes(user.UserID));
        _session.Set(KeyGroupID,   BitConverter.GetBytes(user.GroupCode));
        _session.Set(KeyUserAreaID,BitConverter.GetBytes(user.AreaCode));
    }

    /// <inheritdoc/>
    public void PopulateFromClaims(ClaimsPrincipal principal)
    {
        _session.SetString(KeyUserName,  principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty);
        _session.SetString(KeyGroupName, principal.FindFirstValue(AppClaimTypes.GroupName) ?? string.Empty);
        _session.SetString(KeyUserEmail, principal.FindFirstValue(ClaimTypes.Email)
                                         ?? principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
                                         ?? string.Empty);
        _session.SetString(KeyUserArea,  principal.FindFirstValue(AppClaimTypes.UserArea) ?? string.Empty);

        if (int.TryParse(principal.FindFirstValue(AppClaimTypes.UserDbId),   out var uid)) _session.Set(KeyUserID,     BitConverter.GetBytes(uid));
        if (int.TryParse(principal.FindFirstValue(AppClaimTypes.GroupId),    out var gid)) _session.Set(KeyGroupID,    BitConverter.GetBytes(gid));
        if (int.TryParse(principal.FindFirstValue(AppClaimTypes.UserAreaId), out var aid)) _session.Set(KeyUserAreaID, BitConverter.GetBytes(aid));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string GetStr(string key) =>
        _session.GetString(key) ?? string.Empty;

    private int GetInt(string key)
    {
        var bytes = _session.Get(key);
        return bytes is null ? 0 : BitConverter.ToInt32(bytes, 0);
    }

    private int? GetNullableInt(string key)
    {
        var bytes = _session.Get(key);
        return bytes is null ? null : BitConverter.ToInt32(bytes, 0);
    }

    private void SetNullableInt(string key, int? value)
    {
        if (value is null)
            _session.Remove(key);
        else
            _session.Set(key, BitConverter.GetBytes(value.Value));
    }
}
