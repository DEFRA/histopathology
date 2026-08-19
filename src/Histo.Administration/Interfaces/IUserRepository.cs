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

    /// <summary>
    /// Creates a new user record. Maps to the <c>AddUser</c> stored procedure
    /// (legacy source: <c>clsUser.SaveUserData</c> insert parameter set).
    /// </summary>
    Task CreateUserAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing user record. Maps to the <c>EditUser</c> stored procedure
    /// (legacy source: <c>clsUser.SaveUserData</c> update parameter set).
    /// <para>
    /// <paramref name="userId"/> is the session user's ID passed as <c>@UserID</c> to the
    /// SP for audit logging — distinct from <c>user.UserID</c> (the record being edited).
    /// </para>
    /// </summary>
    Task UpdateUserAsync(User user, int userId, CancellationToken ct = default);
}
