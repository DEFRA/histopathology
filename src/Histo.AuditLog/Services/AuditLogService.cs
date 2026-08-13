using Histo.AuditLog.Interfaces;
using Histo.AuditLog.Models;
using Histo.Infrastructure;

namespace Histo.AuditLog.Services;

/// <summary>
/// Application service for audit log queries.
///
/// Thin orchestration layer over <see cref="IAuditLogRepository"/> with
/// structured error logging. Replaces direct calls to <c>clsAuditLog</c>
/// methods from the ASPX code-behind files.
/// </summary>
public sealed class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repo;
    private readonly IAppLogger _logger;

    public AuditLogService(IAuditLogRepository repo, IAppLogger logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    /// <summary>Returns all audit entries for a submission, optionally filtered by date range.</summary>
    public async Task<IReadOnlyList<AuditLogEntry>> GetBySubmissionAsync(
        int submissionId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default)
    {
        try
        {
            return await _repo.GetBySubmissionAsync(submissionId, startDate, endDate, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve audit log for submission {SubmissionId}.", ex, submissionId);
            return [];
        }
    }

    /// <summary>Returns audit entries within the given date range.</summary>
    public async Task<IReadOnlyList<AuditLogEntry>> GetByDateAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        try
        {
            return await _repo.GetByDateAsync(startDate, endDate, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve audit log by date.", ex);
            return [];
        }
    }

    /// <summary>Returns audit entries for a user, optionally filtered by date range.</summary>
    public async Task<IReadOnlyList<AuditLogEntry>> GetByUserAsync(
        int userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default)
    {
        try
        {
            return await _repo.GetByUserAsync(userId, startDate, endDate, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve audit log for user {UserId}.", ex, userId);
            return [];
        }
    }
}
