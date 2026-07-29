using Histo.Administration.Models;

namespace Histo.Administration.Interfaces;

/// <summary>
/// Data access contract for application users.
///
/// Legacy source: HistopathologyLib/clsUser.vb — all public methods translated.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Resolves a user record from an NT login (DOMAIN\username).
    /// Maps to the <c>GetUserByNTLogin</c> stored procedure.
    ///
    /// Returns <see langword="null"/> when the login is not found or the account is inactive.
    /// </summary>
    Task<User?> GetUserByNtLoginAsync(string ntLogin, CancellationToken ct = default);

    /// <summary>
    /// Returns all users. Maps to <c>GetUsers</c> stored procedure.
    /// </summary>
    Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns users belonging to the given area code.
    /// Maps to <c>GetUsersByUserArea</c> stored procedure.
    /// </summary>
    Task<IReadOnlyList<User>> GetUsersByAreaAsync(string userArea, CancellationToken ct = default);
}
