using Dapper;
using Histo.Infrastructure;
using Histo.QualityControl.Interfaces;
using Histo.QualityControl.Models;

namespace Histo.QualityControl.Repositories;

/// <summary>
/// Dapper implementation of <see cref="IQCNoteRepository"/>.
///
/// The <see cref="UpdateAsync"/> method checks the SP RETURN_VALUE to detect
/// concurrent modification — mirroring the legacy <c>clsQCNote.UpdateQCNote</c>
/// pattern where SP return value 1 signals a stale rowstamp.
/// </summary>
public sealed class QCNoteRepository : IQCNoteRepository
{
    private readonly IDbConnectionFactory _db;

    public QCNoteRepository(IDbConnectionFactory db) => _db = db;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<QCNote>> GetBySubmissionAsync(int submissionId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<QCNote>(
            "GetQCNotes",
            new { ID = submissionId },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<QCNote?> GetByIdAsync(int qcNoteId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<QCNote>(
            "GetQCNote",
            new { ID = qcNoteId },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(int qcNoteId, string text, byte[] rowStamp, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("RETURN_VALUE", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);
        parameters.Add("QCNoteRef", qcNoteId);
        parameters.Add("QCText",    text);
        parameters.Add("RowStamp",  rowStamp, dbType: System.Data.DbType.Binary);
        parameters.Add("UserID",    userId);

        await conn.ExecuteAsync(
            "EditQCNote",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        var returnValue = parameters.Get<int>("RETURN_VALUE");
        if (returnValue == 1)
            throw new QCNoteConcurrencyException();
    }

    /// <inheritdoc/>
    public async Task<int> AddAsync(int submissionId, int createdByUserId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("CreatedBy",   createdByUserId);
        parameters.Add("DateCreated", DateTime.Now);
        parameters.Add("NewID",       dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

        await conn.ExecuteAsync(
            "AddQCNote",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        return parameters.Get<int>("NewID");
    }
}
