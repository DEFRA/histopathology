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
/// - <c>GetQCNoteHistStainTestInformation</c> + <c>GetQCNoteAntibodiesInformation</c> for note
///   text and rowstamp on edit — legacy <c>clsQCNote.GetQCNoteTestInformation</c> combines both
///   result sets into a single table before reading the note; querying only the first SP
///   silently drops antibody-test QC notes.
/// - <c>EditQCNote</c> / <c>AddQCNote</c> for mutations
/// </summary>
public sealed class QCNoteRepository : IQCNoteRepository
{
    private readonly IDbConnectionFactory _db;

    public QCNoteRepository(IDbConnectionFactory db) => _db = db;

    /// <inheritdoc/>
    public async Task<QCNote?> GetByIdAsync(int qcNoteId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        // First call: batch/submission header columns, read via IDictionary so BatchType
        // (needed for the antibodies call below) is available alongside the typed fields.
        var headerRow = (await conn.QueryAsync(
            "GetBatchQCNotes",
            new { QCNoteRef = qcNoteId },
            commandType: System.Data.CommandType.StoredProcedure)).FirstOrDefault();

        if (headerRow is null) return null;
        var header = (IDictionary<string, object>)headerRow;
        var batchType = header.TryGetValue("BatchType", out var bt) ? Convert.ToInt32(bt) : 0;

        // Second + third calls: histology/stain test rows, then antibodies test rows —
        // legacy imports both result sets into the same DataTable before reading QCText.
        var histStainRows = await conn.QueryAsync(
            "GetQCNoteHistStainTestInformation",
            new { QCNoteRef = qcNoteId },
            commandType: System.Data.CommandType.StoredProcedure);

        var antibodyRows = await conn.QueryAsync(
            "GetQCNoteAntibodiesInformation",
            new { QCNoteRef = qcNoteId, SubmissionType = batchType },
            commandType: System.Data.CommandType.StoredProcedure);

        var detail = histStainRows.Concat(antibodyRows)
            .Select(r => (IDictionary<string, object>)r)
            .ToList();

        var first = detail.FirstOrDefault();
        var qcText = first is not null && first.TryGetValue("QCText", out var qt) ? Convert.ToString(qt) ?? "" : "";

        return new QCNote
        {
            ID                 = header.TryGetValue("ID", out var id) ? Convert.ToInt32(id) : 0,
            QCNoteRef          = header.TryGetValue("QCNoteRef", out var qcRef) && qcRef is not null ? Convert.ToInt32(qcRef) : null,
            StainRef           = header.TryGetValue("StainRef", out var stainRef) ? Convert.ToString(stainRef) : null,
            ProjectDescription = header.TryGetValue("ProjectDescription", out var project) ? Convert.ToString(project) : null,
            Species            = header.TryGetValue("Species", out var species) ? Convert.ToString(species) : null,
            // Blank QCText means the note has never been edited — legacy builds a default
            // Sender Ref / Histo Ref / Block Ref / Test table instead of showing nothing.
            Text               = !string.IsNullOrEmpty(qcText) ? qcText : BuildDefaultNoteText(detail),
            CreatedBy          = first is not null && first.TryGetValue("Name", out var name) ? Convert.ToString(name) : null,
            DateCreated        = first is not null && first.TryGetValue("DateCreated", out var created) ? ParseDate(created) : null,
            RowStamp           = first is not null && first.TryGetValue("RowStamp", out var rowStamp) ? rowStamp as byte[] : null,
        };
    }

    /// <summary>
    /// Parses a "DateCreated" value that may arrive as a native SQL <c>DateTime</c> or, when the
    /// SP wraps it in <c>CONVERT(VARCHAR, col, 103)</c>, a <c>dd/MM/yyyy</c> string — mirrors
    /// <see cref="Histo.Infrastructure.NullableDateTimeTypeHandler"/>, which only runs for
    /// Dapper's typed mapping and is bypassed by the dynamic/IDictionary reads used here.
    /// </summary>
    private static DateTime? ParseDate(object? value)
    {
        if (value is null or DBNull) return null;
        if (value is DateTime dt) return dt;

        var s = value.ToString();
        if (string.IsNullOrWhiteSpace(s)) return null;

        return DateTime.TryParseExact(
            s, new[] { "dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss" },
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var parsed)
            ? parsed
            : DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var fallback)
                ? fallback
                : null;
    }

    /// <summary>
    /// Reproduces legacy <c>EditQCNote.aspx.vb</c>'s <c>InsertSpaces</c>-padded table of
    /// Sender Ref / Histo Ref / Block Ref / Test, shown when a QC note has no saved text yet.
    /// </summary>
    private static string BuildDefaultNoteText(IReadOnlyList<IDictionary<string, object>> rows)
    {
        static string Pad(string value, int width) => value + new string(' ', Math.Max(0, width - value.Length));

        var sb = new System.Text.StringBuilder();
        sb.Append(Pad("Sender Ref", 22)).Append(Pad("Histo Ref", 22)).Append(Pad("Block Ref", 15)).Append("Test").AppendLine();

        foreach (var row in rows)
        {
            var senderRef    = row.TryGetValue("SenderRef", out var sr) ? Convert.ToString(sr) ?? "" : "";
            var histologyRef = row.TryGetValue("HistologyRef", out var hr) ? Convert.ToString(hr) ?? "" : "";
            var blockRef     = row.TryGetValue("BlockRef", out var br) ? Convert.ToString(br) ?? "" : "";
            var description  = row.TryGetValue("Description", out var d) ? Convert.ToString(d) ?? "" : "";

            sb.Append(Pad(senderRef, 22)).Append(Pad(histologyRef, 22)).Append(Pad(blockRef, 15)).Append(description).AppendLine();
        }

        return sb.ToString();
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
    public async Task<SubmissionQCNote> GetOrCreateForSubmissionAsync(int batchId, int testId, string testType, int userId, CancellationToken ct = default)
    {
        var table = testType switch
        {
            "Histology"  => "BlockHistology",
            "Antibodies" => "BlockAntibodies",
            "Stain"      => "BlockStain",
            _ => throw new ArgumentOutOfRangeException(nameof(testType), testType, "Unknown test type."),
        };

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);

        var lockResult = await conn.ExecuteScalarAsync<int>(
            "DECLARE @r int; EXEC @r = sp_getapplock @Resource = @Resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000; SELECT @r;",
            new { Resource = $"Histo:QCNote:Batch:{batchId}" }, tx);
        if (lockResult < 0)
            throw new TimeoutException($"Timed out waiting to allocate the QC note for submission {batchId}.");

        var existing = await conn.ExecuteScalarAsync<int?>(SubmissionNoteRefSql, new { BatchID = batchId }, tx);
        if (existing is > 0)
        {
            await tx.CommitAsync(ct);
            return new SubmissionQCNote(existing.Value, null);
        }

        var parameters = new DynamicParameters();
        parameters.Add("CreatedBy",   userId);
        parameters.Add("DateCreated", DateTime.Now);
        parameters.Add("NewID",       dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);
        await conn.ExecuteAsync("AddQCNote", parameters, tx, commandType: System.Data.CommandType.StoredProcedure);
        var newId = parameters.Get<int>("NewID");

        // Linking inside the lock is what makes the note visible to the next caller's lookup.
        var rowStamp = await conn.ExecuteScalarAsync<byte[]?>(
            $"UPDATE {table} SET QCNote = 1, QCNoteRef = @Ref OUTPUT inserted.RowStamp WHERE ID = @ID",
            new { Ref = newId, ID = testId }, tx);

        await tx.CommitAsync(ct);
        return new SubmissionQCNote(newId, rowStamp);
    }

    // Same rows as BlockTestRepository.GetByBatchAsync (histology worklist codes only), latest ref wins.
    private const string SubmissionNoteRefSql = """
        SELECT MAX(r) FROM (
            SELECT h.QCNoteRef AS r FROM BlockHistology h JOIN BatchBlock b ON b.ID = h.BlockID
             WHERE b.BatchID = @BatchID AND RTRIM(h.Code) IN ('1','2','5','7')
            UNION ALL
            SELECT a.QCNoteRef FROM BlockAntibodies a JOIN BatchBlock b ON b.ID = a.BlockID WHERE b.BatchID = @BatchID
            UNION ALL
            SELECT s.QCNoteRef FROM BlockStain s JOIN BatchBlock b ON b.ID = s.BlockID WHERE b.BatchID = @BatchID
        ) x WHERE r > 0
        """;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<QCNote>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<QCNote>(
            "GetBatchQCNotes",
            new { QCNoteRef = (int?)null },
            commandType: System.Data.CommandType.StoredProcedure);

        // The SP returns one row per distinct stain ref on the note's tests; the list shows one row per note.
        return rows
            .GroupBy(r => r.QCNoteRef)
            .Select(g => new QCNote
            {
                ID = g.First().ID,
                QCNoteRef = g.Key,
                ProjectDescription = g.First().ProjectDescription,
                Species = g.First().Species,
                StainRef = string.Join(", ", g.Select(r => r.StainRef?.Trim())
                    .Where(s => !string.IsNullOrEmpty(s)).Distinct(StringComparer.OrdinalIgnoreCase)),
            })
            .ToList();
    }
}
