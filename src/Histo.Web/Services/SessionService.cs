using System.Security.Claims;

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

    // ── Role helpers ─────────────────────────────────────────────────────────

    public bool IsCustomer    => GroupName == "Customer";
    public bool IsHistoUser   => GroupName == "Histopathology User";
    public bool IsMaintenance => GroupName == "Maintenance";

    // ── Populate ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Populate(ClaimsPrincipal principal)
    {
        // Phase 2: replace with full Entra ID claim mapping.
        // For now, read identity name from the principal and leave group/area
        // fields empty — they will be resolved by UserService.ResolveUserAsync
        // once the auth pipeline is wired.
        var name = principal.Identity?.Name ?? string.Empty;
        _session.SetString(KeyUserName, name);
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
