using Histo.AuditLog.Models;

namespace Histo.AuditLog.Interfaces;

/// <summary>
/// Public service contract for audit log queries — the module boundary exposed to Histo.Web.
/// Concrete implementation: <see cref="Histo.AuditLog.Services.AuditLogService"/>.
/// </summary>
public interface IAuditLogService
{
    Task<IReadOnlyList<AuditLogEntry>> GetBySubmissionAsync(int submissionId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLogEntry>> GetByDateAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLogEntry>> GetByUserAsync(int userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default);
}
