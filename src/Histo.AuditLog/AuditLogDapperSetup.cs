using System.Reflection;
using Dapper;
using Histo.AuditLog.Models;

namespace Histo.AuditLog;

/// <summary>
/// Registers Dapper type maps for the AuditLog module.
///
/// Call <see cref="RegisterTypeMaps"/> once at application startup, after
/// <c>SqlMapper.AddTypeHandler</c> calls and before the host is built.
/// </summary>
public static class AuditLogDapperSetup
{
    /// <summary>
    /// Configures a <see cref="CustomPropertyTypeMap"/> for <see cref="AuditLogEntry"/>
    /// that redirects the SP column <c>DateTime</c> (SQL keyword alias returned by all
    /// audit log stored procedures) to the <see cref="AuditLogEntry.ChangedAt"/> property.
    ///
    /// All other columns map by name case-insensitively as usual.
    /// This allows <c>QueryAsync&lt;AuditLogEntry&gt;()</c> to be used throughout
    /// <c>AuditLogRepository</c> without a dynamic intermediate step.
    /// </summary>
    public static void RegisterTypeMaps()
    {
        SqlMapper.SetTypeMap(
            typeof(AuditLogEntry),
            new CustomPropertyTypeMap(
                typeof(AuditLogEntry),
                (type, columnName) =>
                {
                    // The audit log SPs return the timestamp column as "DateTime"
                    // (a SQL Server keyword used as an alias). Map it to ChangedAt.
                    if (string.Equals(columnName, "DateTime", StringComparison.OrdinalIgnoreCase))
                        return type.GetProperty(nameof(AuditLogEntry.ChangedAt))!;

                    // All other columns map by name, case-insensitively.
                    return type.GetProperty(
                        columnName,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)!;
                }));
    }
}
