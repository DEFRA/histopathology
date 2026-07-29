using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Infrastructure;

namespace Histo.Administration.Services;

/// <summary>
/// Application service for user identity resolution and user management.
///
/// Thin orchestration layer over <see cref="IUserRepository"/>. Adds logging
/// and replaces the VB.NET pattern of catching exceptions and returning
/// <c>False</c> with an explicit null-return contract.
/// </summary>
public sealed class UserService
{
    private readonly IUserRepository _users;
    private readonly IAppLogger _logger;

    public UserService(IUserRepository users, IAppLogger logger)
    {
        _users  = users;
        _logger = logger;
    }

    /// <summary>
    /// Resolves the application user for the given NT login.
    ///
    /// Returns <see langword="null"/> when the login is not found or the account
    /// is inactive — equivalent to the legacy <c>clsUser.GetUserByNTLogin</c>
    /// returning <c>False</c>.
    ///
    /// ISS-009: During Phase 2 (auth migration) the NT login will be derived
    /// from the Entra ID UPN claim. Until that phase completes, callers must
    /// supply a login in DOMAIN\username format.
    /// </summary>
    public async Task<User?> ResolveUserAsync(string ntLogin, CancellationToken ct = default)
    {
        try
        {
            return await _users.GetUserByNtLoginAsync(ntLogin, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to resolve user for NT login.", ex);
            return null;
        }
    }

    /// <summary>Returns all active and inactive users.</summary>
    public async Task<IReadOnlyList<User>> GetAllUsersAsync(CancellationToken ct = default)
    {
        try
        {
            return await _users.GetUsersAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve users.", ex);
            return [];
        }
    }

    /// <summary>Returns users belonging to the given area code string.</summary>
    public async Task<IReadOnlyList<User>> GetUsersByAreaAsync(string userArea, CancellationToken ct = default)
    {
        try
        {
            return await _users.GetUsersByAreaAsync(userArea, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve users for area {Area}.", ex, userArea);
            return [];
        }
    }
}
