using Dapper;
using Histo.Infrastructure;
using Histo.QualityControl.Interfaces;
using Histo.QualityControl.Models;

namespace Histo.QualityControl.Repositories;

/// <summary>
/// Dapper implementation of <see cref="IQCNoteRepository"/>.
///
/// Uses legacy SPs exclusively:
/// - <c>GetBatchQCNotes</c> (no param = all notes, @QCNoteRef = filtered) for list and edit header
/// - <c>GetQCNoteHistStainTestInformation</c> for note text and rowstamp on edit
/// - <c>EditQCNote</c> / <c>AddQCNote</c> for mutations
/// </summary>
public sealed class QCNoteRepository : IQCNoteRepository
{
    private readonly IDbConnectionFactory _db;

    public QCNoteRepository(IDbConnectionFactory db) => _db = db;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<QCNote>> GetBySubmissionAsync(int submissionId, CancellationToken ct = default)
    {
        // Legacy QCNotes.aspx always called GetBatchQCNotes with no filter (showed all notes).
        // Filtering by batch is not supported by this SP; return all notes to match legacy behaviour.
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<QCNote>(
            "GetBatchQCNotes",
            new { QCNoteRef = (int?)null },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<QCNote?> GetByIdAsync(int qcNoteId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        // First call: get batch/submission header columns for the note
        var header = (await conn.QueryAsync<QCNote>(
            "GetBatchQCNotes",
            new { QCNoteRef = qcNoteId },
            commandType: System.Data.CommandType.StoredProcedure)).FirstOrDefault();

        if (header is null) return null;

        // Second call: get note text and rowstamp from the test information SP
        var detail = (await conn.QueryAsync<dynamic>(
            "GetQCNoteHistStainTestInformation",
            new { QCNoteRef = qcNoteId },
            commandType: System.Data.CommandType.StoredProcedure)).FirstOrDefault();

        return new QCNote
        {
            ID                 = header.ID,
            QCNoteRef          = header.QCNoteRef,
            StainRef           = header.StainRef,
            ProjectDescription = header.ProjectDescription,
            Species            = header.Species,
            Text               = (string?)detail?.QCText ?? string.Empty,
            RowStamp           = (byte[]?)detail?.RowStamp,
        };
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

    /// <inheritdoc/>
    public async Task<IReadOnlyList<QCNote>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<QCNote>(
            "GetBatchQCNotes",
            new { QCNoteRef = (int?)null },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }
}
