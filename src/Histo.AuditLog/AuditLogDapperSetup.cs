using Dapper;
using Histo.AuditLog.Models;
using System.Reflection;

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
    /// Configures a type map for <see cref="AuditLogEntry"/> that redirects the SP column
    /// <c>DateTime</c> (SQL keyword alias returned by all audit log stored procedures) to the
    /// <see cref="AuditLogEntry.ChangedAt"/> property.
    ///
    /// All other columns map by name case-insensitively as usual, and unrecognised columns are
    /// skipped. This allows <c>QueryAsync&lt;AuditLogEntry&gt;()</c> to be used throughout
    /// <c>AuditLogRepository</c> without a dynamic intermediate step.
    /// </summary>
    public static void RegisterTypeMaps() =>
        SqlMapper.SetTypeMap(typeof(AuditLogEntry), new AuditLogTypeMap());

    private sealed class AuditLogTypeMap : SqlMapper.ITypeMap
    {
        private readonly DefaultTypeMap _inner = new(typeof(AuditLogEntry));

        public ConstructorInfo? FindConstructor(string[] names, Type[] types) => _inner.FindConstructor(names, types);

        public ConstructorInfo? FindExplicitConstructor() => _inner.FindExplicitConstructor();

        public SqlMapper.IMemberMap GetConstructorParameter(ConstructorInfo constructor, string columnName) =>
            _inner.GetConstructorParameter(constructor, columnName);

        // The audit log SPs return the timestamp as "DateTime" (a SQL Server keyword used as an alias).
        public SqlMapper.IMemberMap? GetMember(string columnName) =>
            _inner.GetMember(string.Equals(columnName, "DateTime", StringComparison.OrdinalIgnoreCase)
                ? nameof(AuditLogEntry.ChangedAt)
                : columnName);
    }
}
