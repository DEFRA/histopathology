namespace Histo.Administration.Models;

/// <summary>
/// Represents an application user resolved from the database.
///
/// Legacy source: HistopathologyLib/clsUser.vb — GetUserByNTLogin output parameters
/// and GetUsers DataTable row.
///
/// The <see cref="NtLogin"/> field carries the Windows NT login used as the DB
/// lookup key in the legacy system. During Phase 2 (auth migration) this will
/// be mapped from the Entra ID UPN claim — see ISS-009.
/// </summary>
public sealed class User
{
    public int UserID { get; init; }
    public string Name { get; init; } = string.Empty;
    public int GroupCode { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public int AreaCode { get; init; }
    public string AreaName { get; init; } = string.Empty;
    public bool Active { get; init; }

    /// <summary>
    /// The Windows NT login (DOMAIN\username) used by the legacy GetUserByNTLogin SP.
    /// Populated from the <c>NTLogin</c> column returned by <c>GetUsers</c>.
    /// </summary>
    public string NtLogin { get; init; } = string.Empty;
}
