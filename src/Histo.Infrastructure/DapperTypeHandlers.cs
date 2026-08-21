using System.Data;
using System.Globalization;
using Dapper;

namespace Histo.Infrastructure;

/// <summary>
/// Dapper type handler for <see cref="DateTime?"/> that maps date columns
/// returned as <c>VARCHAR</c> strings in <c>dd/MM/yyyy</c> format (SQL CONVERT
/// style 103) to <see cref="DateTime?"/>.
///
/// <para>
/// Certain legacy stored procedures wrap date columns in
/// <c>CONVERT(VARCHAR, col, 103)</c> before returning them. Dapper's default
/// mapping calls <c>Convert.ChangeType</c> with <see cref="CultureInfo.InvariantCulture"/>,
/// which does not recognise the <c>dd/MM/yyyy</c> format, causing a
/// <see cref="FormatException"/> wrapped in <see cref="System.Data.DataException"/>.
/// </para>
///
/// <para>
/// Register once at startup via
/// <c>SqlMapper.AddTypeHandler(new NullableDateTimeTypeHandler())</c>.
/// Safe for all <see cref="DateTime?"/> columns — native SQL datetime values
/// are returned directly without string conversion.
/// </para>
/// </summary>
/// <summary>
/// Handles non-nullable <see cref="DateTime"/> columns returned as <c>VARCHAR</c> strings
/// (same dd/MM/yyyy format as <see cref="NullableDateTimeTypeHandler"/>).
/// Required for <see cref="Histo.AuditLog.Models.AuditLogEntry.ChangedAt"/> which is
/// <c>DateTime</c> (not <c>DateTime?</c>) and mapped from a SQL <c>DateTime</c> column alias.
/// </summary>
public sealed class DateTimeTypeHandler : SqlMapper.TypeHandler<DateTime>
{
    private static readonly string[] Formats = ["dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss"];

    /// <inheritdoc/>
    public override void SetValue(IDbDataParameter parameter, DateTime value)
        => parameter.Value = value;

    /// <inheritdoc/>
    public override DateTime Parse(object value)
    {
        if (value is null or DBNull) return DateTime.MinValue;
        if (value is DateTime dt) return dt;
        var s = value.ToString();
        if (string.IsNullOrWhiteSpace(s)) return DateTime.MinValue;
        return DateTime.ParseExact(s, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
    }
}

public sealed class NullableDateTimeTypeHandler : SqlMapper.TypeHandler<DateTime?>
{
    private static readonly string[] Formats = ["dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss"];

    /// <inheritdoc/>
    public override void SetValue(IDbDataParameter parameter, DateTime? value)
        => parameter.Value = (object?)value ?? DBNull.Value;

    /// <inheritdoc/>
    public override DateTime? Parse(object value)
    {
        if (value is null or DBNull) return null;
        // Native SQL datetime — returned as-is (no string conversion needed).
        if (value is DateTime dt) return dt;

        var s = value.ToString();
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.ParseExact(
            s,
            Formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None);
    }
}
