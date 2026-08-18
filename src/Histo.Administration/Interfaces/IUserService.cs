using Histo.Administration.Models;

namespace Histo.Administration.Interfaces;

/// <summary>
/// Public service contract for user management — the module boundary exposed to Histo.Web.
/// Concrete implementation: <see cref="Histo.Administration.Services.UserService"/>.
/// </summary>
public interface IUserService
{
    /// <summary>Resolves the application user for the given NT login. Returns <see langword="null"/> if not found.</summary>
    Task<User?> ResolveUserAsync(string ntLogin, CancellationToken ct = default);

    /// <summary>Returns all users (active and inactive).</summary>
    Task<IReadOnlyList<User>> GetAllUsersAsync(CancellationToken ct = default);

    /// <summary>Returns users belonging to the given area code string.</summary>
    Task<IReadOnlyList<User>> GetUsersByAreaAsync(string userArea, CancellationToken ct = default);

    /// <summary>Creates a new user. Returns <see langword="false"/> on failure.</summary>
    Task<bool> CreateUserAsync(User user, CancellationToken ct = default);

    /// <summary>Updates an existing user. Returns <see langword="false"/> on failure.</summary>
    Task<bool> UpdateUserAsync(User user, int userId, CancellationToken ct = default);
}
